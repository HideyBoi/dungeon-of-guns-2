using Godot;
using System;

public partial class TransitionAnimation : AnimationPlayer
{
	public Vector2 newPos;
	public void Transition(string direction, Vector2 pos) {
		Stop();
		Play(direction);
		newPos = pos;
 	}

	public void SetPosition() {	
		GetParent<Node2D>().GlobalPosition = newPos;
	}
}
