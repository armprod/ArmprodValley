using Godot;
using System;

public partial class Fish : Sprite2D
{
	[Export] public float Speed = 50f;       // Rychlost ryby
	[Export] public float TopLimit = -24f;   // Horní hranice (v lokálních souřadnicích)
	[Export] public float BottomLimit = 24f; // Dolní hranice
	[Export] public float Agility = 2.0f;    // Jak často ryba mění směr (vyšší = neklidnější)

	private float _noiseTime = 0f;
	private FastNoiseLite _noise = new FastNoiseLite();

	public override void _Ready()
	{
		// Nastavení šumu pro náhodný, ale plynulý pohyb
		_noise.Seed = (int)GD.Randi();
		_noise.Frequency = 0.5f; 
		
		GD.Print("Plynulá ryba nastartována.");
	}

	public override void _PhysicsProcess(double delta)
	{
		_noiseTime += (float)delta * Agility;

		// Získání náhodného směru z šumu (-1 až 1)
		float moveDirection = _noise.GetNoise1D(_noiseTime);

		// Výpočet nové pozice
		Vector2 newPos = Position;
		newPos.Y += moveDirection * Speed * (float)delta;

		// !!! KLÍČOVÉ: Clamp ji udrží uvnitř baru !!!
		newPos.Y = Mathf.Clamp(newPos.Y, TopLimit, BottomLimit);

		// Pokud narazí na hranici, trochu ji to "odrazí", aby tam nestála
		if (newPos.Y == TopLimit || newPos.Y == BottomLimit)
		{
			_noiseTime += 10.0f; // Skočí v šumu jinam, aby změnila směr
		}

		Position = newPos;
	}
}
