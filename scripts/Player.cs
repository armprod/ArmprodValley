using Godot;
using System;

public partial class Player : CharacterBody2D
{
	// ***** FISHING *****
	// Stavy pro přehlednost
	public enum FishingState { None, HoldingRod, Casting, WaitingForBite }
	public FishingState CurrentFishingState = FishingState.None;

	[Export] public bool HasFishingRod = true; // Pro testování nastaveno na true
	[Export] public float Speed = 100.0f;

	private AnimationPlayer _animPlayer;
	private Sprite2D _sprite;
	
	
	public override void _Ready()
	{
		// Najdeme AnimationPlayer a Sprite2D při startu
		_animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		
	}

	public override void _PhysicsProcess(double delta)
	{
		// 1. Získání směru pohybu (W, S, A, D nebo šipky)
		Vector2 velocity = Velocity;
		Vector2 direction = Input.GetVector("left", "right", "up", "down");

		if (direction != Vector2.Zero)
		{
			velocity = direction * Speed;
			
			// 2. Spuštění správné animace podle směru
			UpdateAnimation(direction);
		}
		else
		{
			velocity = velocity.MoveToward(Vector2.Zero, Speed);
			
			// 3. Pokud hráč stojí, animaci zastavíme (nebo přepneme na Idle)
			_animPlayer.Stop(); 
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void UpdateAnimation(Vector2 dir)
	{
		// Logika výběru animace
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
		{
			// Horizontální pohyb (Doleva/Doprava)
			if (dir.X > 0)
				_animPlayer.Play("walk_right");
			else
				_animPlayer.Play("walk_left");
		}
		else
		{
			// Vertikální pohyb (Nahoru/Dolů)
			if (dir.Y > 0)
				_animPlayer.Play("walk_down");
			else
				_animPlayer.Play("walk_up");
		}
	}

	public override void _Input(InputEvent @event)
{
	// 1. Zmáčknutí klávesy '1'
	if (@event.IsActionPressed("slot_one"))
	{
		// Název animace musí být v uvozovkách!
		_animPlayer.Play("equip_rod"); 
	}

	// 2. Levé tlačítko myši - Nahození (pouze pokud držím prut)
	if (@event.IsActionPressed("left_click") && CurrentFishingState == FishingState.HoldingRod)
	{
		StartCasting();
	}
}

	private void ToggleFishingRod()
	{
		if (CurrentFishingState == FishingState.None)
		{
			CurrentFishingState = FishingState.HoldingRod;
			_animPlayer.Play("RESET"); // Animace, kde hráč vytáhne prut
			GD.Print("Prut připraven v ruce.");
		}
		else if (CurrentFishingState == FishingState.HoldingRod)
		{
			CurrentFishingState = FishingState.None;
			_animPlayer.Play("Idle"); // Schová prut
			GD.Print("Prut schován.");
		}
	}

	private void StartCasting()
	{
		CurrentFishingState = FishingState.Casting;
		_animPlayer.Play("CastRod"); // Tvoje animace nahození
		GD.Print("Nahazuji...");

		// Po skončení animace nahození se přepneme do čekání
		// To můžeš vyřešit signálem AnimationFinished jako minule
	}
}
