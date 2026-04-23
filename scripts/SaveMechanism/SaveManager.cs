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

		// KONTROLA 1: Máme propojený uzel?
		if (FarmingLayerNode == null)
		{
			FarmingLayerNode = GetTree().Root.FindChild("FarmingLayer", true, false) as TileMapLayer;
		}

		if (FarmingLayerNode != null)
		{
			data.FarmTiles.Clear();
			var cells = FarmingLayerNode.GetUsedCells();
			GD.Print($"Nalezeno políček k uložení: {cells.Count}");

			foreach (Vector2I cell in cells)
			{
				TileSaveData tData = new TileSaveData {
					Pos = cell,
					GroundAtlasPos = FarmingLayerNode.GetCellAtlasCoords(cell)
				};

				// KONTROLA 2: Máme rostliny?
				if (PlantsLayerNode != null && PlantsLayerNode.GetCellSourceId(cell) != -1)
				{
					tData.HasPlant = true;
					tData.PlantAtlasPos = PlantsLayerNode.GetCellAtlasCoords(cell);
				}
				data.FarmTiles.Add(tData);
			}
		}
		else
		{
			GD.PrintErr("KRITICKÁ CHYBA: FarmingLayerNode nebyl nalezen ani po záchranném pokusu!");
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
		if (!FileAccess.FileExists(path)) 
		{
			return null;
		}

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		string jsonText = file.GetAsText();
		SaveData data = JsonSerializer.Deserialize<SaveData>(jsonText, _jsonOptions);

		if (data == null)
		{
			GD.PrintErr("CHYBA: Nepodařilo se přeložit JSON (data jsou null)!");
			return null;
		}

		if (FarmingLayerNode == null)
		{
			return data; 
		}

		// Pokud jsme došli sem, začneme kreslit
		FarmingLayerNode.Clear();
		PlantsLayerNode?.Clear();

		foreach (var tile in data.FarmTiles)
		{
			// Změň tu 1 na 0, pokud se stále nic neobjevuje!
			FarmingLayerNode.SetCell(tile.Pos, 0, tile.GroundAtlasPos);
			
			if (tile.HasPlant)
			{
				PlantsLayerNode?.SetCell(tile.Pos, 0, tile.PlantAtlasPos);
			}
		}

		GD.Print($"ÚSPĚCH: Načteno {data.FarmTiles.Count} políček z disku.");
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
