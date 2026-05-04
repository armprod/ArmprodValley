using Godot;
using System;

public partial class LoadGameMenu : Control
{
	// Tady v Inspektoru přetáhni scénu SaveSlot.tscn
	[Export] public PackedScene SaveSlotScene;
	[Export] public VBoxContainer SlotContainer;
	[Export] public Button BackButton;

	public override void _Ready()
	{
		if (BackButton != null)
		{
			BackButton.Pressed += OnBackButtonPressed;
		}
		RefreshSlots();
	}

	public void RefreshSlots()
	{
		// 1. Vyčistíme stará tlačítka
		foreach (Node child in SlotContainer.GetChildren())
		{
			child.QueueFree();
		}

		// 2. Procházíme sloty
		for (int i = 1; i <= 3; i++)
		{
			SaveSlot slotBtn = SaveSlotScene.Instantiate<SaveSlot>();
			
			// Načteme data
			SaveData data = SaveManager.Instance.LoadGame(i);
			slotBtn.SetData(i, data);
			
			// PROPOJENÍ NAČÍTÁNÍ
			slotBtn.SlotSelected += OnSlotSelected;
			
			slotBtn.DeleteRequested += (id) => {
				SaveManager.Instance.DeleteSave(id);
				RefreshSlots(); // Překreslí menu po smazání
			};
			
			SlotContainer.AddChild(slotBtn);
		}
	}

	private void OnSlotSelected(int id)
	{
		if (SaveManager.Instance.DoesSaveExist(id))
		{
			GD.Print($"Načítám slot {id}...");
			SaveManager.Instance.SelectedSlot = id;
			SaveManager.Instance.IsLoadingQueued = true;
			
			// Tady změň cestu na svou hlavní scénu světa
			GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
		}
		else
		{
			GD.Print("Tento slot je prázdný, nemůžu načíst.");
		}
	}

	public void OnBackButtonPressed()
	{
		// Vrátíme se do hlavního menu
		GetTree().ChangeSceneToFile("res://scenes/MenuScenes/MainMenu.tscn");
	}
}
