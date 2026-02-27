using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public enum FishingState { None, HoldingRod, Casting, WaitingForBite, FishBiting, FishingMiniGame }
	public FishingState CurrentFishingState = FishingState.None;

	[Export] public float Speed = 100.0f;
	[Export] public float MinWaitTime = 2.0f;
	[Export] public float MaxWaitTime = 6.0f;
	[Export] public float ReactionWindow = 1.5f; // Jak dlouho má hráč na kliknutí při záběru
	[Export] public PackedScene FishingBarScene;
	private Node _activeFishingBar;

	[Export] public TileMapLayer WaterLayer;

	private AnimationPlayer _animPlayer;
	private Vector2 _lastDirection = Vector2.Down;
	private Timer _fishingTimer;   // Časovač na čekání na rybu
	private Timer _reactionTimer;  // Časovač na to, jak dlouho ryba kouše

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
	}

	public override void _PhysicsProcess(double delta)
	{
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

	private void UpdateAnimation(Vector2 dir)
	{
		string suffix = (CurrentFishingState != FishingState.None) ? "_rod" : "";
		string animBase = GetDirectionName(dir);
		_animPlayer.Play("walk_" + animBase + suffix);
	}

	private void PlayIdleAnimation()
	{
		// Pokud čekáme nebo hrajeme minihru, nebudeme přehrávat standardní Idle
		if (CurrentFishingState == FishingState.WaitingForBite || 
			CurrentFishingState == FishingState.FishBiting || 
			CurrentFishingState == FishingState.FishingMiniGame)
		{
			return; 
		}

		string suffix = (CurrentFishingState != FishingState.None) ? "_rod" : "";
		string animBase = GetDirectionName(_lastDirection);
		_animPlayer.Play("idle_" + animBase + suffix);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("slot_one")) ToggleFishingRod();

		if (@event.IsActionPressed("left_click"))
		{	
			HandleActionInput();
		}
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
		} // Konec switche
	} // Konec metody

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
		
		// Pustíme animaci cukání prutu
		_animPlayer.Play("bite_" + GetDirectionName(_lastDirection));
		
		// Spustíme okno pro reakci
		_reactionTimer.Start(ReactionWindow);
	}

	private void OnFishEscaped()
	{
		GD.Print("Ryba utekla, byl jsi pomalý.");
		ResetFishing();
	}

	private void StartFishingBar()
	{
		// Použij název FishingBarScene, který máš definovaný nahoře
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
		CurrentFishingState = (CurrentFishingState == FishingState.None) ? FishingState.HoldingRod : FishingState.None;
		PlayIdleAnimation();
	}
}
