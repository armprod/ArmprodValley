using Godot;
using System;
using System.Text.Json;
using System.Collections.Generic;

public partial class SaveManager : Node
{
	public static SaveManager Instance { get; private set; }
	
	// Místo jednoho souboru definujeme složku
	private string _saveFolder = "user://saves/";

	public bool IsLoadingQueued { get; set; } = false;

	// Pomocná proměnná pro uchování vybraného slotu při přechodu mezi scénami
	public int SelectedSlot { get; set; } = 1;

	public override void _Ready()
	{
		Instance = this;
		
		// Vytvoříme složku pro savy, pokud neexistuje
		if (!DirAccess.DirExistsAbsolute(_saveFolder))
		{
			DirAccess.MakeDirRecursiveAbsolute(_saveFolder);
		}
	}

	// Pomocná funkce pro sestavení cesty k souboru podle ID slotu
	private string GetSavePath(int id)
	{
		return $"{_saveFolder}save_{id}.json";
	}
	
	public int GetNextFreeSlot()
	{
		int id = 1;
		while (DoesSaveExist(id))
		{
			id++;
		}
		return id;
	}

	// Upravené ukládání s parametrem ID
	public void SaveGame(SaveData data, int id)
	{
		try 
		{
			// Do dat přidáme aktuální čas a název, aby to UI mohlo zobrazit
			data.Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
			
			string jsonString = JsonSerializer.Serialize(data);
			string path = GetSavePath(id);

			using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
			if (file != null)
			{
				file.StoreString(jsonString);
				GD.Print($"Hra uložena do slotu {id} na cestu: {path}");
			}
		}
		catch (Exception e)
		{
			GD.PrintErr("Chyba při ukládání: " + e.Message);
		}
	}

	// Upravené načítání – teď už ID skutečně používá
	public SaveData LoadGame(int id) 
	{
		string path = GetSavePath(id);

		if (!Godot.FileAccess.FileExists(path)) 
		{
			GD.Print($"Save ve slotu {id} neexistuje.");
			return null;
		}

		using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
		string jsonString = file.GetAsText();
		
		return JsonSerializer.Deserialize<SaveData>(jsonString);
	}

	// Funkce pro UI: Zjistí, jestli slot existuje
	public bool DoesSaveExist(int id)
	{
		return Godot.FileAccess.FileExists(GetSavePath(id));
	}
	
	public void RenameSave(int slotId, string newName)
	{
		SaveData data = LoadGame(slotId);
		if (data != null)
		{
			data.SaveName = newName;
			// Tady byla ta chyba - ujisti se, že ID je první!
			SaveGame(data, slotId); 
		}
	}
	
	public void DeleteSave(int id)
	{
		string path = GetSavePath(id);
		if (Godot.FileAccess.FileExists(path))
		{
			// DirAccess umí mazat soubory přes Remove
			DirAccess.RemoveAbsolute(path);
			GD.Print($"Save {id} byl smazán.");
		}
	}
}
