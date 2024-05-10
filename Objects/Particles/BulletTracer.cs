using Godot;
using System;

public partial class BulletTracer : Line2D
{
	public void Setup(Vector2 start, Vector2 end) {
		ClearPoints();
		AddPoint(start);
		AddPoint(end);
		GetNode<AnimationPlayer>("./AnimationPlayer").Play("FadeOut");
	}

	public void EndAnimation() {
		QueueFree();
	}
}
