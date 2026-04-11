using Godot;
using System;

[GlobalClass]
public partial class FarmingSystem : TileMapLayer
{
	[Export] public TileMapLayer GroundLayer;
	[Export] public TileMapLayer PlantsLayer;
	[Export] public Node2D Player; 
	
	[Export] private int _terrainSourceId = 0; // Atlas se zemí
	[Export] private int _plantsSourceId = 1;  // Atlas s rostlinami

	[Export] public float MaxInteractionDistance = 60.0f; 

	// Souřadnice z atlasu
	private Vector2I _dryDirtCoords = new Vector2I(8, 1); 
	private Vector2I _wetDirtCoords = new Vector2I(9, 1); 
	private Vector2I _plantCoords = new Vector2I(0, 0); // Semínko v druhém atlasu
	
	private int _selectedSlot = 1;

	public override void _Input(InputEvent @event)
{
	for (int i = 0; i <= 9; i++) // Začínáme od 0
	{
		string actionName = "slot_" + i;
		if (InputMap.HasAction(actionName)) 
		{
			if (Input.IsActionJustPressed(actionName))
			{
				_selectedSlot = i;
				GD.Print("Vybrán slot: " + _selectedSlot);
			}
		}
	}

	if (Input.IsActionJustPressed("action_use"))
	{
		Vector2 mousePos = GetGlobalMousePosition();

		if (Player != null && Player.GlobalPosition.DistanceTo(mousePos) > MaxInteractionDistance)
		{
			GD.Print("Příliš daleko!");
			return;
		}

		// Logika podle vybraného slotu
		if (_selectedSlot == 4) 
		{
			HandleTilling(mousePos);
		}
		else if (_selectedSlot == 5) 
		{
			WaterGround(mousePos);
		}
		else if (_selectedSlot == 0)
		{
			GD.Print("Sázím semínka slot 0");
			HandlePlanting(mousePos);
		}
	}
}

	private void WaterGround(Vector2 mousePos)
	{
		Vector2I tilePos = LocalToMap(ToLocal(mousePos));
		if (GetCellAtlasCoords(tilePos) == _dryDirtCoords)
		{
			// Použijeme _terrainSourceId (země)
			SetCell(tilePos, _terrainSourceId, _wetDirtCoords);
			GD.Print("Půda zalita.");
		}
	}

	private void HandlePlanting(Vector2 mousePos)
	{
		if (PlantsLayer == null) return;
		Vector2I tilePos = LocalToMap(ToLocal(mousePos));

		// Kontrola, jestli je pod námi hlína (v tvém případě mokrá i suchá, jak jsme chtěli)
		Vector2I groundTile = GetCellAtlasCoords(tilePos);
		if (groundTile == _wetDirtCoords || groundTile == _dryDirtCoords)
		{
			if (PlantsLayer.GetCellSourceId(tilePos) == 1)
			{
				// DŮLEŽITÉ: Použijeme _plantsSourceId pro rostliny!
				PlantsLayer.SetCell(tilePos, _plantsSourceId, _plantCoords);
				GD.Print("Zasazeno semínko!");
			}
		}
		else 
		{
			GD.Print("Tady není vykopaná hlína.");
		}
	}
	
	private void HandleTilling(Vector2 mousePos)
	{
		Vector2I tilePos = GroundLayer.LocalToMap(GroundLayer.ToLocal(mousePos));
		TileData tileData = GroundLayer.GetCellTileData(tilePos);

		if (tileData != null)
		{
			var canTill = tileData.GetCustomData("can_till");
			if (canTill.VariantType != Variant.Type.Nil && (bool)canTill)
			{
				// Použijeme _terrainSourceId (země)
				SetCell(tilePos, _terrainSourceId, _dryDirtCoords); 
				GD.Print($"ÚSPĚCH: Kopu na {tilePos}");
			}
		}
	}
}
