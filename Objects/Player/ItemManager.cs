using Godot;
using Riptide;
using System;

public partial class ItemManager : Marker2D
{
	static ItemManager instance;
	ushort thisId;

	[Export] Marker2D gunShotOrigin;
	[Export] Inventory inventory;
	[Export] Node2D rotation;
	[Export] Marker2D rotationHelper;
	[Export] Sprite2D gunSprite;
	[Export] Marker2D muzzleMarker;
	[Export] PackedScene muzzleFlashEffect;
	[Export] PackedScene impactEffect;
	[Export] PackedScene bulletTracer;
	[Export(PropertyHint.Range, "0,16")] float maxRotOffset = 8;
	[Export(PropertyHint.Layers2DPhysics)] uint layerMask;
	public Weapon gun;
	[ExportGroup("Grenades")]
	[Export] PackedScene grenadePrefab;
	[Export] Marker2D grenadeHoldPos;
	[Export] Control grenadeChargeRoot;
	[Export] Control grenadeChargePointer;
	[Export] double markerMoveDur = 0.6f;
	public Grenade currentGrenade;

    public override void _EnterTree()
    {
        instance = this;
		thisId = NetworkManager.I.Client.Id;

		MoveGrenadeMarker();
    }


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


	GrenadeObject grenadeObject;
	bool throwing = true;
    public override void _PhysicsProcess(double delta)
    {
        TickShoot();

		if (gun != null) {
			if (Input.IsActionJustPressed("reload") && gun.canShoot) {
				Reload();
			}
		}

		if (currentGrenade != null && Input.IsActionJustPressed("grenade")) {
			grenadeChargeRoot.Show();
			throwing = true;
			grenadeObject = grenadePrefab.Instantiate<GrenadeObject>();
			grenadeHoldPos.AddChild(grenadeObject);
			currentGrenade.count--;
			grenadeObject.Setup((Grenade)currentGrenade.Duplicate());
			inventory.UpdateUi(true);
		}

		if (throwing && Input.IsActionJustReleased("grenade")) {
			grenadeChargeRoot.Hide();
			throwing = false;

			if (grenadeObject == null)
				return;

			Vector2 dir = gunShotOrigin.GetLocalMousePosition().Normalized();
			float pointerPos = grenadeChargePointer.Position.X; 
			dir = dir * ((pointerPos - 1) / 55) * currentGrenade.throwPower;
			grenadeObject.Release(dir);

			GrenadeObject saved = grenadeObject;
			grenadeObject.GetParent().RemoveChild(grenadeObject);
			GameManager.I.AddChild(saved);
			saved.GlobalPosition = grenadeHoldPos.GlobalPosition;
		}

		#region 
		Message msg = Message.Create(MessageSendMode.Unreliable, NetworkManager.MessageIds.GunPosRot);

		msg.AddUShort(thisId);
		msg.AddFloat(rotation.GlobalRotation);
		msg.AddVector2(rotation.Position);
		msg.AddVector2(Scale);
		msg.AddBool(gun != null);
		if (gun != null) msg.AddUShort(gun.itemId);

		NetworkManager.I.Client.Send(msg);
		#endregion
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

		MuzzleFlashEffect muzzleFlash = muzzleFlashEffect.Instantiate<MuzzleFlashEffect>();
		AddChild(muzzleFlash);
		muzzleFlash.Setup(rotation.ToGlobal(gun.muzzleLocation), rotation.GlobalRotation, gun.flashSize);

		for (int i = 0; i < gun.shotCount; i++)
		{
			float aimDir = Mathf.RadToDeg(gunShotOrigin.GetLocalMousePosition().Normalized().Angle());
			aimDir += Tools.RandFloatRange(-gun.bloom, gun.bloom);

			PhysicsDirectSpaceState2D state = GetWorld2D().DirectSpaceState;
			PhysicsRayQueryParameters2D parameters = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + (Vector2.Right.Rotated(Mathf.DegToRad(aimDir)) * 69420));
			parameters.CollisionMask = layerMask;
			parameters.Exclude = new Godot.Collections.Array<Rid> { GetParent<CharacterBody2D>().GetRid() };
			Godot.Collections.Dictionary result = state.IntersectRay(parameters);

			if (result.Count > 0) {
				Node2D collider = (Node2D)result["collider"];
				Vector2 pos = (Vector2)result["position"];
				float angle = ((Vector2)result["normal"]).Angle();

				BasicParticleEffect impact = impactEffect.Instantiate<BasicParticleEffect>();
				impact.GlobalPosition = pos;
				impact.GlobalRotation = angle;
				GameManager.I.AddChild(impact);

				BulletTracer tracer = bulletTracer.Instantiate<BulletTracer>();
				GameManager.I.AddChild(tracer);
				tracer.GlobalPosition = gunShotOrigin.GlobalPosition;
				Vector2 start = gunShotOrigin.ToLocal(rotation.ToGlobal(gun.muzzleLocation));
				Vector2 end = gunShotOrigin.ToLocal(pos);
				tracer.Setup(start, end);

				Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.PlayerShot);
				msg.AddVector2(gunShotOrigin.GlobalPosition);
				msg.AddVector2(start);
				msg.AddVector2(end);
				msg.AddFloat(angle);
				NetworkManager.I.Client.Send(msg);

				if (collider is RemotePlayer remotePlayer) {
					remotePlayer.DamageRemotePlayer(gun.itemId, gun.damage, GlobalPosition);
				}
			}
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

