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
	private Vector2I _dryDirtCoords = new Vector2I(9, 1); 
	private Vector2I _wetDirtCoords = new Vector2I(8, 1); 
	private Vector2I _plantCoords = new Vector2I(0, 1); // Semínko v druhém atlasu
	
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

	// 1. KROK: ZORÁNÍ (Vytvoří suchou hlínu)
	private void HandleTilling(Vector2 mousePos)
	{
		Vector2I tilePos = GroundLayer.LocalToMap(GroundLayer.ToLocal(mousePos));
		TileData tileData = GroundLayer.GetCellTileData(tilePos);

		if (tileData != null)
		{
			var canTill = tileData.GetCustomData("can_till");
			if (canTill.VariantType != Variant.Type.Nil && (bool)canTill)
			{
				// Přemalujeme trávu na SUCHOU hlínu
				SetCell(tilePos, _terrainSourceId, _dryDirtCoords); 
				GD.Print("1. Pole zoráno (suchá hlína).");
			}
		}
	}

	// 2. KROK: SÁZENÍ (Vyžaduje zoráno)
	private void HandlePlanting(Vector2 mousePos)
	{
		if (PlantsLayer == null) return;
		Vector2I tilePos = LocalToMap(ToLocal(mousePos));

		// Kontrola: Je pod námi jakákoliv hlína (suchá nebo mokrá)?
		Vector2I currentGround = GetCellAtlasCoords(tilePos);
		bool isTilled = (currentGround == _dryDirtCoords || currentGround == _wetDirtCoords);

		if (isTilled)
		{
			// Kontrola: Je tam volno pro rostlinu?
			if (PlantsLayer.GetCellSourceId(tilePos) == -1)
			{
				PlantsLayer.SetCell(tilePos, _plantsSourceId, _plantCoords);
				GD.Print("2. Semínko zasazeno do připravené hlíny.");
				
				//if (Player is Player p) p.PlayPlantingAnimation();
			}
		}
		else 
		{
			GD.Print("CHYBA: Nejdřív musíš pole zorat motykou!");
		}
	}

	// 3. KROK: ZALÉVÁNÍ (Změní hlínu na mokrou)
	private void WaterGround(Vector2 mousePos)
	{
		// Souřadnice na mapě
		Vector2I tilePos = LocalToMap(ToLocal(mousePos));
		
		// Zjistíme, co je na tomto místě za terén (ve FarmingSystem vrstvě)
		Vector2I currentGround = GetCellAtlasCoords(tilePos);

		// KONTROLA: Je tam suchá hlína? (Nezáleží na tom, jestli je tam semínko)
		if (currentGround == _dryDirtCoords)
		{
			// Přemalujeme na mokrou hlínu
			SetCell(tilePos, _terrainSourceId, _wetDirtCoords);
			GD.Print("Pole bylo úspěšně zalito (voda vsákla do hlíny).");
			
			// Zde můžeš přidat efekt stříknutí vody
		}
		else if (currentGround == _wetDirtCoords)
		{
			GD.Print("Tady už je zalito, neplýtvej vodou!");
		}
		else
		{
			GD.Print("Tady není hlína, kterou bys mohl zalít (voda se jen vsákne do trávy).");
		}
	}
}
