using Godot;
using System;

public interface IInteractable
{
	void Interact(Player player);
	void Destroy(Player player);
	Vector2 GlobalPosition { get; }
}
