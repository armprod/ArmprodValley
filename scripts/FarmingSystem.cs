using Godot;
using System;

public partial class FarmingSystem : TileMapLayer
{
	[Export] public TileMapLayer GroundLayer;
	// Sem v Inspectoru přetáhni svého hráče (CharacterBody2D)
	[Export] public Node2D Player; 

	// Maximální vzdálenost v pixelech (pokud má tvá dlaždice 16px, 3 bloky = cca 48-60px)
	[Export] public float MaxInteractionDistance = 60.0f; 

	private Vector2I _tilledDirtCoords = new Vector2I(8, 1);
	private int _sourceId = 0;
	private int _selectedSlot = 1;

	public override void _Input(InputEvent @event)
	{
		// Přepínání slotů zůstává stejné
		for (int i = 1; i <= 5; i++)
		{
			if (Input.IsActionJustPressed("slot_" + i))
			{
				_selectedSlot = i;
			}
		}

		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			if (_selectedSlot == 5)
			{
				TillGround(GetGlobalMousePosition());
			}
		}
	}

	private void TillGround(Vector2 mouseGlobalPos)
	{
		if (GroundLayer == null || Player == null) 
		{
			GD.Print("Chybí GroundLayer nebo Player v Inspectoru!");
			return;
		}

		// 1. VÝPOČET VZDÁLENOSTI
		float distance = Player.GlobalPosition.DistanceTo(mouseGlobalPos);

		if (distance > MaxInteractionDistance)
		{
			GD.Print($"Příliš daleko! Vzdálenost: {distance}, Limit: {MaxInteractionDistance}");
			return; // Hráč je moc daleko, nic se nestane
		}

		// 2. LOGIKA OKOPÁVÁNÍ (stejná jako předtím)
		Vector2I tilePos = GroundLayer.LocalToMap(GroundLayer.ToLocal(mouseGlobalPos));
		TileData tileData = GroundLayer.GetCellTileData(tilePos);

		if (tileData != null)
		{
			var canTill = tileData.GetCustomData("can_till");
			if (canTill.VariantType != Variant.Type.Nil && (bool)canTill)
			{
				if (GetCellSourceId(tilePos) == -1)
				{
					SetCell(tilePos, _sourceId, _tilledDirtCoords);
					GD.Print("Okopáno v dosahu!");
				}
			}
		}
	}
}
