using Godot;
using System;

public partial class LocalPlayer : CharacterBody2D
{
	[Export] float speed;
	[Export] float speedChange = 7;
	[Export] AnimatedSprite2D playerSprite;
	float lastX = 0;
	enum CurrentState  { MOVING };
	CurrentState currentState;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		switch (currentState)
		{
			case CurrentState.MOVING:
				TickMove(delta);
				break;
		}	
	}

	void TickMove(double delta) {
		Vector2 currentMoveDir;
		currentMoveDir.X = Input.GetAxis("move_left", "move_right");
		currentMoveDir.Y = Input.GetAxis("move_up", "move_down");
		currentMoveDir = currentMoveDir.Normalized();

		float _speedChange = (float)delta * speedChange;

		Vector2 vel = Vector2.Zero;
		vel.X = Mathf.Lerp(Velocity.X, currentMoveDir.X * speed, _speedChange);
		vel.Y = Mathf.Lerp(Velocity.Y, currentMoveDir.Y * speed, _speedChange);

		Velocity = vel;

		if (currentMoveDir.X != 0)
			lastX = currentMoveDir.X;

		if (currentMoveDir.Length() > 0) {
			if (lastX > 0) {
				playerSprite.Play("Walk-R");
			} else {
				playerSprite.Play("Walk-L");
			}	
		} else {
			if (lastX > 0) {
				playerSprite.Play("Idle-R");
			} else {
				playerSprite.Play("Idle-L");
			}
		}

		MoveAndSlide();
	}
}
