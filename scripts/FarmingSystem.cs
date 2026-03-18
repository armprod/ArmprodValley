using Godot;
using System;

public partial class FarmingSystem : Node2D
{
	[Export] public TileMapLayer GroundLayer;   // Tady je tráva
	[Export] public TileMapLayer FarmingLayer;  // Sem budeme kreslit hlínu
	
	// Souřadnice v Atlasu pro okopanou hlínu (uprav podle svého TileSetu)
	private Vector2I _tilledDirtCoords = new Vector2I(9, 1); 
	private int _selectedSlot = 1;

	public override void _Input(InputEvent @event)
	{
		// 1. Přepínání slotů (1-5)
		for (int i = 1; i <= 5; i++)
		{
			if (Input.IsActionJustPressed("slot_" + i)) // Musíš mít v Input Map (slot_1 atd.)
			{
				_selectedSlot = i;
				GD.Print("Vybrán slot: " + _selectedSlot);
			}
		}

		// 2. Použití motyky (předpokládáme, že motyka je ve slotu 1)
		if (Input.IsActionJustPressed("action_use") && _selectedSlot == 1)
		{
			TillGround();
		}
	}

	private void TillGround()
	{
		// Získáme pozici myši v mapě
		Vector2 mousePos = GetGlobalMousePosition();
		Vector2I gridPos = GroundLayer.LocalToMap(mousePos);

		// Zkontrolujeme, jestli je na GroundLayer tráva, která jde okopat
		TileData tileData = GroundLayer.GetCellTileData(gridPos);
		
		if (tileData != null && (bool)tileData.GetCustomData("can_till"))
		{
			// Pokud tam ještě není hlína, "vykopeme" ji
			if (FarmingLayer.GetCellSourceId(gridPos) == -1) // -1 znamená prázdno
			{
				// Nastavíme dlaždici hlíny (0 je ID zdroje v atlasu)
				FarmingLayer.SetCell(gridPos, 0, _tilledDirtCoords);
				GD.Print("Půda okopána na: " + gridPos);
			}
		}
	}
}
