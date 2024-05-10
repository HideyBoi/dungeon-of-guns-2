using Godot;
using System;

public partial class MuzzleFlashEffect : AnimatedSprite2D
{

	public void Setup(Vector2 pos, float rotation, Weapon.MuzzleFlashSize flashSize) {
		GlobalPosition = pos;
		GlobalRotation = rotation; 

		switch (flashSize)
		{
			default:
				Play("default");
				break;
		}

		timer = GetTree().CreateTimer(0.1);
		timer.Timeout += Done;
	}

	SceneTreeTimer timer;

	void Done() {
		QueueFree();
	}
}
