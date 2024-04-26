using Godot;
using System;
using Riptide;

public partial class LocalPlayer : CharacterBody2D
{
	ushort pId;
	[Export] float speed;
	[Export] float speedChange = 7;
	float lastX = 0;
	enum PlayerMoveState  { MOVING };
	PlayerMoveState currentState;
	[Export] AnimatedSprite2D playerSprite;

	public void SetupPlayer(ushort myId) {
		pId = myId;
		#if !DEBUG
		DisplayServer.MouseSetMode(DisplayServer.MouseMode.Confined);
		#endif
	}

	public override void _PhysicsProcess(double delta)
	{
		switch (currentState)
		{
			case PlayerMoveState.MOVING:
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

		Vector2 mousePos = GetLocalMousePosition();
		
		if (mousePos.X >= 0)  {
			playerSprite.Scale = new(1, 1);
		}
		else {
			playerSprite.Scale = new(-1, 1);
		}
			
		MoveAndSlide();

		Message posRot = Message.Create(MessageSendMode.Unreliable, NetworkManager.MessageIds.PlayerPosRot);
		posRot.AddUShort(pId);
		posRot.AddVector2(GlobalPosition);
		NetworkManager.I.Client.Send(posRot);
	}
}
