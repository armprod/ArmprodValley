using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class FarmingSystem : TileMapLayer
{
	[Export] public TileMapLayer GroundLayer;
	[Export] public TileMapLayer PlantsLayer;
	[Export] public Node2D Player; 
	
	[Export] private int _terrainSourceId = 0; // Atlas se zemí
	[Export] private int _plantsSourceId = 1;  // Atlas s rostlinami

	[Export] public float MaxInteractionDistance = 60.0f; 

	[Export] public float GrowthStageTimeSeconds = 60.0f;
	private int _maxStages = 6;
	private Dictionary<Vector2I, ulong> _trackedPlants = new();

	// Souřadnice z atlasu
	private Vector2I _dryDirtCoords = new Vector2I(9, 1); 
	private Vector2I _wetDirtCoords = new Vector2I(8, 1); 
	private Vector2I _plantCoords = new Vector2I(0, 1); // První fáze (semínko) v druhém atlasu
	
	private int _selectedSlot = 1;

	public override void _Process(double delta)
	{
		CheckPlantGrowth();
	}

	public override void _Input(InputEvent @event)
	{
		for (int i = 0; i <= 9; i++) 
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
			if (_selectedSlot == 2) // Pickaxe
				HandleMineGround(mousePos);
			if (_selectedSlot == 4) // Hoe
				HandleTilling(mousePos);
			else if (_selectedSlot == 5) // Watering can
				WaterGround(mousePos);
			else if (_selectedSlot == 0) // Seed
				HandlePlanting(mousePos);
			else if (_selectedSlot == 6) // Scythe
				HandleHarvesting(mousePos);
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
				SetCell(tilePos, _terrainSourceId, _dryDirtCoords); 
				GD.Print("1. Pole zoráno.");
			}
		}
	}

	private void HandlePlanting(Vector2 mousePos)
	{
		if (PlantsLayer == null) return;
		Vector2I tilePos = LocalToMap(ToLocal(mousePos));

		Vector2I currentGround = GetCellAtlasCoords(tilePos);
		bool isTilled = (currentGround == _dryDirtCoords || currentGround == _wetDirtCoords);

		if (isTilled)
		{
			if (PlantsLayer.GetCellSourceId(tilePos) == -1)
			{
				PlantsLayer.SetCell(tilePos, _plantsSourceId, _plantCoords);
				// Zapsání času zasazení
				_trackedPlants[tilePos] = Time.GetTicksMsec();
				GD.Print("2. Semínko zasazeno a začalo růst.");
			}
		}
		else 
		{
			GD.Print("CHYBA: Nejdřív musíš pole zorat!");
		}
	}

	private void WaterGround(Vector2 mousePos)
	{
		Vector2I tilePos = LocalToMap(ToLocal(mousePos));
		Vector2I currentGround = GetCellAtlasCoords(tilePos);

		if (currentGround == _dryDirtCoords)
		{
			SetCell(tilePos, _terrainSourceId, _wetDirtCoords);
			GD.Print("3. Pole zalito.");
		}
	}

	private void CheckPlantGrowth()
	{
		if (_trackedPlants.Count == 0) return;

		ulong currentTime = Time.GetTicksMsec();
		List<Vector2I> completedPlants = new List<Vector2I>();

		foreach (var plant in _trackedPlants)
		{
			Vector2I pos = plant.Key;
			ulong startTime = plant.Value;

			double secondsPassed = (currentTime - startTime) / 1000.0;
			int currentStage = (int)(secondsPassed / GrowthStageTimeSeconds);

			if (currentStage >= _maxStages - 1)
			{
				UpdatePlantTile(pos, _maxStages - 1);
				completedPlants.Add(pos);
			}
			else
			{
				UpdatePlantTile(pos, currentStage);
			}
		}

		foreach (var pos in completedPlants)
		{
			_trackedPlants.Remove(pos);
		}
	}

	private void UpdatePlantTile(Vector2I pos, int stage)
	{
		// Posuneme X souřadnici v atlasu o číslo fáze
		Vector2I stageCoords = new Vector2I(_plantCoords.X + stage, _plantCoords.Y);
		
		if (PlantsLayer.GetCellAtlasCoords(pos) != stageCoords)
		{
			PlantsLayer.SetCell(pos, _plantsSourceId, stageCoords);
		}
	}

	private void HandleHarvesting(Vector2 mousePos)
	{
		Vector2I tilePos = PlantsLayer.LocalToMap(PlantsLayer.ToLocal(mousePos));
		Vector2I currentCoords = PlantsLayer.GetCellAtlasCoords(tilePos);
		
		// Finální fáze je 5. dlaždice v řadě (index 5)
		Vector2I finalStageCoords = new Vector2I(_plantCoords.X + 5, _plantCoords.Y);

		if (currentCoords == finalStageCoords)
		{
			PlantsLayer.SetCell(tilePos, -1); // Smazat rostlinu
			SetCell(tilePos, _terrainSourceId, _dryDirtCoords); // Vrátit hlínu na suchou
			GD.Print("Sklizeno kosou! Pole je opět připraveno.");
			if (Player is Player p) 
			{
				p.AddMoney(25); 
				GD.Print("Peníze přidány!");
			}
		}
		else if (PlantsLayer.GetCellSourceId(tilePos) != -1)
		{
			GD.Print("Ještě to není zralé!");
		}
	}
	
	private void HandleMineGround(Vector2 mousePos)
	{
		Vector2I tilePos = LocalToMap(ToLocal(mousePos));

		if (GetCellSourceId(tilePos) != -1) 
		{
			SetCell(tilePos, -1); 
			GD.Print("Pole bylo odstraněno krumpáčem.");
		}

		if (PlantsLayer.GetCellSourceId(tilePos) != -1)
		{
			PlantsLayer.SetCell(tilePos, -1);
			
			if (_trackedPlants.ContainsKey(tilePos))
			{
				_trackedPlants.Remove(tilePos);
			}
			GD.Print("Rostlina na zničeném poli byla odstraněna.");
		}
	}
	
	public List<TileSaveData> GetSaveData()
	{
		var list = new List<TileSaveData>();
		
		foreach (Vector2I pos in GroundLayer.GetUsedCells())
		{
			TileSaveData tile = new TileSaveData();
			tile.Pos = pos;
			tile.GroundAtlasPos = GroundLayer.GetCellAtlasCoords(pos);
			tile.HasPlant = _trackedPlants.ContainsKey(pos);
			tile.PlantStartTime = tile.HasPlant ? _trackedPlants[pos] : 0;
			
			list.Add(tile);
		}
		return list;
	}
}
