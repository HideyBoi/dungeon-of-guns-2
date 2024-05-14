using Godot;
using System;

public partial class DamageIndicator : Marker2D
{
    public override void _EnterTree()
    {
        GetNode<AnimationPlayer>("./AnimationPlayer").Play("Show");
    }

	public void OnAnimationDone() {
		QueueFree();
	}
}
