using Godot;
using Riptide;

public static class Tools {

	// Summary:
	//	Exclusive
	public static int RandIntRange(int min, int max) {
		return (int)(GD.Randi() % max + min);
	}

	// Summary:
	//	Inclusive
	public static float RandFloatRange(float min, float max) {
		return (float)GD.RandRange(min, max);
	}

	public static Message AddWeapon(this Message msg, Weapon weapon) {
		ushort weaponId = weapon.itemId;
		int currentAmmo = weapon.currentAmmo;

		msg.AddUShort(weaponId);
		msg.AddInt(currentAmmo);
		
		return msg;
	}

	public static Weapon GetWeapon(this Message msg) {
		ushort weaponId = msg.GetUShort();
		int currentAmmo = msg.GetInt();

		Weapon newWeapon = (Weapon)GameManager.GetNewInventoryItem(weaponId);
		newWeapon.currentAmmo = currentAmmo;
		
		return newWeapon;
	}

	public static Message AddAmmo(this Message msg, Ammo ammo) {
		ushort ammoId = ammo.itemId;
		int ammoType = (int)ammo.ammoType;
		int count = ammo.count;

		msg.AddUShort(ammoId);
		msg.AddInt(ammoType);
		msg.AddInt(count);

		return msg;
	}

	public static Ammo GetAmmo(this Message msg) {
		ushort ammoId = msg.GetUShort();
		Weapon.AmmoType ammoType = (Weapon.AmmoType)msg.GetInt();
		int ammoCount = msg.GetInt();

		Ammo ammo = (Ammo)GameManager.GetNewInventoryItem(ammoId);
		ammo.ammoType = ammoType;
		ammo.count = ammoCount;

		return ammo;
	}

	public static Message AddHealable(this Message msg, Healable heal) {
		ushort healId = heal.itemId;
		int itemCount = heal.count;
		float healAmount = heal.healAmount;

		msg.AddUShort(healId);
		msg.AddInt(itemCount);
		msg.AddFloat(healAmount);

		return msg;
	}

	public static Healable GetHealable(this Message msg) {
		ushort healId = msg.GetUShort();
		int itemCount = msg.GetInt();
		float healAmount = msg.GetFloat();

		Healable healable = (Healable)GameManager.GetNewInventoryItem(healId);
		healable.count = itemCount;
		healable.healAmount = healAmount;

		return healable;
	}

	public static Message AddGrenade(this Message msg, Grenade grenade) {
		// TODO: Grenade
		ushort grenadeId = grenade.itemId;
		int itemCount = grenade.count;

		msg.AddUShort(grenadeId);
		msg.AddInt(itemCount);

		return msg;
	}

	public static Grenade GetGrenade(this Message msg) {
		ushort grenadeId = msg.GetUShort();
		int itemCount = msg.GetInt();

		Grenade grenade = (Grenade)GameManager.GetNewInventoryItem(grenadeId);
		grenade.count = itemCount;

		return grenade;
	}

	public static Message AddVector2(this Message msg, Vector2 v2) {
		return msg.AddFloat(v2.X).AddFloat(v2.Y);
	}

	public static Vector2 GetVector2(this Message msg) {
		return new Vector2(msg.GetFloat(), msg.GetFloat());
	}
}