using Godot;
using System;
using System.Collections.Generic;

public class SaveData
{
	public string SaveName { get; set; } = "New Farm";
	public string Date { get; set; } = "";
	public List<TileSaveData> FarmTiles { get; set; } = new List<TileSaveData>();
	
	// Herní data
	public int Money { get; set; }
	public Vector2 PlayerPosition { get; set; }
	// Přidej další věci (Level, Inventář, Stav farmy...)
}
