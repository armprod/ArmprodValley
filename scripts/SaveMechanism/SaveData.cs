using Godot;
using System;
using System.Collections.Generic;

public class SaveData
{
	// Hráč
	public int Money { get; set; }
	public Vector2 PlayerPosition { get; set; }

	// Farma (tvůj seznam políček)
	// C# automaticky použije tu třídu TileSaveData, co jsi vytvořil předtím
	public List<TileSaveData> FarmTiles { get; set; } = new List<TileSaveData>();
}
