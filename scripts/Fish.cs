using Godot;
using System;

public partial class Fish : Node2D 
{
	[Export] public float Speed = 150f;
	private float _targetY;
	private float _timer = 0;
	private Random _random = new Random();

	public override void _PhysicsProcess(double delta)
	{
		_timer -= (float)delta;
		if (_timer <= 0)
		{
			_targetY = (float)(_random.NextDouble() * 160 - 80); // Rozsah -80 až 80
			_timer = (float)(_random.NextDouble() * 1.5f + 0.5f);
		}

		Position = new Vector2(Position.X, Mathf.MoveToward(Position.Y, _targetY, Speed * (float)delta));
	}
}
