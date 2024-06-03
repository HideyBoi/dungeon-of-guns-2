using Godot;
using System;

public partial class GrenadeObject : CharacterBody2D
{

	public Grenade thisGrenade;
	[Export] Area2D nearbyDetector;
	[Export] Sprite2D sprite;
	bool isFree;

	public void Setup(Grenade newGrenade) {
		thisGrenade = newGrenade;
		thisGrenade.OnStartUse(this);

		sprite.Texture = thisGrenade.itemSprite;
		sprite.ZIndex = 1;
	}

	public void Release(Vector2 newVelocity) {
		Velocity = newVelocity;
		thisGrenade.OnFinishedUse();
		isFree = true;
		sprite.ZIndex = 0;
	}

    public override void _Process(double delta)
    {
		if (!isFree)
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
    }

    public void Explode() {
		GD.Print("exploding");
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
