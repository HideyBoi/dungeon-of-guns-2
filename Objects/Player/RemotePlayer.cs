using Godot;
using System;
using Riptide;

public partial class RemotePlayer : StaticBody2D
{
	public ushort pId;
	float lastX;
	Vector2 lastPos;
	[Export] AnimatedSprite2D animatedSprite;
	[Export] CollisionShape2D collision;
	[Export] Node2D gunRotationPoint;
	[Export] Node2D gunRoot;
	[Export] Sprite2D gunSprite;
	int currentGunId = 0;
	bool hasGun = false;

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
		msg.AddVector2(lastDamageOrigin);
		msg.AddUShort(NetworkManager.I.Client.Id);
		msg.AddUShort(pId);
		msg.AddUShort(lastDamagedByItemId);
		msg.AddInt(totalTickDamageAmount);
		NetworkManager.I.Client.Send(msg);

		totalTickDamageAmount = 0;
	}

	ushort lastDamagedByItemId;
	int totalTickDamageAmount;
	Vector2 lastDamageOrigin;

	public void DamageRemotePlayer(ushort itemId, int damageAmount, Vector2 damageOrigin) {
		lastDamagedByItemId = itemId;
		totalTickDamageAmount += damageAmount;
		lastDamageOrigin = damageOrigin;
	}

    [MessageHandler((ushort)NetworkManager.MessageIds.PlayerPosRot)]
    public static void HandlePlayerPosRot(Message msg) {
		ushort id = msg.GetUShort();

		// If this player doesn't exist yet, don't.
		if (!GameManager.PlayingPlayers.ContainsKey(id))
		{
			return;
		}	

		RemotePlayer remote = (RemotePlayer)GameManager.PlayingPlayers[id].playerNode;	
		
        remote.GlobalPosition = msg.GetVector2();
		remote.animatedSprite.FlipH = msg.GetBool();
		remote.animatedSprite.Play(msg.GetString());
    }
	
	[MessageHandler((ushort)NetworkManager.MessageIds.GunPosRot)]
	public static void HandleGunPosRot(Message msg) {
		ushort id = msg.GetUShort();

		// If this player doesn't exist yet, don't.
		if (!GameManager.PlayingPlayers.ContainsKey(id))
		{
			return;
		}	

		RemotePlayer remote = (RemotePlayer)GameManager.PlayingPlayers[id].playerNode;

		remote.gunRotationPoint.GlobalRotation = msg.GetFloat();
		remote.gunRotationPoint.Position = msg.GetVector2();
		remote.gunRoot.Scale = msg.GetVector2();

		if (msg.GetBool()) {
			ushort itemId = msg.GetUShort();
			if (itemId != remote.currentGunId) {
				remote.currentGunId = itemId;
				Weapon weapon = (Weapon)GameManager.GetNewInventoryItem(itemId);
				remote.gunSprite.Texture = weapon.itemSprite;
			}
		} else {
			remote.gunSprite.Texture = null;
		}
	}

	[MessageHandler((ushort)NetworkManager.MessageIds.PlayerRespawn)]
    public static void PlayerRespawn(Message msg) {
		ushort id = msg.GetUShort();

		// If this player doesn't exist yet, don't.
		if (!GameManager.PlayingPlayers.ContainsKey(id))
		{
			return;
		}		
		
		RemotePlayer player = (RemotePlayer)GameManager.PlayingPlayers[id].playerNode;
		player.collision.Disabled = false;
		player.animatedSprite.Show();
    }

	[MessageHandler((ushort)NetworkManager.MessageIds.PlayerDead)]
	
    public static void PlayerDead(Message msg) {
		ushort id = msg.GetUShort();
		ushort killerId = msg.GetUShort();
		ushort itemId = msg.GetUShort();

		// If this player doesn't exist yet, don't.
		if (!GameManager.PlayingPlayers.ContainsKey(id))
		{
			return;
		}		
		
		RemotePlayer player = (RemotePlayer)GameManager.PlayingPlayers[id].playerNode;
		player.collision.Disabled = true;
		player.animatedSprite.Hide();

		if (killerId == NetworkManager.I.Client.Id) {
			HealthManager.instance.GotKill(player.pId);
		}

		HealthManager.instance.ShowKillChat(killerId, id, itemId);
    }
}
