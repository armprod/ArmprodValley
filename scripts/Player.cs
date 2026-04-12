using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] public TileMapLayer WaterLayer;
	private AnimationPlayer _animPlayer;
	private Vector2 _lastDirection = Vector2.Down;
	private int _money = 0;
	private Label _moneyLabel;
	
	// Akluální nástroj
	private string _currentToolSuffix = "";
	
	public enum FishingState { None, HoldingRod, Casting, WaitingForBite, FishBiting, FishingMiniGame }
	public FishingState CurrentFishingState = FishingState.None;

	// Fishing minigame
	[Export] public float Speed = 100.0f;
	[Export] public float MinWaitTime = 2.0f;
	[Export] public float MaxWaitTime = 6.0f;
	[Export] public float ReactionWindow = 1.5f; // Jak dlouho má hráč na kliknutí při záběru
	[Export] public PackedScene FishingBarScene;
	private Node _activeFishingBar;

	private Timer _fishingTimer;   // Časovač na čekání na rybu
	private Timer _reactionTimer;  // Časovač na to, jak dlouho ryba kouše
	public bool IsFishing = false;
	
	public override void _Ready()
	{
		_animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_animPlayer.AnimationFinished += OnAnimationFinished;

		// Časovač 1: Čekání na rybu
		_fishingTimer = new Timer();
		_fishingTimer.OneShot = true;
		_fishingTimer.Connect("timeout", Callable.From(OnFishBiteStarts));
		AddChild(_fishingTimer);

		// Časovač 2: Reakce hráče na záběr
		_reactionTimer = new Timer();
		_reactionTimer.OneShot = true;
		_reactionTimer.Connect("timeout", Callable.From(OnFishEscaped));
		AddChild(_reactionTimer);
		
		_moneyLabel = GetNodeOrNull<Label>("MoneyLayout/Label");
		UpdateMoneyUI();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsFishing) return; // Pokud rybaří, skript hráče do animací nekecá
		// STOPKA: Pokud se rybaří, hráč se nehýbe
		if (CurrentFishingState != FishingState.None && CurrentFishingState != FishingState.HoldingRod)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		Vector2 direction = Input.GetVector("left", "right", "up", "down");
		Vector2 velocity = Velocity;

		if (direction != Vector2.Zero)
		{
			_lastDirection = direction;
			velocity = direction * Speed;
			UpdateAnimation(direction);
		}
		else
		{
			velocity = velocity.MoveToward(Vector2.Zero, Speed);
			PlayIdleAnimation();
		}

		Velocity = velocity;
		MoveAndSlide();
	}
	
	public override void _Input(InputEvent @event)
	{
		// Detekce slotů pro nástroje
		if (@event.IsActionPressed("slot_0")) _currentToolSuffix = "_seed";
		else if (@event.IsActionPressed("slot_1")) _currentToolSuffix = "_sword";
		else if (@event.IsActionPressed("slot_2")) _currentToolSuffix = "_pickaxe";
		else if (@event.IsActionPressed("slot_3")) _currentToolSuffix = "_axe";
		else if (@event.IsActionPressed("slot_4")) _currentToolSuffix = "_hoe";
		else if (@event.IsActionPressed("slot_5")) _currentToolSuffix = "_can";
		else if (@event.IsActionPressed("slot_7")) {
			ToggleFishingRod();
			_currentToolSuffix = (CurrentFishingState != FishingState.None) ? "_rod" : "";
		}
		// Ostatní sloty (prázdná ruka)
		else if (@event.IsActionPressed("slot_8") || @event.IsActionPressed("slot_9")) _currentToolSuffix = "";

		if (@event.IsActionPressed("action_use"))
		{	
			HandleActionInput();
		}
	}

	private void UpdateAnimation(Vector2 dir)
	{
		string animBase = GetDirectionName(dir);
		// Prioritu má stav rybaření, pak suffix vybraného nástroje
		string suffix = (CurrentFishingState != FishingState.None) ? "_rod" : _currentToolSuffix;

		_animPlayer.Play("walk_" + animBase + suffix);
		_animPlayer.Advance(0);
	}

	private void PlayIdleAnimation()
	{
		// Ignorujeme idle během aktivních fází rybaření
		if (CurrentFishingState == FishingState.WaitingForBite || 
			CurrentFishingState == FishingState.FishBiting || 
			CurrentFishingState == FishingState.FishingMiniGame)
		{
			return; 
		}

		string animBase = GetDirectionName(_lastDirection);
		string suffix = (CurrentFishingState != FishingState.None) ? "_rod" : _currentToolSuffix;

		_animPlayer.Play("idle_" + animBase + suffix);
	}

	private void HandleActionInput()
	{
		switch (CurrentFishingState)
		{
			case FishingState.HoldingRod:
				if (IsFacingWater()) StartCasting();
				break;

			case FishingState.WaitingForBite:
				GD.Print("Moc brzo! Rybu jsi vyplašil.");
				_fishingTimer.Stop();
				ResetFishing();
				break;

			case FishingState.FishBiting:
				GD.Print("ZÁSEK! Jdeme na minihru.");
				_reactionTimer.Stop();
				StartFishingBar(); // Voláme metodu pro zobrazení scény
				break;
		}
	}

	private void StartCasting()
	{
		CurrentFishingState = FishingState.Casting;
		_animPlayer.Play("cast_" + GetDirectionName(_lastDirection));
	}

	private void OnAnimationFinished(StringName animName)
	{
		if (animName.ToString().StartsWith("cast_"))
		{
			CurrentFishingState = FishingState.WaitingForBite;
			float waitTime = (float)GD.RandRange(MinWaitTime, MaxWaitTime);
			_fishingTimer.Start(waitTime);
			GD.Print($"Nahozeno. Čekám...");
		}
	}

	private void OnFishBiteStarts()
	{
		GD.Print("RYBA KOUŠE!");
		CurrentFishingState = FishingState.FishBiting;
		
		_animPlayer.Play("bite_" + GetDirectionName(_lastDirection));
		
		_reactionTimer.Start(ReactionWindow);
	}

	private void OnFishEscaped()
	{
		GD.Print("Ryba utekla, byl jsi pomalý.");
		ResetFishing();
	}

	private void StartFishingBar()
	{
		if (FishingBarScene == null) return; 

		CurrentFishingState = FishingState.FishingMiniGame;
		
		_activeFishingBar = FishingBarScene.Instantiate();
		GetTree().Root.AddChild(_activeFishingBar);
	}

	private void ResetFishing()
	{
		CurrentFishingState = FishingState.HoldingRod;
		PlayIdleAnimation();
	}

	private bool IsFacingWater()
	{
		if (WaterLayer == null) return false;
		Vector2 checkPos = GlobalPosition + (_lastDirection * 24);
		Vector2I tilePos = WaterLayer.LocalToMap(WaterLayer.ToLocal(checkPos));
		return WaterLayer.GetCellTileData(tilePos) != null;
	}

	private string GetDirectionName(Vector2 dir)
	{
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
			return dir.X > 0 ? "right" : "left";
		return dir.Y > 0 ? "down" : "up";
	}

	private void ToggleFishingRod()
{
	if (CurrentFishingState == FishingState.None)
	{
		CurrentFishingState = FishingState.HoldingRod;
		_currentToolSuffix = "_rod"; // Nastavíme suffix prutu
	}
	else
	{
		CurrentFishingState = FishingState.None;
		_currentToolSuffix = ""; // Resetujeme na prázdné ruce
	}
	PlayIdleAnimation();
}
	
	public void AddMoney(int amount)
	{
		_money += amount;
		UpdateMoneyUI();
	}
	
	private void UpdateMoneyUI()
	{
		if (_moneyLabel != null)
		{
			_moneyLabel.Text = $"{(float)_money:N2} €";
		}
	}
	
	//private void PlayPlantingAnimation()
	//{
		//string animBase = GetDirectionName(_lastDirection);
		//string animName = "use_" + animBase + "_seed";
		//
		//if(_animPlayer.HasAnimation(animName))
		//{
			//_animPlayer.Play(animName);
		//}
		//else 
		//{
			//GD.Print("Animace {animName} nenalezena.");
		//}
	//}
}
