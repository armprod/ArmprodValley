using Godot;
using System;

public partial class FishBar : CanvasLayer
{
	private TextureProgressBar _progressBar;
	private Area2D _cursorArea; 
	private AnimationPlayer _uiAnim;

	[Export] public float FillSpeed = 40f;   // Rychlost přičítání
	[Export] public float DrainSpeed = 30f;  // Rychlost ODČÍTÁNÍ

	public override void _Ready()
	{
		// Tento řádek vynutí, aby se fyzika ryby restartovala na pozici baru
		var fish = GetNode<RigidBody2D>("MainContainer/fish");
		fish.GlobalPosition = GetNode<Control>("MainContainer").GlobalPosition;
	
		AnimationPlayer anim = GetNode<AnimationPlayer>("AnimationPlayer");
	
		if (anim != null && anim.HasAnimation("fishing_bar_appear"))
		{
			anim.Play("fishing_bar_appear"); // Spustí animaci vyjetí
		}
	
		_progressBar = GetNodeOrNull<TextureProgressBar>("TextureProgressBar");
		// Cesta k tvojí Area2D na zeleném kurzoru
		_cursorArea = GetNodeOrNull<Area2D>("fish/Area2D"); 

		if (_progressBar != null)
		{
			_progressBar.Value = 0;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_progressBar == null || _cursorArea == null) return;

		// Zjistíme kolik věcí se právě teď dotýkáme
		int count = _cursorArea.GetOverlappingAreas().Count + _cursorArea.GetOverlappingBodies().Count;

		if (count > 0)
		{
			// Dotýkáme se aspoň jedné věci (ryby)
			_progressBar.Value += FillSpeed * (float)delta;
		}
		else
		{
			// Nedotýkáme se NIČEHO
			_progressBar.Value -= DrainSpeed * (float)delta;
		}

		_progressBar.Value = Mathf.Clamp(_progressBar.Value, 0, 100);
	}
}
