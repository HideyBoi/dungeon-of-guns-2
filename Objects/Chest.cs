using Godot;
using Riptide;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;

public partial class Chest : StaticBody2D
{
	public static Dictionary<ushort, Chest> Chests = new();
	ushort thisId;
	[Export] AnimatedSprite2D boxSprite;
	[Export] PackedScene itemObject;
	[Export] float[] impulse;
	[Export] CollisionShape2D collider;
	[Export] Control popupRoot;

	float legendaryChance;
	float rareChance;

    public override void _Ready()
    {
		thisId = (ushort)GlobalPosition.GetHashCode();
		Chests.Add(thisId, this);

		legendaryChance = float.Parse(ConfigManager.CurrentGamerules["legendary_chance"]);
		rareChance = float.Parse(ConfigManager.CurrentGamerules["rare_chance"]);
    }

	SceneTreeTimer timeTillRegen;
	public void Open(bool fromNetwork = false) {
		// TODO: Art stuffs to figure out later lol
		collider.Disabled = true;
		boxSprite.Frame = 1;
		
		if (bool.Parse(ConfigManager.CurrentGamerules["chests_regenerate"])) {
			timeTillRegen = GetTree().CreateTimer(float.Parse(ConfigManager.CurrentGamerules["chests_regeneration_time"]));
			timeTillRegen.Timeout += ChestRegen;
		}

		if (!fromNetwork) {
			SpawnLoot();

			Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.ChestOpened);
			msg.AddUShort(thisId);
			NetworkManager.I.Client.Send(msg);
		}
	}

	[MessageHandler((ushort)NetworkManager.MessageIds.ChestOpened)]
	public static void ChestOpenedRemotely(Message msg) {
		ushort chestId = msg.GetUShort();

		if (Chests.TryGetValue(chestId, out Chest chest)) {
			chest.Open(true);
		} else {
			GD.PrintErr("Missing chest!!!!");
		}
	}

	void ChestRegen() {
		// TODO: some sort of sound effect/particle/animation
		collider.Disabled = false;
		boxSprite.Frame = 0;
	}

	void SpawnLoot() {
		float weaponRng = Tools.RandFloatRange(0, 100);

		Weapon weapon;

		if (weaponRng < legendaryChance) {
			ushort rand = (ushort)GameManager.I.rareChestItems[Tools.RandIntRange(0, GameManager.I.rareChestItems.Count)];
			weapon = (Weapon)GameManager.GetNewInventoryItem(rand);
		} else if (weaponRng < rareChance) {
			ushort rand = (ushort)GameManager.I.midChestItems[Tools.RandIntRange(0, GameManager.I.midChestItems.Count)];
			weapon = (Weapon)GameManager.GetNewInventoryItem(rand);
		} else {
			ushort rand = (ushort)GameManager.I.commonChestItems[Tools.RandIntRange(0, GameManager.I.commonChestItems.Count)];
			weapon = (Weapon)GameManager.GetNewInventoryItem(rand);
		}
		weapon.currentAmmo = weapon.maxAmmo;

		InventoryItemObject newWeapon = itemObject.Instantiate<InventoryItemObject>();
		newWeapon.Setup(weapon, NetworkManager.I.Client.Id, Tools.RandDirection(), Tools.RandFloatRange(impulse[0], impulse[1]));
		newWeapon.GlobalPosition = GlobalPosition;
		GameManager.I.AddChild(newWeapon);

		Ammo ammo = (Ammo)GameManager.GetNewInventoryItem((ushort)GameManager.I.ammo[(int)weapon.ammoType]);
		ammo.count = weapon.maxAmmo * (int)Math.Floor(float.Parse(ConfigManager.CurrentGamerules["ammo_multiplier"]));

		InventoryItemObject newAmmo = itemObject.Instantiate<InventoryItemObject>();
		newAmmo.Setup(ammo, NetworkManager.I.Client.Id, Tools.RandDirection(), Tools.RandFloatRange(impulse[0], impulse[1]));
		newAmmo.GlobalPosition = GlobalPosition;
		GameManager.I.AddChild(newAmmo);

		float healRng = Tools.RandFloatRange(0, 100);
		if (healRng < float.Parse(ConfigManager.CurrentGamerules["secondary_chance"])) {
			InventoryItem secondary = GameManager.GetNewInventoryItem((ushort)GameManager.I.secondaries[Tools.RandIntRange(0, GameManager.I.secondaries.Count)]);
			if (secondary is Healable healable) {
				healable.count = Tools.RandIntRange(0, int.Parse(ConfigManager.CurrentGamerules["secondary_max_count"] + 1));

				InventoryItemObject newSecondary = itemObject.Instantiate<InventoryItemObject>();
				newSecondary.Setup(healable, NetworkManager.I.Client.Id, Tools.RandDirection(), Tools.RandFloatRange(impulse[0], impulse[1]));
				newSecondary.GlobalPosition = GlobalPosition;
				GameManager.I.AddChild(newSecondary);
			} else if (secondary is Grenade grenade) {
				grenade.count = Tools.RandIntRange(0, int.Parse(ConfigManager.CurrentGamerules["secondary_max_count"] + 1));

				InventoryItemObject newSecondary = itemObject.Instantiate<InventoryItemObject>();
				newSecondary.Setup(grenade, NetworkManager.I.Client.Id, Tools.RandDirection(), Tools.RandFloatRange(impulse[0], impulse[1]));
				newSecondary.GlobalPosition = GlobalPosition;
				GameManager.I.AddChild(newSecondary);
			}
		}
	}

	SceneTreeTimer timeToEndInteract;

	public void Interact() {
		if (timeToEndInteract == null) {
			timeToEndInteract = GetTree().CreateTimer(0.6f);
			timeToEndInteract.Timeout += EndInteraction;
		}
		popupRoot.Modulate = Colors.White;
		timeToEndInteract.TimeLeft = 0.4f;
	}

	void EndInteraction() {
		timeToEndInteract = null;

		Tween tween = CreateTween();
		tween.TweenProperty(popupRoot, "modulate", Colors.Transparent, 0.2f);
	}

    public override void _ExitTree()
    {
        Chests.Remove(thisId);
    }
}
