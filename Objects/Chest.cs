using Godot;
using Riptide;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;

public partial class Chest : StaticBody2D
{
	public static Dictionary<ushort, Chest> Chests = new();
	enum ChestDir { FORWARD, LEFT, BACK, RIGHT};
	ushort thisId;

	[Export] ChestDir chestDir;
	[Export] Texture2D[] sprites;
	[Export] Sprite2D chestSprite;
	[Export] PackedScene itemObject;

	float legendaryChance;
	float rareChance;

    public override void _Ready()
    {
		thisId = (ushort)GlobalPosition.GetHashCode();
		Chests.Add(thisId, this);

		legendaryChance = float.Parse(ConfigManager.CurrentGamerules["legendary_chance"]);
		rareChance = float.Parse(ConfigManager.CurrentGamerules["rare_chance"]);
    }

	public void Open(bool fromNetwork = false) {
		// TODO: Art stuffs to figure out later lol
		
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

	void SpawnLoot() {
		float rolledValue = Tools.RandFloatRange(0, 100);

		Weapon weapon;

		if (rolledValue < legendaryChance) {
			ushort rand = (ushort)GameManager.I.rareChestItems[Tools.RandIntRange(0, GameManager.I.rareChestItems.Length)];
			weapon = (Weapon)GameManager.GetNewInventoryItem(rand);
		} else if (rolledValue < rareChance) {
			ushort rand = (ushort)GameManager.I.midChestItems[Tools.RandIntRange(0, GameManager.I.midChestItems.Length)];
			weapon = (Weapon)GameManager.GetNewInventoryItem(rand);
		} else {
			ushort rand = (ushort)GameManager.I.commonChestItems[Tools.RandIntRange(0, GameManager.I.commonChestItems.Length)];
			weapon = (Weapon)GameManager.GetNewInventoryItem(rand);
		}
	}

    public override void _ExitTree()
    {
        Chests.Remove(thisId);
    }
}
