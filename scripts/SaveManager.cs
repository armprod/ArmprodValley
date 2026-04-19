using Godot;
using System;

public partial class SaveManager : Node
{
	public static SaveManager Instance { get; private set; }
	public bool IsLoadingQueued = false;

	public override void _Ready()
	{
		Instance = this;
	}

	public void SaveGame(Player player, int money)
	{
		using var saveFile = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Write);

		var saveData = new Godot.Collections.Dictionary<string, Variant>()
		{
			{ "Money", money },
			{ "PosX", player.GlobalPosition.X },
			{ "PosY", player.GlobalPosition.Y },
			// Sem přidej další věci (inventář, stav políček atd.)
		};

		var jsonString = Json.Stringify(saveData);
		saveFile.StoreLine(jsonString);
		GD.Print("Hra uložena!");
	}

	public void LoadGame(Player player)
	{
		if (!FileAccess.FileExists("user://savegame.save")) return;

		using var saveFile = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Read);
		var jsonString = saveFile.GetLine();
		var json = new Json();
		var parseResult = json.Parse(jsonString);

		if (parseResult == Error.Ok)
		{
			var data = new Godot.Collections.Dictionary<string, Variant>((Godot.Collections.Dictionary)json.Data);
			
			// Načtení dat do hráče
			player.GlobalPosition = new Vector2((float)data["PosX"], (float)data["PosY"]);
			player.AddMoney((int)data["Money"]); // Musíš mít tuhle metodu v Player.cs
			
			GD.Print("Hra načtena!");
		}
		IsLoadingQueued = false;
	}
}
