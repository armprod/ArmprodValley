using Godot;
using System;

public class TileSaveData
{
	public Vector2I Pos { get; set; }
	public Vector2I GroundAtlasPos { get; set; } // Pro FarmingLayer
	public Vector2I PlantAtlasPos { get; set; }  // Pro PlantsLayer
	public bool HasPlant { get; set; }
	public ulong PlantStartTime { get; set; }
}
