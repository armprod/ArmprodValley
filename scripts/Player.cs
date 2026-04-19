using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public int Money => _money;

	[Export] public TileMapLayer WaterLayer;
	[Export] public float Speed = 100.0f;
	
	private AnimationPlayer _animPlayer;
	private int _money = 0;
	private Label _moneyLabel;
	private string _currentToolSuffix = ""; 
	private bool _isActing = false;
	private Vector2 _lookDirection = Vector2.Down;
	private string _lastDirStr = "down";
	
	private FishingSystem _fishing;

	public override void _Ready()
	{
		_animPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		_fishing = GetNodeOrNull<FishingSystem>("FishingComponents");
		_moneyLabel = GetNodeOrNull<Label>("MoneyLayout/Label");

		if (_animPlayer != null)
			_animPlayer.AnimationFinished += OnAnimationFinished;

		if (_fishing != null)
			_fishing.StateChanged += (state) => PlayFishingAnimation(state);

		UpdateMoneyUI();

		if (SaveManager.Instance != null && SaveManager.Instance.IsLoadingQueued)
			SaveManager.Instance.LoadGame(this);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_fishing != null && _fishing.CurrentState != FishingSystem.FishingState.None && 
			_fishing.CurrentState != FishingSystem.FishingState.HoldingRod)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		Vector2 direction = Input.GetVector("left", "right", "up", "down");
		
		if (direction != Vector2.Zero)
		{
			_lookDirection = direction;
			_lastDirStr = GetDirectionName(direction);
			Velocity = direction * Speed;
			if (!_isActing) UpdateWalkAnimation();
		}
		else
		{
			Velocity = Velocity.MoveToward(Vector2.Zero, Speed);
			if (!_isActing) PlayIdleAnimation();
		}

		MoveAndSlide();
	}

	public override void _Input(InputEvent @event)
	{
		if (_isActing) return;

		HandleSlotInput(@event);

		if (@event.IsActionPressed("action_use"))
		{	
			// 1. PŘEDNOST MÁ RYBAŘENÍ
			// Pokud máš v ruce prut (suffix je _rod)
			if (_currentToolSuffix == "_rod" && _fishing != null)
			{
				_fishing.HandleInput();
				// Tady můžeme přidat 'return', aby se zbytek metody už neřešil
				return; 
			}
			
			// 2. OSTATNÍ NÁSTROJE (spustí se jen, když nemáš prut)
			if (!string.IsNullOrEmpty(_currentToolSuffix))
			{
				PlayToolAnimation();
			}
		}
	}

	private void HandleSlotInput(InputEvent @event)
	{
		// 1. Speciální případ pro Slot 7 (Prut) - ten musí fungovat vždy, aby šel prut i schovat
		if (@event.IsActionPressed("slot_7")) 
		{
			ToggleFishing();
			return;
		}

		// 2. ZÁMEK: Pokud už prut držíš, ignoruj všechny ostatní sloty (0-6, 8, 9)
		if (_currentToolSuffix == "_rod") 
		{
			return; 
		}

		// 3. Normální přepínání nástrojů (stane se jen, když v ruce NENÍ prut)
		if (@event.IsActionPressed("slot_0")) _currentToolSuffix = "_seed";
		else if (@event.IsActionPressed("slot_1")) _currentToolSuffix = "_sword";
		else if (@event.IsActionPressed("slot_2")) _currentToolSuffix = "_pickaxe";
		else if (@event.IsActionPressed("slot_3")) _currentToolSuffix = "_axe";
		else if (@event.IsActionPressed("slot_4")) _currentToolSuffix = "_hoe";
		else if (@event.IsActionPressed("slot_5")) _currentToolSuffix = "_can";
		else if (@event.IsActionPressed("slot_6")) _currentToolSuffix = "_scythe";
		else if (@event.IsActionPressed("slot_8") || @event.IsActionPressed("slot_9")) 
		{
			_currentToolSuffix = "";
		}
	}

	// --- KLÍČOVÉ OPRAVY PRO OSTATNÍ SKRIPTY ---

	// Opravuje chybu v SaveManager a FarmingSystem
	public void AddMoney(int amount) 
	{ 
		_money += amount; 
		UpdateMoneyUI(); 
	}

	public bool IsFacingWater()
	{
		if (WaterLayer == null) return false;
		Vector2 checkPos = GlobalPosition + (_lookDirection * 24);
		Vector2I tilePos = WaterLayer.LocalToMap(WaterLayer.ToLocal(checkPos));
		return WaterLayer.GetCellTileData(tilePos) != null;
	}

	public void SetMoney(int amount) { _money = amount; UpdateMoneyUI(); }

	private string GetCurrentCombinedSuffix()
	{
		if (_fishing != null && _fishing.CurrentState != FishingSystem.FishingState.None) return "_rod";
		return _currentToolSuffix;
	}

	private void UpdateWalkAnimation()
	{
		if (_animPlayer == null) return;

		// Musí se brát aktuální suffix, který jsme nastavili v ToggleFishing
		string animName = "walk_" + _lastDirStr + _currentToolSuffix;

		if (_animPlayer.CurrentAnimation != animName)
		{
			if (_animPlayer.HasAnimation(animName))
			{
				_animPlayer.Play(animName);
			}
			else
			{
				// Pokud nemáš animaci walk_down_rod, pustí se aspoň základní chůze
				_animPlayer.Play("walk_" + _lastDirStr);
				GD.PrintErr("VAROVÁNÍ: Animace " + animName + " neexistuje!");
			}
		}
	}

	private void PlayIdleAnimation()
	{
		if (_animPlayer == null) return;
		string animName = "idle_" + _lastDirStr + GetCurrentCombinedSuffix();
		if (_animPlayer.CurrentAnimation != animName) _animPlayer.Play(animName);
	}

	private void PlayToolAnimation()
	{
		if (_animPlayer == null) return;
		string toolName = _currentToolSuffix.TrimStart('_');
		string animName = $"use_{_lastDirStr}_{toolName}";
		if (_animPlayer.HasAnimation(animName)) { _isActing = true; _animPlayer.Play(animName); }
	}

	private void ToggleFishing()
	{
		if (_fishing == null) 
		{
			GD.PrintErr("CHYBA: FishingComponent nebyl nalezen!");
			return;
		}

		if (_fishing.CurrentState == FishingSystem.FishingState.None)
		{
			_fishing.CurrentState = FishingSystem.FishingState.HoldingRod;
			_currentToolSuffix = "_rod";
			GD.Print("Vytahuji prut. Suffix nastaven na: _rod");
		}
		else
		{
			_fishing.CurrentState = FishingSystem.FishingState.None;
			_currentToolSuffix = "";
			GD.Print("Schovávám prut.");
		}
		
		// Okamžitě aktualizuj animaci, aby hráč nečekal na pohyb
		PlayIdleAnimation();
	}

	private void UpdateMoneyUI() { if (_moneyLabel != null) _moneyLabel.Text = $"{_money} €"; }
	
	private void PlayFishingAnimation(string state) { _animPlayer?.Play(state + "_" + _lastDirStr); }

	private void OnAnimationFinished(StringName animName) 
	{ 
		string aName = animName.ToString();
		if (aName.StartsWith("cast_")) _fishing?.OnCastAnimationFinished();
		else if (aName.StartsWith("use_") || aName.StartsWith("bite_") || aName.StartsWith("catch_")) _isActing = false;
	}

	private string GetDirectionName(Vector2 dir) 
	{
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y)) return dir.X > 0 ? "right" : "left";
		return dir.Y > 0 ? "down" : "up";
	}
}
