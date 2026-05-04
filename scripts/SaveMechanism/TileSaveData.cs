using Godot;
using System;
using System.Collections.Generic;

public class TileSaveData
{
	public Vector2I Pos { get; set; }
	public Vector2I GroundAtlasPos { get; set; }
	public Vector2I PlantAtlasPos { get; set; }
	public bool HasPlant { get; set; }
	public int PlantStage { get; set; }
	public float PlantAge { get; set; }
}
