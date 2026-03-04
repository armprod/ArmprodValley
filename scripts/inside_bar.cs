using Godot;
using System;

public partial class inside_bar : RigidBody2D
{
	// Zvedli jsme hodnotu, aby přetlačila odpor 10.0f
	[Export] public float LiftForce = 1200f; 

	public override void _Ready()
	{
		this.LinearDamp = 10.0f; 
		this.LockRotation = true; // Drží kurzor rovně
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsActionPressed("left_click"))
		{
			// CentralForce tlačí kontinuálně, směr nahoru je záporné Y
			ApplyCentralForce(new Vector2(0, -LiftForce));
		}
	}
}
