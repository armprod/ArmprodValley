using Godot;
using System;
using System.Collections.Generic;

public class SaveData
{
	public string SaveName { get; set; } = "New Farm";
	public string Date { get; set; } = "";
	public List<TileSaveData> FarmTiles { get; set; } = new List<TileSaveData>();
	public int Money { get; set; }
	public Vector2 PlayerPosition { get; set; }
	public List<BeehiveSaveData> Beehives { get; set; } = new List<BeehiveSaveData>();
	public List<FruitTreesSaveData> FruitTrees { get; set; } = new List<FruitTreesSaveData>();
}

public class BeehiveSaveData
{
	public Vector2 Pos { get; set; }
	public int CurrentStage { get; set; }
}

public class FruitTreesSaveData
{
	public Vector2 Pos {get; set; }
	public int CurrentStage { get; set; }
}