	Tween grenadeMarkerTween;
	void MoveGrenadeMarker() {
		grenadeMarkerTween = CreateTween();
		grenadeMarkerTween.SetLoops();
		grenadeMarkerTween.TweenProperty(grenadeChargePointer, "position", new Vector2(56, 0), markerMoveDur).SetEase(Tween.EaseType.InOut);
		grenadeMarkerTween.TweenProperty(grenadeChargePointer, "position", new Vector2(1, 0), markerMoveDur).SetEase(Tween.EaseType.InOut);

		/*
		if (isGoingRight) {
			grenadeMarkerTween = CreateTween();
			grenadeMarkerTween.TweenProperty(grenadeChargePointer, "position", new Vector2(56, 0), markerMoveDur).SetEase(Tween.EaseType.InOut);
			grenadeMarkerTween.TweenCallback(Callable.From(MoveGrenadeMarker)).SetDelay(markerMoveDur);
			isGoingRight = !isGoingRight;
		} else {
			grenadeMarkerTween = CreateTween();
			grenadeMarkerTween.TweenProperty(grenadeChargePointer, "position", new Vector2(1, 0), markerMoveDur).SetEase(Tween.EaseType.InOut);
			grenadeMarkerTween.TweenCallback(Callable.From(MoveGrenadeMarker)).SetDelay(markerMoveDur);
			isGoingRight = !isGoingRight;
		}
		*/
	}

	public void UpdateHolding(int newGunIndex, Grenade newGrenade) {
		gun = inventory.weapons[newGunIndex];
		if (gun != null) {
			gunSprite.Texture = gun.itemSprite;
		} else {
			gunSprite.Texture = null;
		}

		shotsTimer = null;
		reloadTimer = null;

		currentGrenade = newGrenade;
	}

    [MessageHandler((ushort)NetworkManager.MessageIds.PlayerShot)]
    public static void PlayerShot(Message msg) => instance.PlayerShot(msg.GetVector2(),msg.GetVector2(), msg.GetVector2(), msg.GetFloat());

    void PlayerShot(Vector2 origin, Vector2 start, Vector2 end, float angle) {
		BasicParticleEffect impact = impactEffect.Instantiate<BasicParticleEffect>();
		impact.GlobalPosition = origin + end;
		impact.GlobalRotation = angle;
		GameManager.I.AddChild(impact);

		BulletTracer tracer = bulletTracer.Instantiate<BulletTracer>();
		GameManager.I.AddChild(tracer);
		tracer.GlobalPosition = origin;
		tracer.Setup(start, end);
	}
}
