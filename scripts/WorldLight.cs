using Godot;
using System;

public partial class WorldLight : CanvasModulate
{
	// Definujeme barvy pro jednotlivé fáze dne
	[Export] public Color NightColor = new Color(0.15f, 0.15f, 0.35f); // Tmavě modrá
	[Export] public Color SunsetColor = new Color(1.0f, 0.7f, 0.5f);   // Oranžová
	[Export] public Color DayColor = new Color(1.0f, 1.0f, 1.0f);     // Bílá (normální)
	[Export] public Color SunriseColor = new Color(1.0f, 0.9f, 0.7f);  // Nažloutlá

	public override void _Process(double delta)
	{
		if (TimeManager.Instance == null) return;

		// Získáme aktuální čas jako jedno desetinné číslo (např. 14.5 pro 14:30)
		float time = TimeManager.Instance.GetFloatTime();
		
		UpdateColor(time);
	}

	private void UpdateColor(float time)
	{
		if (time >= 0 && time < 5) // Hluboká noc
			Color = NightColor;
		else if (time >= 5 && time < 8) // Svítání
			Color = NightColor.Lerp(SunriseColor, (time - 5) / 3.0f);
		else if (time >= 8 && time < 17) // Den
			Color = DayColor;
		else if (time >= 17 && time < 20) // Západ slunce
			Color = DayColor.Lerp(SunsetColor, (time - 17) / 3.0f);
		else if (time >= 20 && time < 22) // Stmívání
			Color = SunsetColor.Lerp(NightColor, (time - 20) / 2.0f);
		else // Noc
			Color = NightColor;
	}
}
