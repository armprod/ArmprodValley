using Godot;
using System;

public partial class SaveSlot : Button
{
	// Signál pro smazání
	[Signal] public delegate void DeleteRequestedEventHandler(int id);
	// NOVINKA: Signál pro výběr (načtení) slotu
	[Signal] public delegate void SlotSelectedEventHandler(int id);
	
	[Export] public Label TitleLabel;
	[Export] public Button DeleteButton;
	[Export] public LineEdit RenameInput;
	[Export] public Button RenameButton;

	private int _slotId;

	public override void _Ready()
	{
		// Propojení vlastního kliknutí (tlačítko samo na sebe)
		this.Pressed += () => EmitSignal(SignalName.SlotSelected, _slotId);

		// Propojení mazání
		if (DeleteButton != null)
			DeleteButton.Pressed += OnDeletePressed;
		
		if (RenameButton != null)
			RenameButton.Pressed += OnRenamePressed;

		if (RenameInput != null)
			RenameInput.TextSubmitted += OnRenameConfirmed;
	}
	
	private void OnRenamePressed()
	{
		// Schováme label, ukážeme políčko pro psaní
		TitleLabel.Visible = false;
		RenameInput.Visible = true;
		RenameInput.Text = TitleLabel.Text; // Předvyplníme stávající název
		RenameInput.GrabFocus(); // Automaticky tam skočí kurzor
	}

	private void OnRenameConfirmed(string newName)
	{
		// Tady musíme říct SaveManageru, aby změnil jméno v souboru
		SaveManager.Instance.RenameSave(_slotId, newName);
		
		// Vrátíme UI do normálu
		TitleLabel.Text = newName;
		TitleLabel.Visible = true;
		RenameInput.Visible = false;
	}

	private void OnDeletePressed()
	{
		EmitSignal(SignalName.DeleteRequested, _slotId);
	}

	public void SetData(int id, SaveData data)
	{
		_slotId = id;

		// --- POJISTKA PROTI PÁDU ---
		if (TitleLabel == null)
		{
			GD.PrintErr($"CHYBA: SaveSlot (ID: {id}) nemá v Inspektoru přiřazený TitleLabel!");
			return; // Ukončíme metodu, aby hra nespadla
		}
		// ---------------------------

		if (data != null)
		{
			TitleLabel.Text = $"{data.SaveName} ({data.Date})";
		}
		else
		{
			TitleLabel.Text = $"Slot {id}: Prázdný";
		}
		
		if (DeleteButton != null) 
		{
			DeleteButton.Visible = (data != null);
		}
	}
}
