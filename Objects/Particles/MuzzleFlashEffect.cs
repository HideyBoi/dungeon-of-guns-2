using Godot;
using System;

public partial class MuzzleFlashEffect : AnimatedSprite2D
{

	public void Setup(Vector2 pos, float rotation, Weapon.MuzzleFlashSize flashSize) {
		GlobalPosition = pos;
		GlobalRotation = rotation; 

		switch (flashSize)
		{
			case Weapon.MuzzleFlashSize.Light:
				Play("light");
				break;
			case Weapon.MuzzleFlashSize.Medium:
				Play("medium");
				break;
			case Weapon.MuzzleFlashSize.Heavy:
				Play("heavy");
				break;
		}

		timer = GetTree().CreateTimer(0.1);
		timer.Timeout += Done;
	}

	SceneTreeTimer timer;

	void Done() {
		timer.Timeout -= Done;
		QueueFree();
	}
}
