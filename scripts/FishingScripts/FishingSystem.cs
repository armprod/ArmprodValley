using Godot;
using System;

public partial class FishingSystem : Node2D
{
	[Signal] public delegate void StateChangedEventHandler(string newState);

	public enum FishingState { None, HoldingRod, Casting, WaitingForBite, FishBiting, FishingMiniGame }
	public FishingState CurrentState = FishingState.None;

	[Export] public float MinWaitTime = 2.0f;
	[Export] public float MaxWaitTime = 6.0f;
	[Export] public float ReactionWindow = 1.5f;
	[Export] public PackedScene FishingBarScene;

	private Timer _fishingTimer;
	private Timer _reactionTimer;
	private Player _player;

	public override void _Ready()
	{
		_player = GetParent<Player>();

		_fishingTimer = new Timer { OneShot = true };
		_fishingTimer.Timeout += OnFishBiteStarts; // Modernější zápis v C#
		AddChild(_fishingTimer);

		_reactionTimer = new Timer { OneShot = true };
		_reactionTimer.Timeout += OnFishEscaped;
		AddChild(_reactionTimer);
	}

	public void HandleInput()
	{
		if (_player == null) return;

		switch (CurrentState)
		{
			case FishingState.HoldingRod:
				// POZOR: IsFacingWater musí být v Player.cs označen jako PUBLIC
				if (_player.IsFacingWater()) StartCasting();
				break;
			case FishingState.WaitingForBite:
				ResetFishing();
				break;
			case FishingState.FishBiting:
				_reactionTimer.Stop();
				StartFishingBar();
				break;
		}
	}

	private void StartCasting()
	{
		CurrentState = FishingState.Casting;
		EmitSignal(SignalName.StateChanged, "cast");
	}

	public void OnCastAnimationFinished()
	{
		CurrentState = FishingState.WaitingForBite;
		_fishingTimer.Start((float)GD.RandRange(MinWaitTime, MaxWaitTime));
	}

	private void OnFishBiteStarts()
	{
		CurrentState = FishingState.FishBiting;
		EmitSignal(SignalName.StateChanged, "bite");
		_reactionTimer.Start(ReactionWindow);
	}

	private void OnFishEscaped()
	{
		ResetFishing();
	}

	private void StartFishingBar()
	{
		if (FishingBarScene == null) 
		{
			GD.PrintErr("Chyba: FishingBarScene není přiřazena v Inspectoru!");
			ResetFishing();
			return;
		}
		CurrentState = FishingState.FishingMiniGame;
		var bar = FishingBarScene.Instantiate();
		GetTree().Root.AddChild(bar);
	}

	public void ResetFishing()
	{
		CurrentState = FishingState.HoldingRod;
		EmitSignal(SignalName.StateChanged, "idle");
	}
}
