using Godot;
using System;

public partial class inside_bar : RigidBody2D
{
	// Tato hodnota určuje, jak moc to bude "plavat"
	// Pokud je to moc pomalé, zkus -40 nebo -50
	// Změň z -30 na -60 nebo -80, dokud bar nezačne svižně stoupat
	[Export] public float LiftForce = -50f;

	public override void _Ready()
	{
		// KLÍČ K PLYNULOSTI: Nastavíme odpor přímo v kódu
		// Čím vyšší číslo, tím méně bude bar odskakovat a bude plynulejší
		this.LinearDamp = 10.0f; 
	}

	public override void _PhysicsProcess(double delta)
	{
		// Používáme IsActionPressed, aby bar stoupal plynule po celou dobu držení
		if (Input.IsActionPressed("left_click"))
		{
			// Aplikujeme malý impuls 60x za sekundu
			ApplyImpulse(new Vector2(0, LiftForce));
		}
	}
}
