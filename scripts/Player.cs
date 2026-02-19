using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public enum FishingState { None, HoldingRod, Casting, WaitingForBite }
	public FishingState CurrentFishingState = FishingState.None;

	[Export] public float Speed = 100.0f;

	private AnimationPlayer _animPlayer;
	private Vector2 _lastDirection = Vector2.Down; // Výchozí pohled dolů

	public override void _Ready()
	{
		_animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Input.GetVector("left", "right", "up", "down");
		Vector2 velocity = Velocity;

		if (direction != Vector2.Zero)
		{
			_lastDirection = direction; // Uložíme si, kam hráč naposledy šel
			velocity = direction * Speed;
			UpdateAnimation(direction);
		}
		else
		{
			velocity = velocity.MoveToward(Vector2.Zero, Speed);
			PlayIdleAnimation(); // Když stojí, pustíme Idle (s prutem nebo bez)
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void UpdateAnimation(Vector2 dir)
	{
		string suffix = (CurrentFishingState == FishingState.HoldingRod) ? "_rod" : "";
		
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
		{
			if (dir.X > 0) _animPlayer.Play("walk_right" + suffix);
			else _animPlayer.Play("walk_left" + suffix);
		}
		else
		{
			if (dir.Y > 0) _animPlayer.Play("walk_down" + suffix);
			else _animPlayer.Play("walk_up" + suffix);
		}
	}

	private void PlayIdleAnimation()
	{
		string suffix = (CurrentFishingState == FishingState.HoldingRod) ? "_rod" : "";
		
		// Vybere Idle animaci podle toho, kam se hráč naposledy díval
		if (Mathf.Abs(_lastDirection.X) > Mathf.Abs(_lastDirection.Y))
		{
			if (_lastDirection.X > 0) _animPlayer.Play("idle_right" + suffix);
			else _animPlayer.Play("idle_left" + suffix);
		}
		else
		{
			if (_lastDirection.Y > 0) _animPlayer.Play("idle_down" + suffix);
			else _animPlayer.Play("idle_up" + suffix);
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Přepínání prutu klávesou 1
		if (@event.IsActionPressed("slot_one"))
		{
			ToggleFishingRod();
		}

		// Nahození
		if (@event.IsActionPressed("left_click") && CurrentFishingState == FishingState.HoldingRod)
		{
			StartCasting();
		}
	}

	private void ToggleFishingRod()
	{
		if (CurrentFishingState == FishingState.None)
		{
			CurrentFishingState = FishingState.HoldingRod;
			GD.Print("Prut vytažen");
		}
		else
		{
			CurrentFishingState = FishingState.None;
			GD.Print("Prut schován");
		}
		// Okamžitě aktualizujeme animaci postavy, aby se vizuálně změnil postoj
		PlayIdleAnimation();
	}

	private void StartCasting()
	{
		CurrentFishingState = FishingState.Casting;
		
		// Určíme, na jakou stranu nahodit
		string dirName = "down";
		if (Mathf.Abs(_lastDirection.X) > Mathf.Abs(_lastDirection.Y))
			dirName = _lastDirection.X > 0 ? "right" : "left";
		else
			dirName = _lastDirection.Y > 0 ? "down" : "up";

		_animPlayer.Play("cast_" + dirName);
		GD.Print("Nahazuji na stranu: " + dirName);
	}
}
