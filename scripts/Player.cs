using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public int Money 
	{ 
		get => _money; 
		set { _money = value; UpdateMoneyUI(); } 
	}

	[Export] public float Speed = 100.0f;
	[Export] public float MaxInteractionDistance = 60.0f;
	[Export] public TileMapLayer WaterLayer;
	[Export] private int _beehivePrice = 10000;
	[Export] private int _fruitTreePrice = 5000;
	
	[Export]private Label _moneyLabel;
	[Export]private Label _timeLabel;
	[Export]private Label _dayLabel;
	
	// GENERAL
	private AnimationPlayer _animPlayer;
	private int _money = 0;
	public string _currentToolSuffix = ""; 
	private bool _isActing = false;
	private Vector2 _lookDirection = Vector2.Down;
	private string _lastDirStr = "down";
	
	private FishingSystem _fishing;

	// BUILDING SYSTEM
	private Node2D _ghostBuilding;      
	private PackedScene _buildingToPlace; 
	private bool _isPlacing = false;  
	private bool _canPlaceNow = false;
	private BuildMenu _buildMenu;
	private Area2D _interactionArea;
	private int _currentPrice;
	private bool _isPlacingBeehive = false;
	private bool _isPlacingTree = false;

	public override void _Ready()
	{
		_animPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		_fishing = GetNodeOrNull<FishingSystem>("FishingComponents");
		_moneyLabel = GetNodeOrNull<Label>("Layouts/MoneyLayout/MoneyLabel");
		_timeLabel = GetNodeOrNull<Label>("Layouts/TimeLayout/TimeLabel");
		_interactionArea = GetNodeOrNull<Area2D>("InteractionArea");
		
		_buildMenu = GetTree().CurrentScene.FindChild("BuildMenu", true, false) as BuildMenu;
		if (_buildMenu != null)
		{
			_buildMenu.BuildingSelected += (scenePath, price) => StartGhostPlacement(scenePath, price);
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
		
		var timeManager = GetTree().Root.FindChild("TimeManager", true, false) as TimeManager;

		if (timeManager != null)
		{
			// Propojíme signál s metodou UpdateTimelineUI
			timeManager.Connect(TimeManager.SignalName.TimeChanged, Callable.From<int, int, int>(UpdateTimelineUI));
			
			// Hned po startu tam něco vypíšeme, abychom viděli, že to funguje
			UpdateTimelineUI(timeManager.CurrentDay, 8, 0);
			GD.Print("PLAYER: Úspěšně připojeno k TimeManageru.");
		}
		else
		{
			GD.PrintErr("PLAYER: TimeManager nebyl ve scéně nalezen!");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isPlacing && _ghostBuilding != null)
		{
			Vector2 mousePos = GetGlobalMousePosition();
			int gridSize = 16;
			
			// Výpočet mřížky
			float snappedX = Mathf.Floor(mousePos.X / gridSize) * gridSize;
			float snappedY = Mathf.Floor(mousePos.Y / gridSize) * gridSize;
			_ghostBuilding.GlobalPosition = new Vector2(snappedX, snappedY);

			int currentPrice = GetCurrentBuildingPrice();
			_ghostBuilding.Modulate = (Money >= currentPrice) ? new Color(1, 1, 1, 0.5f) : new Color(1, 0, 0, 0.5f);

			if (!_canPlaceNow && !Input.IsActionPressed("action_use"))
				_canPlaceNow = true;
			if (_canPlaceNow && Input.IsActionJustPressed("action_use"))
				PlaceRealBuilding();
			else if (Input.IsActionJustPressed("ui_cancel"))
				CancelPlacement();
			
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
		if (_isPlacing && @event.IsActionPressed("ui_cancel"))
		{
			CancelPlacement();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (_isActing || _isPlacing) return;

		HandleSlotInput(@event);

		if (@event.IsActionPressed("action_interact"))
		{
			HandleInteraction(false); 
		}

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
				
				if (_currentToolSuffix == "_pickaxe" || _currentToolSuffix == "_axe")
				{
					HandleInteraction(true); 
				}
			}
		}
	}

	private void HandleInteraction(bool isAttack)
	{
		if (_interactionArea == null) return;

		var targets = _interactionArea.GetOverlappingBodies();
		IInteractable bestTarget = null; // Používáme Interface!
		float minMouseDist = float.MaxValue;
		Vector2 mousePos = GetGlobalMousePosition();

		foreach (var body in targets)
		{
			// Ptáme se: "Implementuje tento objekt rozhraní IInteractable?"
			if (body is IInteractable interactable)
			{
				if (GlobalPosition.DistanceTo(interactable.GlobalPosition) > MaxInteractionDistance)
					continue;

				float distToMouse = mousePos.DistanceTo(interactable.GlobalPosition);
				if (distToMouse < minMouseDist)
				{
					minMouseDist = distToMouse;
					bestTarget = interactable;
				}
			}
		}

		if (bestTarget != null)
		{
			if (isAttack) bestTarget.Destroy(this);
			else bestTarget.Interact(this); // Volá správnou metodu podle typu objektu
		}
	}

	public bool IsFacingWater()
	{
		if (WaterLayer == null) return false;
		// Kontrola 24 pixelů před hráčem
		Vector2 checkPos = GlobalPosition + (_lookDirection * 24);
		Vector2I tilePos = WaterLayer.LocalToMap(WaterLayer.ToLocal(checkPos));
		return WaterLayer.GetCellTileData(tilePos) != null;
	}

	// --- SYSTÉM STAVĚNÍ ---

	private void StartGhostPlacement(string scenePath, int price)
	{
		if (_ghostBuilding != null) _ghostBuilding.QueueFree();
		Input.MouseMode = Input.MouseModeEnum.Visible;

		// Nastavení ceny a typu stavby
		_currentPrice = price;
		_isPlacingBeehive = scenePath.Contains("Beehive");
		_isPlacingTree = scenePath.Contains("FruitTree");

		_buildingToPlace = GD.Load<PackedScene>(scenePath);
		_ghostBuilding = _buildingToPlace.Instantiate<Node2D>();
		_ghostBuilding.Modulate = new Color(1, 1, 1, 0.5f);
		_ghostBuilding.TopLevel = true; 

		DisableCollisionRecursive(_ghostBuilding);
		GetTree().CurrentScene.AddChild(_ghostBuilding);
		
		_isPlacing = true;
		_canPlaceNow = false; 
	}

	private void DisableCollisionRecursive(Node node)
	{
		if (node is CollisionShape2D cs) cs.Disabled = true;
		if (node is CollisionPolygon2D cp) cp.Disabled = true;
		foreach (Node child in node.GetChildren()) DisableCollisionRecursive(child);
	}

	private void PlaceRealBuilding()
	{
		if (_buildingToPlace == null) return;
		if (Money >= _currentPrice)
		{
			RemoveMoney(_currentPrice);
			var realBuilding = _buildingToPlace.Instantiate<Node2D>();
			GetTree().CurrentScene.AddChild(realBuilding);
			realBuilding.GlobalPosition = _ghostBuilding.GlobalPosition;
			CancelPlacement();
		} else {
			GD.Print("Nedostatek financí.");
		}
	}

	private void CancelPlacement()
	{
		if (_ghostBuilding != null) _ghostBuilding.QueueFree();
		_ghostBuilding = null;
		_isPlacing = false;
		_canPlaceNow = false;
		_buildingToPlace = null;
	}

	// --- POMOCNÉ METODY ---

	public void AddMoney(int amount) { _money += amount; UpdateMoneyUI(); }
	public void RemoveMoney(int amount) { _money -= amount; UpdateMoneyUI(); }
	
	private void HandleSlotInput(InputEvent @event)
	{
		if (@event.IsActionPressed("slot_7")) { ToggleFishing(); return; }
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
		if (_animPlayer.HasAnimation(animName)) _animPlayer.Play(animName);
		else _animPlayer.Play("walk_" + _lastDirStr);
	}

	private void PlayIdleAnimation()
	{
		if (_animPlayer == null) return;
		string animName = "idle_" + _lastDirStr + GetCurrentCombinedSuffix();
		if (_animPlayer.HasAnimation(animName)) _animPlayer.Play(animName);
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
		if (_fishing.CurrentState == FishingSystem.FishingState.None) { _fishing.CurrentState = FishingSystem.FishingState.HoldingRod; _currentToolSuffix = "_rod"; }
		else { _fishing.CurrentState = FishingSystem.FishingState.None; _currentToolSuffix = ""; }
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
	
	private int GetCurrentBuildingPrice()
	{
		if (_isPlacingBeehive) return _beehivePrice;
		if (_isPlacingTree) return _fruitTreePrice;
		return 0;
	}
	
	// Metoda, která se zavolá při každém "tiknutí" minuty
	private void OnTimeChanged(int day, int hour, int minute)
	{
		if (GodotObject.IsInstanceValid(_timeLabel))
			UpdateTimelineUI(day, hour, minute);
	}

	private void UpdateTimelineUI(int day, int hour, int minute)
	{
		string[] weekDays = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
		string currentDayName = weekDays[day % 7];

		if (_timeLabel != null)
			_timeLabel.Text = $"{currentDayName}. {hour:D2}:{minute:D2}";

		if (_dayLabel != null)
			_dayLabel.Text = $"Day: {day}";
	}
	
	public override void _ExitTree()
	{
		if (TimeManager.Instance != null)
			TimeManager.Instance.TimeChanged -= OnTimeChanged;
	}
	
}
