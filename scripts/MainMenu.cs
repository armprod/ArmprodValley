using Godot;
using System;

public partial class MainMenu : Control
{
	// Cesta k hlavní scéně tvé hry
	[Export] public string GameScenePath = "res://scenes/Main.tscn";
	
	private Control _settingsPanel;

	public override void _Ready()
	{
		var newGameBtn = GetNode<Button>("VBoxContainer/NewGameButton");
		var loadGameBtn = GetNode<Button>("VBoxContainer/LoadGameButton");
		var settingsBtn = GetNode<Button>("VBoxContainer/SettingsButton");
		var quitBtn = GetNode<Button>("VBoxContainer/QuitButton");

		// 2. Propojíme signály pomocí C# eventů (+=)
		newGameBtn.Pressed += OnNewGamePressed;
		loadGameBtn.Pressed += OnLoadGamePressed;
		settingsBtn.Pressed += OnSettingsPressed;
		quitBtn.Pressed += OnQuitPressed;
		
		GD.Print("Menu inicializováno a tlačítka propojena.");
		
		_settingsPanel = GetNode<Control>("SettingsPanel");
		GetNode<Button>("SettingsPanel/CloseSettingsButton").Pressed += () => _settingsPanel.Hide();
	}

	private void OnNewGamePressed()
	{
		GD.Print("Startuji novou hru...");
		GetTree().ChangeSceneToFile(GameScenePath);
	}

	private void OnLoadGamePressed()
	{
		if (FileAccess.FileExists("user://savegame.save"))
		{
			SaveManager.Instance.IsLoadingQueued = true;
			GetTree().ChangeSceneToFile(GameScenePath);
		}
		else
		{
			GD.Print("Save nenalezen!");
		}
	}

	private void OnSettingsPressed()
	{
		GD.Print("Otevírám nastavení...");
		_settingsPanel.Show();
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
