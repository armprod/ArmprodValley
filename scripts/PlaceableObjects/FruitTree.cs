using Godot;
using System;

public partial class FruitTree : StaticBody2D, IInteractable
{
	[Export] public int MaxStages = 3;
	
	private Sprite2D _sprite;
	private Timer _growthTimer;
	private Sprite2D _collectIcon;
	private int _currentStage = 0;
	private int _startFrame = 1;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_growthTimer = GetNode<Timer>("GrowthTimer");
		_collectIcon = GetNode<Sprite2D>("CollectIcon");

		_collectIcon.Visible = false;
		_sprite.Frame = _startFrame;

		_growthTimer.Timeout += OnGrowthTimeout;
	}

	private void OnGrowthTimeout()
	{
		if (_currentStage < MaxStages - 1)
		{
			_currentStage++;
			_sprite.Frame = _startFrame + _currentStage;

			if (_currentStage == MaxStages - 1)
			{
				_collectIcon.Visible = true;
				_growthTimer.Stop();
			}
		}
	}

	public void CollectFruit(Player player)
	{
		if (_currentStage == MaxStages - 1)
		{
			GD.Print("Ovoce sebráno!");
			player.AddMoney(100);
			_currentStage = 0;
			_sprite.Frame = _startFrame;
			_collectIcon.Visible = false;
			_growthTimer.Start();
		}
	}
	
	public void Interact(Player player) => CollectFruit(player);

	public void Destroy(Player player)
	{
		if (player._currentToolSuffix == "_axe")
		{
			GD.Print("Strom pokácen!");
			player.AddMoney(1250);
			QueueFree();
		}
		else{
			GD.Print("Na strom potřebuješ sekeru");
		}
	}
	
	public Godot.Collections.Dictionary<string, Variant> Save()
	{
		return new Godot.Collections.Dictionary<string, Variant>()
		{
			{ "Type", "FruitTree" },
			{ "PosX", GlobalPosition.X },
			{ "PosY", GlobalPosition.Y },
			{ "CurrentStage", _currentStage }
		};
	}
	
	public int GetCurrentStage() {return _currentStage; }
	
	public void LoadFromSave(int stage)
	{
		_currentStage = stage;
		
		if (_sprite != null) _sprite.Frame = _startFrame + _currentStage;
		if (_currentStage >= MaxStages - 1)  _collectIcon.Visible = true;
	}
}
