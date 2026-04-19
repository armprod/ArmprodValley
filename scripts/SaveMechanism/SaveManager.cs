using Godot;
using System;
using System.Text.Json;
using System.Collections.Generic;

public partial class SaveManager : Node
{
	public static SaveManager Instance { get; private set; }
	private string _savePath = "user://savegame.json";

	// Tato proměnná chyběla (řeší chyby v MainMenu a Player)
	public bool IsLoadingQueued { get; set; } = false;

	public override void _Ready()
	{
		Instance = this;
	}

	public void SaveGame(SaveData data)
	{
		try 
		{
			string jsonString = JsonSerializer.Serialize(data);
			// Musíme použít Godot.FileAccess, aby se to nepletlo se System.IO
			using var file = Godot.FileAccess.Open(_savePath, Godot.FileAccess.ModeFlags.Write);
			if (file != null)
			{
				file.StoreString(jsonString);
			}
		}
		catch (Exception e)
		{
			GD.PrintErr("Chyba při ukládání: " + e.Message);
		}
	}

	// Přidali jsme parametr 'id', aby to sedělo s voláním v Player.cs
	public SaveData LoadGame(int id = 0) 
	{
		if (!Godot.FileAccess.FileExists(_savePath)) return null;

		using var file = Godot.FileAccess.Open(_savePath, Godot.FileAccess.ModeFlags.Read);
		string jsonString = file.GetAsText();
		
		return JsonSerializer.Deserialize<SaveData>(jsonString);
	}
}
