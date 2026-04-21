using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	[Export] public Control MenuRoot;
	[Export] public Control SettingsMenuNode;
	[Export] public LineEdit SaveNameInput;

	public override void _Ready()
	{
		// DŮLEŽITÉ: CanvasLayer musí mít ProcessMode Always, 
		// aby fungoval, i když je hra stopnutá (Paused).
		ProcessMode = ProcessModeEnum.Always;
		
		if (MenuRoot != null) 
		{
			MenuRoot.Hide();
			// Pozadí nesmí blokovat myš, jinak neklikneš na tlačítka
			MenuRoot.MouseFilter = Control.MouseFilterEnum.Ignore;
		}
		this.Hide();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			// Zkontroluj, jestli se tvoje scéna menu jmenuje PŘESNĚ "MainMenu"
			if (GetTree().CurrentScene.Name == "MainMenu") return;
			
			TogglePause();
			GetViewport().SetInputAsHandled();
		}
	}

	public void TogglePause()
	{
		bool isPaused = !GetTree().Paused;
		GetTree().Paused = isPaused;
		
		if (isPaused)
		{
			this.Show();
			if (MenuRoot != null) MenuRoot.Show();
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else
		{
			if (MenuRoot != null) MenuRoot.Hide();
			this.Hide();
			// Pokud máš hru, kde se myš schovává, odkomentuj řádek níže:
			// Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	// --- TYTO METODY MUSÍŠ PROPOJIT V EDITORU (ZÁLOŽKA NODE) ---

	public void OnResumeButtonPressed()
	{
		GD.Print("Resume stisknuto");
		TogglePause();
	}

	public void OnSaveButtonPressed()
	{
		GD.Print("Pokus o uložení...");
		SaveData data = new SaveData();

		// --- NOVINKA: Získání názvu ---
		if (SaveNameInput != null)
		{
			string name = SaveNameInput.Text.Trim();
			// Pokud je políčko prázdné, dáme automatický název
			data.SaveName = string.IsNullOrEmpty(name) ? $"Uloženo {DateTime.Now:HH:mm}" : name;
		}
		// ------------------------------

		var playerNode = GetTree().CurrentScene.FindChild("Player", true, false) as Player;
		if (playerNode != null)
		{
			data.Money = playerNode.Money;
			data.PlayerPosition = playerNode.GlobalPosition;
		}

		var farmingNode = GetTree().CurrentScene.FindChild("FarmingLayer", true, false) as FarmingSystem;
		if (farmingNode != null)
		{
			data.FarmTiles = farmingNode.GetSaveData();
			GD.Print("Farma nalezena!");
		}

		if (SaveManager.Instance != null)
		{
			SaveManager.Instance.SaveGame(data, SaveManager.Instance.SelectedSlot);
			GD.Print($"HRA ÚSPĚŠNĚ ULOŽENA pod názvem: {data.SaveName}");
			
			// Po uložení políčko vymažeme, aby bylo čisté pro příště
			if (SaveNameInput != null) SaveNameInput.Text = "";
		}
		else
		{
			GD.PrintErr("SaveManager nenalezen!");
		}
	}
	
	public void OnSettingsButtonPressed()
	{
		GD.Print("Otevírám settings...");
		MenuRoot.Hide();
		SettingsMenuNode.Show();
	}
	
	public void OnSettingsMenuClosed()
	{
		GD.Print("Signál zachycen: Vracím se ze Settings do Pause Menu!");

		if (SettingsMenuNode != null)
		{
			SettingsMenuNode.Hide();
		}

		if (MenuRoot != null)
		{
			MenuRoot.Show();
			MenuRoot.MouseFilter = Control.MouseFilterEnum.Stop;
		}
	}

	public void OnQuitButtonPressed()
	{
		GD.Print("Ukončuji hru");
		GetTree().Paused = false;
		GetTree().Quit();
	}
}
