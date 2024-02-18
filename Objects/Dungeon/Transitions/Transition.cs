using Godot;
using System;

public partial class Transition : Node2D
{
	[Export] int sideId;

	[Signal] public delegate void OnTransitionEventHandler(int id, Node2D body, Vector2 origin);

	void OnEntered(Node2D body) {
		EmitSignal(SignalName.OnTransition, sideId, body, GlobalPosition);
	}
}
