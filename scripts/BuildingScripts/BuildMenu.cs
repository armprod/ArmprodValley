using Godot;
using System;

public partial class BuildMenu : CanvasLayer
{
	// Signál, který vyšleme do světa, když hráč klikne na budovu
	[Signal] public delegate void BuildingSelectedEventHandler(string scenePath, int price);
	
	private int _beehivePrice = 500;
	private int _fruitTreePrice = 1500;

	public override void _Ready()
	{
		Hide(); // Menu je na začátku schované
	}
	
	public override void _Input(InputEvent @event)
	{
		if (Visible && @event.IsActionPressed("ui_cancel"))
		{
			ToggleMenu(); // Zavře menu
			GetViewport().SetInputAsHandled(); // ZASTAVÍ šíření signálu k Pause Menu
		}
	}

	// Metoda pro otevření/zavření (volá Player s kladivem)
	public void ToggleMenu()
	{
		Visible = !Visible;
		Input.MouseMode = Input.MouseModeEnum.Visible; 
	}

	// Tuto metodu napojíme na 'pressed' signál tvého Beehive tlačítka
	public void OnBeehiveButtonPressed()
	{
		GD.Print("Tlačítko stisknuto!");
		// Nejdříve uvolníme myš, aby s ní šlo hýbat ve světě
		Input.MouseMode = Input.MouseModeEnum.Visible; 
		
		EmitSignal(SignalName.BuildingSelected, "res://scenes/PlaceableObjects/Beehive.tscn", _beehivePrice); 
		
		// ToggleMenu už v sobě má logiku pro Visible/Captured, 
		// tak si pohlídej, aby se po zavření menu myš nezasekla.
		ToggleMenu();
	}
	
	public void OnFruitTreeButtonPressed()
	{
		GD.Print("Tlačítko stisknuto!");
		Input.MouseMode = Input.MouseModeEnum.Visible;
		EmitSignal(SignalName.BuildingSelected, "res://scenes/PlaceableObjects/FruitTree.tscn", _fruitTreePrice);
		ToggleMenu();
	}
}
