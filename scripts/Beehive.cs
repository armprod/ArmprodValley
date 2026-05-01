using Godot;
using System;

public partial class Beehive : StaticBody2D
{
	[Export] public int MaxStages = 4;
	
	private Sprite2D _sprite;
	private Timer _growthTimer;
	private Sprite2D _collectIcon;
	private int _currentStage = 0;
	private int _startFrame = 1; // Frame, kde začínají včely

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_growthTimer = GetNode<Timer>("GrowthTimer");
		_collectIcon = GetNode<Sprite2D>("CollectIcon");

		_collectIcon.Visible = false; // Na začátku schováme ikonu
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
				_collectIcon.Visible = true; // Med je hotov, ukážeme ikonu
				_growthTimer.Stop();
			}
		}
	}

	public void HarvestHoney(Player player)
	{
		if (_currentStage == MaxStages - 1)
		{
			GD.Print("Med vybrán!");
			player.AddMoney(100);
			_currentStage = 0;
			_sprite.Frame = _startFrame;
			_collectIcon.Visible = false; // Schováme ikonu po sběru
			_growthTimer.Start();
		}
	}

	public void Destroy(Player player)
	{
		GD.Print("Úl zničen!");
		player.AddMoney(5000);
		QueueFree();
	}
	
	public Godot.Collections.Dictionary<string, Variant> Save()
	{
		return new Godot.Collections.Dictionary<string, Variant>()
		{
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
