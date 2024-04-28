using Godot;
using System;

public partial class ItemManager : Marker2D
{
	[Export] Inventory inventory;
	[Export] Node2D rotation;
	[Export] Marker2D rotationHelper;
	[Export] Sprite2D gunSprite;
	[Export] Marker2D muzzleMarker;
	[Export(PropertyHint.Range, "0,16")] float maxRotOffset = 8;
	[Export(PropertyHint.Layers2DPhysics)] uint layerMask;
	public Weapon gun;

	public override void _Process(double delta)
	{
		Vector2 mousePos = GetGlobalMousePosition();
		rotationHelper.LookAt(mousePos);

		float angle = rotationHelper.GlobalRotationDegrees;
		float absAngle = MathF.Abs(angle);

		rotation.GlobalRotationDegrees = angle;
		float offset = maxRotOffset * ((absAngle - 1)/(90 - 1));
		rotation.Position = new(offset, rotation.Position.Y);

		if (GlobalPosition.X - mousePos.X < 0) {
            Scale = new(1, 1);
        } else {
            Scale = new(-1, 1);
			rotation.Position = new(rotation.Position.X + (maxRotOffset * -2), rotation.Position.Y);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        TickShoot();

		if (gun != null) {
			if (Input.IsActionJustPressed("reload") && gun.canShoot) {
				Reload();
			}
		}
    }

    bool wantsToShoot = false;
	bool isReloading = false;

	SceneTreeTimer shotsTimer;

	void TickShoot() {
		if (Input.IsActionJustPressed("shoot"))
			wantsToShoot = true;
		if (Input.IsActionJustReleased("shoot"))
			wantsToShoot = false;

		if (gun == null) {
			wantsToShoot = false;
			return;
		}

		if (!gun.canShoot && (shotsTimer == null)) {
			shotsTimer = GetTree().CreateTimer(gun.timeBetweenShots);
			shotsTimer.Timeout += ShotTimerDone;
			return;
		}

		if (!wantsToShoot || !gun.canShoot || isReloading)
			return;

		gun.canShoot = false;
		shotsTimer = GetTree().CreateTimer(gun.timeBetweenShots);
		shotsTimer.Timeout += ShotTimerDone;

		if (gun.currentAmmo == 0) {
			Reload();
			return;
		}

		gun.currentAmmo--;

		Vector2 aimDir = GetLocalMousePosition().Normalized();

		PhysicsDirectSpaceState2D state = GetWorld2D().DirectSpaceState;
		PhysicsRayQueryParameters2D parameters = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + (aimDir * 69420));
		parameters.CollisionMask = layerMask;
		parameters.Exclude = new Godot.Collections.Array<Rid> { GetParent<CharacterBody2D>().GetRid() };
		Godot.Collections.Dictionary result = state.IntersectRay(parameters);

		if (result.Count > 0) {
			Node2D collider = (Node2D)result["collider"];
		}

		inventory.UpdateUi(true);
	}

	void ShotTimerDone() {
		shotsTimer = null;

		gun.canShoot = true;
	}

	SceneTreeTimer reloadTimer;
	void Reload() {
		if (inventory.ammoCounts[(int)gun.ammoType] > 0) {
			isReloading = true;
			reloadTimer = GetTree().CreateTimer(gun.reloadTime);
			reloadTimer.Timeout += DoneReload;

			// Play reload sound
		} else {
			// Play empty clip sound
		}
	}
	
	void DoneReload() {
		reloadTimer = null;

		isReloading = false;
		int maxAmmo = gun.maxAmmo;
		int currentGunAmmo = gun.currentAmmo;
		int currentAmmo = inventory.ammoCounts[(int)gun.ammoType];
		int diff = maxAmmo - currentGunAmmo;

		if (diff < currentAmmo) {
			currentAmmo -= diff;
			currentGunAmmo += diff;
		} else {
			currentGunAmmo = currentAmmo;
			currentAmmo = 0;
		}

		inventory.ammoCounts[(int)gun.ammoType] = currentAmmo;
		gun.currentAmmo = currentGunAmmo;

		inventory.UpdateUi(true);
	}

	public void UpdateHolding(int newGunIndex) {
		gun = inventory.weapons[newGunIndex];
		if (gun != null) {
			gunSprite.Texture = gun.itemSprite;
		} else {
			gunSprite.Texture = null;
		}

		shotsTimer = null;
		reloadTimer = null;
	}
}
