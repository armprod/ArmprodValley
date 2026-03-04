using Godot;
using System;

public partial class FishBar : CanvasLayer
{
	private TextureProgressBar _progressBar;
	private Area2D _cursorArea; // Area na rybě
	private RigidBody2D _playerArea; // Zelený cursor

	[ExportGroup("Difficulty settings")]
	[Export] public float FillSpeed = 40f;   // Jak rychle bar roste
	[Export] public float DrainSpeed = 20f;  // Jak rychle bar klesá

	public override void _Ready()
	{
		_progressBar = GetNodeOrNull<TextureProgressBar>("MainContainer/TextureProgressBar");
		_cursorArea = GetNodeOrNull<Area2D>("MainContainer/fish/Area2D"); 
		_playerArea = GetNodeOrNull<RigidBody2D>("MainContainer/outside/GreenCursor");

		if (_progressBar != null) _progressBar.Value = 0;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_progressBar == null || _cursorArea == null || _playerArea == null) return;

		// Ptáme se Area2D (ryba), jestli se dotýká tělesa (GreenCursor)
		if (_cursorArea.OverlapsBody(_playerArea))
		{
			_progressBar.Value += FillSpeed * (float)delta;
		}
		else
		{
			_progressBar.Value -= DrainSpeed * (float)delta;
		}

		_progressBar.Value = Mathf.Clamp(_progressBar.Value, 0, 100);
	}
}
