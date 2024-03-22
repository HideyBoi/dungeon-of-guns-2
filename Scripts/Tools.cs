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

		Weapon newWeapon = (Weapon)GameManager.I.possibleItems[weaponId].Duplicate();
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

		Ammo ammo = (Ammo)GameManager.I.possibleItems[ammoId].Duplicate();
		ammo.ammoType = ammoType;
		ammo.count = ammoCount;

		return ammo;
	}

	public static Message AddVector2(this Message msg, Vector2 v2) {
		return msg.AddFloat(v2.X).AddFloat(v2.Y);
	}

	public static Vector2 GetVector2(this Message msg) {
		return new Vector2(msg.GetFloat(), msg.GetFloat());
	}
}