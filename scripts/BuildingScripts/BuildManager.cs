using Godot;
using System;

public partial class BuildManager : Node
{
	[Export] public PackedScene BeehiveScene; // Přetáhni Beehive.tscn do Inspektoru
	
	private Node2D _ghostBuilding;
	private bool _isBuildingMode = false;

	public override void _Process(double delta)
	{
		if (_isBuildingMode && _ghostBuilding != null)
		{
			// Duch následuje myš (ve světových souřadnicích)
			Vector2 mousePos = GetViewport().GetMousePosition();
			// Pokud máš kameru, použij: GetGlobalMousePosition()
			_ghostBuilding.GlobalPosition = mousePos;

			// Potvrzení stavby levým kliknutím
			if (Input.IsActionJustPressed("action_use"))
			{
				PlaceBuilding();
			}
			
			// Zrušení stavby pravým kliknutím
			if (Input.IsActionJustPressed("ui_cancel")) // Např. klávesa Esc nebo pravá myš
			{
				CancelBuilding();
			}
		}
	}

	// Tuhle metodu propojíme se signálem z BuildMenu
	public void OnBuildingSelected(string scenePath)
	{
		// Pokud už nějakého ducha máme, smažeme ho
		if (_ghostBuilding != null) 
		{
			_ghostBuilding.QueueFree();
		}

		// Načteme scénu úlu
		var scene = GD.Load<PackedScene>(scenePath);
		_ghostBuilding = (Node2D)scene.Instantiate();
		
		// --- VYPNUTÍ KOLIZÍ PRO DUCHA ---
		// Prohledáme všechny děti a vypneme CollisionShape2D, aby duch do ničeho nenarážel
		foreach (var child in _ghostBuilding.FindChildren("*", "CollisionShape2D", true))
		{
			if (child is CollisionShape2D shape)
			{
				shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
			}
		}

		// Pokud je samotný duch StaticBody2D nebo Area2D, vypneme jeho vrstvy
		if (_ghostBuilding is CollisionObject2D collisionObject)
		{
			collisionObject.CollisionLayer = 0;
			collisionObject.CollisionMask = 0;
		}
		// --------------------------------

		// Uděláme ho poloprůhledným a zelenkavým (jako hologram)
		_ghostBuilding.Modulate = new Color(0.3f, 1.0f, 0.3f, 0.5f); 
		
		// Nastavíme ZIndex vysoko, aby byl duch nad hráčem a stromy
		_ghostBuilding.ZIndex = 100;
		
		// Přidáme ho do hlavní scény
		GetTree().CurrentScene.AddChild(_ghostBuilding);
		_isBuildingMode = true;
		
		GD.Print("Duch budovy vytvořen bez kolizí.");
	}

	private void PlaceBuilding()
	{
		// Vytvoříme skutečný úl na místě ducha
		var finalBuilding = (Node2D)BeehiveScene.Instantiate();
		finalBuilding.GlobalPosition = _ghostBuilding.GlobalPosition;
		
		GetTree().CurrentScene.AddChild(finalBuilding);

		// Uklidíme
		CancelBuilding();
		GD.Print("Úl byl postaven!");
	}

	private void CancelBuilding()
	{
		if (_ghostBuilding != null) _ghostBuilding.QueueFree();
		_ghostBuilding = null;
		_isBuildingMode = false;
	}
}
