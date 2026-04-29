using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] public string GameScenePath = "res://scenes/Main.tscn";
	[Export] public string LoadMenuScenePath = "res://scenes/MenuScenes/LoadGameMenu.tscn";
	
	[Export] public Control SettingsMenuNode;
	
	[Export] public Button NewGameButton;
	[Export] public Button LoadGameButton;
	[Export] public Button SettingsButton;
	[Export] public Button QuitButton;

	public override void _Ready()
	{
		if (NewGameButton != null) NewGameButton.Pressed += OnNewGamePressed;
		if (LoadGameButton != null) LoadGameButton.Pressed += OnLoadGamePressed;
		if (SettingsButton != null) SettingsButton.Pressed += OnSettingsPressed;
		if (QuitButton != null) QuitButton.Pressed += OnQuitPressed;
		
		if (SettingsMenuNode != null)
		{
			SettingsMenuNode.Hide();
		}
		
		GD.Print("MainMenu inicializováno.");
	}

	private void OnNewGamePressed()
	{
		if (SaveManager.Instance != null)
		{
			// Najde první číslo, které ještě neexistuje (např. save_3.json)
			int freeSlot = SaveManager.Instance.GetNextFreeSlot();
			SaveManager.Instance.SelectedSlot = freeSlot; 
			SaveManager.Instance.IsLoadingQueued = false;
			
			GD.Print($"Startujeme novou hru ve slotu: {freeSlot}");
		}
		GetTree().ChangeSceneToFile(GameScenePath);
	}

	private void OnLoadGamePressed()
	{
		// Místo kontroly jednoho souboru prostě přepneme do menu slotů
		if (ResourceLoader.Exists(LoadMenuScenePath))
		{
			GetTree().ChangeSceneToFile(LoadMenuScenePath);
		}
		else
		{
			GD.PrintErr("Chyba: Scéna pro Load Menu nebyla nalezena na: " + LoadMenuScenePath);
		}
	}

	private void OnSettingsPressed()
	{
		if (SettingsMenuNode != null)
		{
			SettingsMenuNode.Show();
		}
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
