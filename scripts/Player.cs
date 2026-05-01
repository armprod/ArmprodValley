using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public int Money 
	{ 
		get => _money; 
		set { _money = value; UpdateMoneyUI(); } 
	}

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
	private BuildMenu _buildMenu;

	// SYSTÉM STAVĚNÍ
	private Node2D _ghostBuilding;      
	private PackedScene _buildingToPlace; 
	private bool _isPlacing = false;  
	private bool _canPlaceNow = false; // Pojistka proti okamžitému položení    

	public override void _Ready()
	{
		_animPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		_fishing = GetNodeOrNull<FishingSystem>("FishingComponents");
		_moneyLabel = GetNodeOrNull<Label>("MoneyLayout/Label");
		
		_buildMenu = GetTree().CurrentScene.FindChild("BuildMenu", true, false) as BuildMenu;
		if (_buildMenu != null)
		{
			_buildMenu.BuildingSelected += StartGhostPlacement;
		}

		if (_animPlayer != null)
			_animPlayer.AnimationFinished += OnAnimationFinished;

		if (_fishing != null)
			_fishing.StateChanged += (state) => PlayFishingAnimation(state);

		UpdateMoneyUI();

		if (SaveManager.Instance != null && SaveManager.Instance.IsLoadingQueued)
		{
			var data = SaveManager.Instance.LoadGame(SaveManager.Instance.SelectedSlot);
			if (data != null) 
			{
				GlobalPosition = new Vector2(0, -500);
				this.Money = data.Money;
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// Logika pro "ducha" stavby
		if (_isPlacing && _ghostBuilding != null)
		{
			// Duch sleduje myš
			_ghostBuilding.GlobalPosition = GetGlobalMousePosition();

			// POJISTKA: Čekáme, až hráč pustí tlačítko z menu, než dovolíme stavět
			if (!_canPlaceNow && !Input.IsActionPressed("action_use"))
			{
				_canPlaceNow = true;
			}
			
			// Samotné položení budovy
			if (_canPlaceNow && Input.IsActionJustPressed("action_use"))
			{
				PlaceRealBuilding();
			}
			else if (Input.IsActionJustPressed("ui_cancel"))
			{
				CancelPlacement();
			}
			
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

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
		if (_isActing || _isPlacing) return;

		HandleSlotInput(@event);

		if (@event.IsActionPressed("action_use"))
		{    
			if (_currentToolSuffix == "_rod" && _fishing != null)
			{
				_fishing.HandleInput();
				return; 
			}
			
			if (_currentToolSuffix == "_hammer")
			{
				if (_buildMenu != null && !_buildMenu.Visible) 
				{
					_buildMenu.ToggleMenu();
					GetViewport().SetInputAsHandled();
					return; 
				}
			}

			if (!string.IsNullOrEmpty(_currentToolSuffix))
			{
				PlayToolAnimation();
			}
		}
	}

	// --- VEŘEJNÉ METODY PRO OSTATNÍ SYSTÉMY ---

	public void AddMoney(int amount) 
	{ 
		_money += amount; 
		UpdateMoneyUI(); 
	}

	public void SetMoney(int amount) 
	{ 
		_money = amount; 
		UpdateMoneyUI(); 
	}

	public bool IsFacingWater()
	{
		if (WaterLayer == null) return false;
		Vector2 checkPos = GlobalPosition + (_lookDirection * 24);
		Vector2I tilePos = WaterLayer.LocalToMap(WaterLayer.ToLocal(checkPos));
		return WaterLayer.GetCellTileData(tilePos) != null;
	}

	// --- SYSTÉM STAVĚNÍ ---

	private void StartGhostPlacement(string scenePath)
	{
		if (_ghostBuilding != null) _ghostBuilding.QueueFree();
		
		Input.MouseMode = Input.MouseModeEnum.Visible;

		_buildingToPlace = GD.Load<PackedScene>(scenePath);
		_ghostBuilding = _buildingToPlace.Instantiate<Node2D>();
		_ghostBuilding.Modulate = new Color(1, 1, 1, 0.5f);
		
		// Vypnutí kolizí u ducha
		DisableCollisionRecursive(_ghostBuilding);

		// Přidání do scény (mimo hráče)
		GetTree().CurrentScene.AddChild(_ghostBuilding);
		
		_isPlacing = true;
		_canPlaceNow = false; // Resetujeme pojistku
	}

	private void DisableCollisionRecursive(Node node)
	{
		if (node is CollisionShape2D cs) cs.Disabled = true;
		if (node is CollisionPolygon2D cp) cp.Disabled = true;

		foreach (Node child in node.GetChildren())
		{
			DisableCollisionRecursive(child);
		}
	}

	private void PlaceRealBuilding()
	{
		if (_buildingToPlace == null) return;

		var realBuilding = _buildingToPlace.Instantiate<Node2D>();
		GetTree().CurrentScene.AddChild(realBuilding);
		realBuilding.GlobalPosition = _ghostBuilding.GlobalPosition;

		CancelPlacement();
	}

	private void CancelPlacement()
	{
		if (_ghostBuilding != null) _ghostBuilding.QueueFree();
		_ghostBuilding = null;
		_isPlacing = false;
		_canPlaceNow = false;
		_buildingToPlace = null;
	}

	// --- ZBYTEK LOGIKY NÁSTROJŮ A ANIMACÍ ---

	private void HandleSlotInput(InputEvent @event)
	{
		if (@event.IsActionPressed("slot_7")) 
		{
			ToggleFishing();
			return;
		}

		if (_currentToolSuffix == "_rod") return; 

		if (@event.IsActionPressed("slot_0")) _currentToolSuffix = "_seed";
		else if (@event.IsActionPressed("slot_1")) _currentToolSuffix = "_sword";
		else if (@event.IsActionPressed("slot_2")) _currentToolSuffix = "_pickaxe";
		else if (@event.IsActionPressed("slot_3")) _currentToolSuffix = "_axe";
		else if (@event.IsActionPressed("slot_4")) _currentToolSuffix = "_hoe";
		else if (@event.IsActionPressed("slot_5")) _currentToolSuffix = "_can";
		else if (@event.IsActionPressed("slot_6")) _currentToolSuffix = "_scythe";
		else if (@event.IsActionPressed("slot_8")) _currentToolSuffix = "_hammer";
		else if (@event.IsActionPressed("slot_9")) _currentToolSuffix = "";
	}

	private void UpdateWalkAnimation()
	{
		if (_animPlayer == null) return;
		string animName = "walk_" + _lastDirStr + _currentToolSuffix;
		if (_animPlayer.CurrentAnimation != animName)
		{
			if (_animPlayer.HasAnimation(animName)) _animPlayer.Play(animName);
			else _animPlayer.Play("walk_" + _lastDirStr);
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

	private void UpdateMoneyUI() { if (_moneyLabel != null) _moneyLabel.Text = $"{_money} €"; }
	
	private string GetCurrentCombinedSuffix() => (_fishing != null && _fishing.CurrentState != FishingSystem.FishingState.None) ? "_rod" : _currentToolSuffix;

	private void ToggleFishing()
	{
		if (_fishing == null) return;
		if (_fishing.CurrentState == FishingSystem.FishingState.None)
		{
			_fishing.CurrentState = FishingSystem.FishingState.HoldingRod;
			_currentToolSuffix = "_rod";
		}
		else
		{
			_fishing.CurrentState = FishingSystem.FishingState.None;
			_currentToolSuffix = "";
		}
		PlayIdleAnimation();
	}

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
