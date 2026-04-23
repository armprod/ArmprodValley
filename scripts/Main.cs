using Godot;
using System;

public partial class Main : Node2D
{
	[Export] public TileMapLayer FarmLayer; // Přetáhni svůj TileMapLayer do inspektoru

	public override void _Ready()
	{
		if (SaveManager.Instance != null)
		{
			// 1. NEJDŘÍV propojíme uzly
			SaveManager.Instance.FarmingLayerNode = GetNodeOrNull<TileMapLayer>("FarmingLayer");
			SaveManager.Instance.PlantsLayerNode = GetNodeOrNull<TileMapLayer>("PlantsLayer");
			GD.Print("ÚSPĚCH: FarmingLayer propojen z Main.cs");

			// 2. TEPRVE TEĎ načteme hru pro aktuální slot
			int slotId = SaveManager.Instance.SelectedSlot; 
			SaveManager.Instance.LoadGame(slotId);
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Kontrola, zda hráč klikl levým tlačítkem myši
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			InteractWithTile(GetGlobalMousePosition());
		}
	}

	private void InteractWithTile(Vector2 globalPos)
	{
		// 1. Převede globální pozici myši na souřadnice mřížky (např. [5, 10])
		Vector2I tilePos = FarmLayer.LocalToMap(FarmLayer.ToLocal(globalPos));

		// 2. Získá data o konkrétní dlaždici
		TileData tileData = FarmLayer.GetCellTileData(tilePos);

		if (tileData != null)
		{
			// 3. Načte tvůj Custom Data Layer (v C# vrací Variant, musíme přetypovat)
			bool isFarmable = (bool)tileData.GetCustomData("can_till");

			if (isFarmable)
			{
				GD.Print($"Klikl jsi na políčko {tilePos} a dá se tu farmařit!");
				// Sem později přidáme kód pro změnu dlaždice na "vzoráno"
				// ChangeTile(tilePos);
			}
		}
	}
}
