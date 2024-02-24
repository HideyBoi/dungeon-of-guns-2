using Godot;
using System;
using Riptide;

public partial class RemotePlayer : Node2D
{
	ushort pId;
	float lastX;
	Vector2 lastPos;
	[Export] AnimatedSprite2D playerSprite;

	public void SetupPlayer(ushort myId) {
		pId = myId;
	}

    public override void _PhysicsProcess(double delta)
    {
		Vector2 currentMoveDir = Position - lastPos;

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

		lastPos = Position;
    }

    [MessageHandler((ushort)NetworkManager.MessageIds.PlayerPosRot)]
    public static void HandlePlayerPosRot(Message msg) {
		ushort id = msg.GetUShort();

		// If this player doesn't exist yet, don't.
		if (!GameManager.PlayingPlayers.ContainsKey(id))
		{
			return;
		}		
		
		float x = msg.GetFloat();
		float y = msg.GetFloat();
        GameManager.PlayingPlayers[id].playerNode.GlobalPosition = new(x, y);
    }
}
