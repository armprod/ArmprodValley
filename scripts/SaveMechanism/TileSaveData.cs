using Godot;
using System;

public class TileSaveData
{
	public Vector2I Pos { get; set; }
	public Vector2I GroundAtlasPos { get; set; }
	public bool HasPlant { get; set; }
	public ulong PlantStartTime { get; set; }
}
