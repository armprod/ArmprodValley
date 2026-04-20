using Godot;
using System;
using System.Collections.Generic;

public partial class SettingsMenu : Control
{
	[Export] public HSlider VolumeSlider;
	[Export] public OptionButton ResOption;

	private List<Vector2I> resolutions = new List<Vector2I>
	{
		new Vector2I(1920, 1080),
		new Vector2I(1600, 900),
		new Vector2I(1280, 720),
		new Vector2I(1024, 768)
	};

	public override void _Ready()
	{
		// Nastavení rozlišení do OptionButtonu
		ResOption.Clear();
		foreach (var res in resolutions)
		{
			ResOption.AddItem($"{res.X}x{res.Y}");
		}

		// Propojení signálů kódem (jistota)
		VolumeSlider.ValueChanged += OnVolumeChanged;
		ResOption.ItemSelected += OnResolutionSelected;
	}

	private void OnVolumeChanged(double value)
	{
		float db = Mathf.LinearToDb((float)value); 
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), db);
	}

	private void OnResolutionSelected(long index)
	{
		Vector2I selectedRes = resolutions[(int)index];
		DisplayServer.WindowSetSize(selectedRes);
		
		// Vycentrování okna po změně
		Vector2I screenSize = DisplayServer.ScreenGetSize();
		DisplayServer.WindowSetPosition(screenSize / 2 - selectedRes / 2);
	}

	public void OnBackButtonPressed()
	{
		this.Hide(); // Jen schováme okno settings
	}
}
