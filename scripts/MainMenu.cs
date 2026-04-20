using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] public string GameScenePath = "res://scenes/Main.tscn";
	
	// Místo GetNode použijeme Export - pak jen uzel přetáhneš v Inspektoru
	[Export] public Control SettingsMenuNode;

	public override void _Ready()
	{
		// Tlačítka v menu - ujisti se, že cesty v GetNode sedí s tvým VBoxem!
		GetNode<Button>("VBoxContainer/NewGameButton").Pressed += OnNewGamePressed;
		GetNode<Button>("VBoxContainer/LoadGameButton").Pressed += OnLoadGamePressed;
		GetNode<Button>("VBoxContainer/SettingsButton").Pressed += OnSettingsPressed;
		GetNode<Button>("VBoxContainer/QuitButton").Pressed += OnQuitPressed;
		
		// Schováme settings při startu, pokud jsou přiřazeny
		if (SettingsMenuNode != null)
		{
			SettingsMenuNode.Hide();
		}
		
		GD.Print("MainMenu inicializováno.");
	}

	private void OnNewGamePressed()
	{
		GetTree().ChangeSceneToFile(GameScenePath);
	}

	private void OnLoadGamePressed()
	{
		if (FileAccess.FileExists("user://savegame.save"))
		{
			// Pozor: Ujisti se, že tvůj SaveManager je v projektu jako Autoload!
			if (SaveManager.Instance != null)
			{
				SaveManager.Instance.IsLoadingQueued = true;
				GetTree().ChangeSceneToFile(GameScenePath);
			}
		}
	}

	private void OnSettingsPressed()
	{
		if (SettingsMenuNode != null)
		{
			SettingsMenuNode.Show();
		}
		else
		{
			GD.PrintErr("Chyba: SettingsMenuNode není přiřazeno v Inspektoru MainMenu!");
		}
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
