using Godot;
using Riptide;
using System;
using System.Collections.Generic;

public partial class GrenadeObject : CharacterBody2D
{
	public static Dictionary<ushort, GrenadeObject> Grenades;

	public Grenade thisGrenade;
	[Export] Area2D nearbyDetector;
	[Export] Sprite2D sprite;
	[Export] Area2D explosionArea;
	[Export] CollisionShape2D collisionShape;
	bool isFree;
	ushort ownerId;
	ushort objectId;

	public void Setup(Grenade newGrenade, bool fromNetwork = false) {
		thisGrenade = newGrenade;

		sprite.Texture = thisGrenade.itemSprite;
		sprite.ZIndex = 1;

		if (!fromNetwork) {
			thisGrenade.OnStartUse(this);

			if (Grenades == null)
				Grenades = new();

			bool foundId = false;
			while(!foundId) {
				objectId = (ushort)Tools.RandIntRange(0, 69696969);
				
				foundId = !Grenades.TryGetValue(objectId, out _);
			}

			Grenades.Add(objectId, this);

			ownerId = NetworkManager.I.Client.Id;
			SendSpawn();
		}
			
	}

	void SendSpawn() {
		Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.GrenadeSpawn);
		msg.AddVector2(GlobalPosition);
		msg.AddUShort(ownerId);
		msg.AddUShort(objectId);
		msg.AddUShort(thisGrenade.itemId);
		NetworkManager.I.Client.Send(msg);
	}

	static PackedScene grenadeObject;

	[MessageHandler((ushort)NetworkManager.MessageIds.GrenadeSpawn)]
	public static void HandleSpawn(Message msg) {
		Vector2 pos = msg.GetVector2();
		ushort ownerId = msg.GetUShort();
		ushort objectId = msg.GetUShort();
		Grenade grenade = (Grenade)GameManager.GetNewInventoryItem(msg.GetUShort());

		if (grenadeObject == null)
			grenadeObject = ResourceLoader.Load<PackedScene>("res://Objects/GrenadeObject.tscn");
		
		GrenadeObject newGrenade = grenadeObject.Instantiate<GrenadeObject>();
		newGrenade.ownerId = ownerId;
		newGrenade.objectId = objectId;
		newGrenade.Position = pos;
		GameManager.I.AddChild(newGrenade);
		newGrenade.Setup(grenade, fromNetwork: true);

		if (Grenades == null)
			Grenades = new();

		Grenades.Add(objectId, newGrenade);
	}

	public void Release(Vector2 newVelocity) {
		Velocity = newVelocity;
		thisGrenade.OnFinishedUse();
		isFree = true;
		sprite.ZIndex = 0;
	}

	Vector2 lastPos;

    public override void _PhysicsProcess(double delta)
    {
		if (!isFree || (ownerId != NetworkManager.I.Client.Id))
			return;

		float _speedChange = (float)delta * thisGrenade.drag;

		Vector2 vel = Vector2.Zero;
		vel.X = Mathf.Lerp(Velocity.X, 0, _speedChange);
		vel.Y = Mathf.Lerp(Velocity.Y, 0, _speedChange);

		Velocity = vel;

		KinematicCollision2D collision = MoveAndCollide(Velocity * (float)delta);
		if (collision != null) {
			Velocity = Velocity.Bounce(collision.GetNormal()) * thisGrenade.bounceAmount;
		}

		
		
		if (lastPos != GlobalPosition) {
			lastPos = GlobalPosition;

			Message msg =  Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.GrenadeMove);
			msg.AddUShort(objectId);
			msg.AddVector2(GlobalPosition);
			NetworkManager.I.Client.Send(msg);
		}
    }

	[MessageHandler((ushort)NetworkManager.MessageIds.GrenadeMove)]
	public static void HandleMove(Message msg) {
		ushort id = msg.GetUShort();
		Vector2 newPos = msg.GetVector2();

		if (Grenades.TryGetValue(id, out GrenadeObject grenade)) {
			grenade.GlobalPosition = newPos;
		} else {
			GD.PushWarning("/!\\ Missing grenade! Could be a race condition, probably safe to ignore.");
		}
	}

    public void Explode() {
		Node2D explosionEffect = thisGrenade.blastEffect.Instantiate<Node2D>();
		explosionEffect.GlobalPosition = GlobalPosition;
		GameManager.I.AddChild(explosionEffect);

		//TODO: Sounds & Camera Shake

		Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.GrenadeExplode);
		msg.AddVector2(GlobalPosition);
		msg.AddUShort(objectId);
		msg.AddUShort(thisGrenade.itemId);
		NetworkManager.I.Client.Send(msg);

		CalcDamage();

		QueueFree();
	}

	void CalcDamage() {
		CircleShape2D circle = (CircleShape2D)collisionShape.Shape;
		circle.Radius = thisGrenade.blastRadius;
		
		Godot.Collections.Array<Node2D> nodes = explosionArea.GetOverlappingBodies();
		GD.Print($"Caught {nodes.Count}");
		foreach (Node2D node in nodes) {
			if (node is LocalPlayer player) {
				player.GetNode<HealthManager>("./HealthManager").OnPlayerDamaged(GlobalPosition, NetworkManager.I.Client.Id, thisGrenade.itemId, CalcDamageAmount(player.GlobalPosition));
			} else if (node is RemotePlayer remote) {
				remote.DamageRemotePlayer(thisGrenade.itemId, CalcDamageAmount(remote.GlobalPosition), GlobalPosition);
			}
		}
	}

	int CalcDamageAmount(Vector2 playerPos) {
		float dist = Distance(explosionArea.GlobalPosition, playerPos);
		float distNormalized = dist/(thisGrenade.blastRadius + 10);
		int damage = (int)MathF.Floor(thisGrenade.damageFalloff.Sample(distNormalized) * thisGrenade.damageAmount);
		GD.Print(damage);
		return damage;
	}

	float Distance(Vector2 v1, Vector2 v2) {
        return Mathf.Abs(Mathf.Sqrt(Mathf.Pow(v1.X - v2.X, 2) + Mathf.Pow(v1.Y - v2.Y, 2)));
    }

	[MessageHandler((ushort)NetworkManager.MessageIds.GrenadeExplode)]
	public static void HandleExplosion(Message msg) {
		Vector2 pos = msg.GetVector2();
		ushort objectId = msg.GetUShort();
		ushort itemId = msg.GetUShort();

		Grenade grenade = (Grenade)GameManager.GetNewInventoryItem(itemId);

		Node2D explosionEffect = grenade.blastEffect.Instantiate<Node2D>();
		explosionEffect.GlobalPosition = pos;
		GameManager.I.AddChild(explosionEffect);

		Grenades[objectId].QueueFree();
	}

    public override void _ExitTree()
    {
        try {
			Grenades.Remove(objectId);
		} catch (ArgumentNullException) {
			// do nothing cus wtv lol
		}
    }

    public void StartScanning() {
		nearbyDetector.Monitoring = true;
	}

	public void NearbyDetection(Node2D body) {
		if (body is RemotePlayer || body is LocalPlayer) {
			Explode();
		}
	}
}
