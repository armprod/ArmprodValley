using Godot;
using System;
using System.Text.Json;
using System.Collections.Generic;

public partial class SaveManager : Node
{
	public static SaveManager Instance { get; private set; }
	private string _saveFolder = "user://saves/";
	
	public TileMapLayer FarmingLayerNode { get; set; }
	public TileMapLayer PlantsLayerNode { get; set; }

	public bool IsLoadingQueued { get; set; } = false;
	public int SelectedSlot { get; set; } = 1;

	// Nastavení pro JSON, aby správně ukládal Vector2I a další data
	private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };

	public override void _Ready()
	{
		Instance = this;
		if (!DirAccess.DirExistsAbsolute(_saveFolder))
			DirAccess.MakeDirRecursiveAbsolute(_saveFolder);			
	}

	public string GetSavePath(int id) => $"{_saveFolder}save_{id}.json";
	public bool DoesSaveExist(int id) => FileAccess.FileExists(GetSavePath(id));

	public int GetNextFreeSlot()
	{
		int id = 1;
		while (DoesSaveExist(id)) id++;
		return id;
	}

	public void SaveGame(SaveData data, int id)
	{
		GD.Print($"--- POKUS O ULOŽENÍ (Slot {id}) ---");

		if (FarmingLayerNode == null)
		{
			FarmingLayerNode = GetTree().Root.FindChild("FarmingLayer", true, false) as TileMapLayer;
		}

		if (FarmingLayerNode is FarmingSystem farming)
		{
			// Metoda, která korektně ošetřuje i vyrostlé plodiny
			data.FarmTiles = farming.GetSaveData();
			GD.Print($"Uloženo {data.FarmTiles.Count} políček přes FarmingSystem.");
		}
		else if (FarmingLayerNode != null)
		{
			// Záložní logika, pokud by přetypování selhalo
			data.FarmTiles.Clear();
			foreach (Vector2I cell in FarmingLayerNode.GetUsedCells())
			{
				TileSaveData tData = new TileSaveData {
					Pos = cell,
					GroundAtlasPos = FarmingLayerNode.GetCellAtlasCoords(cell)
				};

				if (PlantsLayerNode != null && PlantsLayerNode.GetCellSourceId(cell) != -1)
				{
					tData.HasPlant = true;
					tData.PlantAtlasPos = PlantsLayerNode.GetCellAtlasCoords(cell);
				}
				data.FarmTiles.Add(tData);
			}
		}
		
		data.Beehives.Clear();
		var beehiveNodes = GetTree().GetNodesInGroup("beehives");
		GD.Print($"Nalezeno úlů k uložení: {beehiveNodes.Count}");

		foreach (Node node in beehiveNodes)
		{
			if (node is Beehive beehive)
			{
				data.Beehives.Add(new BeehiveSaveData {
					Pos = beehive.GlobalPosition,
					CurrentStage = beehive.GetCurrentStage()
				});
			}
		}
		
		data.FruitTrees.Clear();
		var treeNodes = GetTree().GetNodesInGroup("trees");
		GD.Print($"Nalezeno stromů k uložení: {treeNodes.Count}");

		foreach (Node node in treeNodes)
		{
			if (node is FruitTree tree)
			{
				data.FruitTrees.Add(new FruitTreesSaveData {
					Pos = tree.GlobalPosition,
					CurrentStage = tree.GetCurrentStage()
				});
			}
		}
		
		if (TimeManager.Instance != null)
		{
			data.CurrentDay = TimeManager.Instance.CurrentDay;
			data.TotalSeconds = TimeManager.Instance.TotalSeconds;
			GD.Print($"Ukládám čas do JSONu: Den {data.CurrentDay}, Sekundy {data.TotalSeconds}");
		}

		// Uložení do JSONu (nezapomeň na ty Options, jinak Vector2I nebude fungovat!)
		var options = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
		string jsonString = JsonSerializer.Serialize(data, options);
		using var file = FileAccess.Open(GetSavePath(id), FileAccess.ModeFlags.Write);
		if (file != null) file.StoreString(jsonString);
		
		GD.Print("--- KONEC UKLÁDÁNÍ ---");
	}

	public SaveData LoadGame(int id) 
	{
		string path = GetSavePath(id);
		if (!FileAccess.FileExists(path)) return null;

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		string jsonText = file.GetAsText();
		SaveData data = JsonSerializer.Deserialize<SaveData>(jsonText, _jsonOptions);

		if (data == null) return null;
		if (FarmingLayerNode == null) return data; 

		// Pokud jsme došli sem, začneme kreslit
		FarmingLayerNode.Clear();
		PlantsLayerNode?.Clear();

		foreach (var tile in data.FarmTiles)
		{
			FarmingLayerNode.SetCell(tile.Pos, 0, tile.GroundAtlasPos);
			
			if (tile.HasPlant)
			{
				PlantsLayerNode?.SetCell(tile.Pos, 1, tile.PlantAtlasPos);
				
				if (FarmingLayerNode is FarmingSystem farming)
				{
					// OPRAVA: Předáváme PlantAge, nikoliv StartTime
					farming.LoadTrackedPlant(tile.Pos, tile.PlantAge);
				}
			}
		}
		
		foreach (Node node in GetTree().GetNodesInGroup("beehives")) node.QueueFree();
		PackedScene beehiveScene = GD.Load<PackedScene>("res://scenes/PlaceableObjects/Beehive.tscn");
		foreach (var bData in data.Beehives)
		{
			var newBeehive = beehiveScene.Instantiate<Beehive>();
			FarmingLayerNode.GetParent().AddChild(newBeehive);
			newBeehive.GlobalPosition = bData.Pos;
			newBeehive.LoadFromSave(bData.CurrentStage); 
		}
		
		foreach (Node node in GetTree().GetNodesInGroup("trees")) node.QueueFree();
		PackedScene treeScene = GD.Load<PackedScene>("res://scenes/PlaceableObjects/FruitTree.tscn");
		foreach (var tData in data.FruitTrees)
		{
			var newTree = treeScene.Instantiate<FruitTree>();
			FarmingLayerNode.GetParent().AddChild(newTree);
			newTree.GlobalPosition = tData.Pos;
			newTree.LoadFromSave(tData.CurrentStage); 
		}
		
		if (TimeManager.Instance != null)
			TimeManager.Instance.LoadTime(data.CurrentDay, data.TotalSeconds);

		GD.Print($"ÚSPĚCH: Načteno {data.FarmTiles.Count} políček, {data.Beehives.Count} úlů a {data.FruitTrees.Count} stromů.");
		return data;
	}

	public void RenameSave(int slotId, string newName)
	{
		SaveData data = LoadGame(slotId);
		if (data != null)
		{
			data.SaveName = newName;
			SaveGame(data, slotId); 
		}
	}

	public void DeleteSave(int id)
	{
		string path = GetSavePath(id);
		if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(path);
	}
}
