using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] public TileMapLayer WaterLayer;
	[Export] public float Speed = 100.0f;
	
	private AnimationPlayer _animPlayer;
	private int _money = 0;
	private Label _moneyLabel;
	
	// --- SYSTÉM NÁSTROJŮ A ANIMACÍ ---
	private string _currentToolSuffix = "";
	private bool _isActing = false;
	private Vector2 _lookDirection = Vector2.Down; // Pro uchování směru (Vector2)
	private string _lastDirStr = "down";           // Pro textový název směru

	// --- RYBAŘENÍ ---
	public enum FishingState { None, HoldingRod, Casting, WaitingForBite, FishBiting, FishingMiniGame }
	public FishingState CurrentFishingState = FishingState.None;

	[Export] public float MinWaitTime = 2.0f;
	[Export] public float MaxWaitTime = 6.0f;
	[Export] public float ReactionWindow = 1.5f; 
	[Export] public PackedScene FishingBarScene;
	private Node _activeFishingBar;

	private Timer _fishingTimer;
	private Timer _reactionTimer;
	
	public override void _Ready()
	{
		_animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_animPlayer.AnimationFinished += OnAnimationFinished;

		_fishingTimer = new Timer();
		_fishingTimer.OneShot = true;
		_fishingTimer.Connect("timeout", Callable.From(OnFishBiteStarts));
		AddChild(_fishingTimer);

		_reactionTimer = new Timer();
		_reactionTimer.OneShot = true;
		_reactionTimer.Connect("timeout", Callable.From(OnFishEscaped));
		AddChild(_reactionTimer);
		
		_moneyLabel = GetNodeOrNull<Label>("MoneyLayout/Label");
		UpdateMoneyUI();
	}

	public override void _PhysicsProcess(double delta)
	{
		// 1. RYBAŘENÍ: Tady postavu zastavíme (kromě držení prutu)
		if (CurrentFishingState != FishingState.None && CurrentFishingState != FishingState.HoldingRod)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		// 2. POHYB: Ten poběží VŽDY, i když _isActing je true
		Vector2 direction = Input.GetVector("left", "right", "up", "down");
		
		if (direction != Vector2.Zero)
		{
			_lookDirection = direction;
			_lastDirStr = GetDirectionName(direction);
			Velocity = direction * Speed;
			
			// Animaci chůze pustíme jen tehdy, pokud zrovna nepoužíváme nástroj
			if (!_isActing) 
			{
				UpdateAnimation(direction);
			}
		}
		else
		{
			Velocity = Velocity.MoveToward(Vector2.Zero, Speed);
			if (!_isActing) // Idle pustíme jen pokud se nic neděje
			{
				PlayIdleAnimation();
			}
		}

		MoveAndSlide();
	}
	
	public override void _Input(InputEvent @event)
	{
		// Pokud hráč právě provádí akci, ignorujeme změnu slotů a vstupy
		if (_isActing) return;

		// Detekce slotů pro nástroje
		if (@event.IsActionPressed("slot_0")) _currentToolSuffix = "_seed";
		else if (@event.IsActionPressed("slot_1")) _currentToolSuffix = "_sword";
		else if (@event.IsActionPressed("slot_2")) _currentToolSuffix = "_pickaxe";
		else if (@event.IsActionPressed("slot_3")) _currentToolSuffix = "_axe";
		else if (@event.IsActionPressed("slot_4")) _currentToolSuffix = "_hoe";
		else if (@event.IsActionPressed("slot_5")) _currentToolSuffix = "_can";
		else if (@event.IsActionPressed("slot_6")) _currentToolSuffix = "_scythe";
		else if (@event.IsActionPressed("slot_7")) 
		{
			ToggleFishingRod();
		}
		else if (@event.IsActionPressed("slot_8") || @event.IsActionPressed("slot_9")) 
		{
			_currentToolSuffix = "";
			CurrentFishingState = FishingState.None;
		}

		if (@event.IsActionPressed("action_use"))
		{	
			HandleActionInput();
		}
	}

	private void UpdateAnimation(Vector2 dir)
	{
		if (_isActing) return;
		
		string animBase = GetDirectionName(dir);
		string suffix = (CurrentFishingState != FishingState.None) ? "_rod" : _currentToolSuffix;
		_animPlayer.Play("walk_" + animBase + suffix);
	}

	private void PlayIdleAnimation()
	{
		if (_isActing) return;
		if (CurrentFishingState == FishingState.WaitingForBite || 
			CurrentFishingState == FishingState.FishBiting || 
			CurrentFishingState == FishingState.FishingMiniGame) return; 

		string suffix = (CurrentFishingState != FishingState.None) ? "_rod" : _currentToolSuffix;
		_animPlayer.Play("idle_" + _lastDirStr + suffix);
	}

	private void HandleActionInput()
	{
		// Pokud máme prut, řešíme rybářskou logiku
		if (CurrentFishingState != FishingState.None)
		{
			switch (CurrentFishingState)
			{
				case FishingState.HoldingRod:
					if (IsFacingWater()) StartCasting();
					break;
				case FishingState.WaitingForBite:
					ResetFishing();
					break;
				case FishingState.FishBiting:
					_reactionTimer.Stop();
					StartFishingBar();
					break;
			}
			return;
		}

		// Pokud máme jiný nástroj (ne prut), spustíme akční animaci
		if (!string.IsNullOrEmpty(_currentToolSuffix))
		{
			PlayToolAnimation();
		}
	}

	private void PlayToolAnimation()
	{
		string toolName = _currentToolSuffix.TrimStart('_');
		string animName = $"use_{_lastDirStr}_{toolName}";

		if (_animPlayer.HasAnimation(animName))
		{
			_isActing = true;
			_animPlayer.Play(animName);
		}
	}

	private void OnAnimationFinished(StringName animName)
	{
		string aName = animName.ToString();

		// Pokud skončila animace nahazování
		if (aName.StartsWith("cast_"))
		{
			CurrentFishingState = FishingState.WaitingForBite;
			float waitTime = (float)GD.RandRange(MinWaitTime, MaxWaitTime);
			_fishingTimer.Start(waitTime);
		}
		// Pokud skončila jakákoliv akční animace nástroje
		else if (aName.StartsWith("use_"))
		{
			_isActing = false;
		}
	}

	// --- POMOCNÉ METODY PRO RYBAŘENÍ ---
	private void StartCasting()
	{
		CurrentFishingState = FishingState.Casting;
		_animPlayer.Play("cast_" + _lastDirStr);
	}

	private void OnFishBiteStarts()
	{
		CurrentFishingState = FishingState.FishBiting;
		_animPlayer.Play("bite_" + _lastDirStr);
		_reactionTimer.Start(ReactionWindow);
	}

	private void OnFishEscaped()
	{
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
		_isActing = false;
		PlayIdleAnimation();
	}

	private void ToggleFishingRod()
	{
		if (CurrentFishingState == FishingState.None)
		{
			CurrentFishingState = FishingState.HoldingRod;
			_currentToolSuffix = "_rod";
		}
		else
		{
			CurrentFishingState = FishingState.None;
			_currentToolSuffix = "";
		}
		PlayIdleAnimation();
	}

	private bool IsFacingWater()
	{
		if (WaterLayer == null) return false;
		Vector2 checkPos = GlobalPosition + (_lookDirection * 24);
		Vector2I tilePos = WaterLayer.LocalToMap(WaterLayer.ToLocal(checkPos));
		return WaterLayer.GetCellTileData(tilePos) != null;
	}

	private string GetDirectionName(Vector2 dir)
	{
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
			return dir.X > 0 ? "right" : "left";
		return dir.Y > 0 ? "down" : "up";
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
			_moneyLabel.Text = $"{_money:N2} €";
		}
	}
}
