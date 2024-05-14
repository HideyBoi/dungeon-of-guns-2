using Godot;
using System;
using Riptide;

public partial class RemotePlayer : StaticBody2D
{
	ushort pId;
	float lastX;
	Vector2 lastPos;
	public void SetupPlayer(ushort myId) {
		pId = myId;
	}

    public override void _PhysicsProcess(double delta)
    {
		/*
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
		*/

		if (totalTickDamageAmount > 0) {
			TickDamage();
		}
    }

	void TickDamage() {
		Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.DamagePlayer);
		msg.AddUShort(NetworkManager.I.Client.Id);
		msg.AddUShort(pId);
		msg.AddUShort(lastDamagedByItemId);
		msg.AddInt(totalTickDamageAmount);
		NetworkManager.I.Client.Send(msg);

		totalTickDamageAmount = 0;
	}

	ushort lastDamagedByItemId;
	int totalTickDamageAmount;

	public void DamageRemotePlayer(ushort itemId, int damageAmount) {
		lastDamagedByItemId = itemId;
		totalTickDamageAmount += damageAmount;
	}

    [MessageHandler((ushort)NetworkManager.MessageIds.PlayerPosRot)]
    public static void HandlePlayerPosRot(Message msg) {
		ushort id = msg.GetUShort();

		// If this player doesn't exist yet, don't.
		if (!GameManager.PlayingPlayers.ContainsKey(id))
		{
			return;
		}		
		
		Vector2 pos = msg.GetVector2();
        GameManager.PlayingPlayers[id].playerNode.GlobalPosition = pos;
    }
}
