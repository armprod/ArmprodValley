using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 100.0f;

	private AnimationPlayer _animPlayer;
	private Sprite2D _sprite;

	public override void _Ready()
	{
		// Najdeme AnimationPlayer a Sprite2D při startu
		_animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite = GetNode<Sprite2D>("Sprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		// 1. Získání směru pohybu (W, S, A, D nebo šipky)
		Vector2 velocity = Velocity;
		Vector2 direction = Input.GetVector("left", "right", "up", "down");

		if (direction != Vector2.Zero)
		{
			velocity = direction * Speed;
			
			// 2. Spuštění správné animace podle směru
			UpdateAnimation(direction);
		}
		else
		{
			velocity = velocity.MoveToward(Vector2.Zero, Speed);
			
			// 3. Pokud hráč stojí, animaci zastavíme (nebo přepneme na Idle)
			_animPlayer.Stop(); 
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void UpdateAnimation(Vector2 dir)
	{
		// Logika výběru animace
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
		{
			// Horizontální pohyb (Doleva/Doprava)
			if (dir.X > 0)
				_animPlayer.Play("walk_right");
			else
				_animPlayer.Play("walk_left");
		}
		else
		{
			// Vertikální pohyb (Nahoru/Dolů)
			if (dir.Y > 0)
				_animPlayer.Play("walk_down");
			else
				_animPlayer.Play("walk_up");
		}
	}
}
