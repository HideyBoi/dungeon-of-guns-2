using Godot;
using Riptide;
using System;
using System.Collections.Generic;

public partial class InventoryItemObject : CharacterBody2D
{
	public static Dictionary<ushort, InventoryItemObject> Items;
	static PackedScene itemObject;

	enum ItemType { NONE, WEAPON, AMMO, MEDICAL, GRENADE }

	public InventoryItem Item;
	[Export] Sprite2D itemSprite;
	[Export] Material[] mats;
	[Export] Label itemText;
	[Export] Control popupRoot;
	[Export] float speed;
	[Export] float deceleration;

	public ushort thisId;
	ushort ownerId;
	ushort localId;
	ItemType itemType;


	public void Setup(InventoryItem item, ushort owner, Vector2 spawnVelocity, float impulse = 1, bool fromNetwork = false) {
		localId = NetworkManager.I.Client.Id;
		ownerId = owner;
		Velocity = spawnVelocity * speed * impulse;

		if (!fromNetwork) {
			if (Items == null)
				Items = new();

			bool foundId = false;
			while(!foundId) {
				thisId = (ushort)Tools.RandIntRange(0, 69696969);
				
				foundId = !Items.TryGetValue(thisId, out _);
			}

			Items.Add(thisId, this);
		}

		Item = item;
		itemSprite.Texture = Item.itemSprite;

		switch (item)
		{
			case Weapon weapon:
				itemType = ItemType.WEAPON;
				itemText.Text = $"{weapon.itemName}\n{weapon.currentAmmo} - {Weapon.GetRarityText(weapon.rarity)}";
				itemSprite.Material = mats[(int)weapon.rarity];
				break;
			case Ammo ammo:
				itemType = ItemType.AMMO;
				itemText.Text = $"{ammo.itemName}\n{ammo.count}";
				break;
			case Healable heal:
				itemType = ItemType.MEDICAL;
				int healAmount = (int)Math.Floor(heal.healAmount * float.Parse(ConfigManager.CurrentGamerules["med_multiplier"]));
				itemText.Text = $"{heal.itemName}\n{heal.count} | +{healAmount}hp";
				break;
			case Grenade grenade:
				itemType = ItemType.GRENADE;
				itemText.Text = $"{grenade.itemName}\n{grenade.count}";
				break;
			default:
				itemType = ItemType.NONE;
				itemText.Text = $"{item.itemName}\n/!\\ Unknown ItemType.";
				GD.PushWarning($"Item {item.itemName} did not match any supported classes.");
				break;
		}

		if (!fromNetwork)
			SendSpawn();
	}

	Vector2 lastPos;

    public override void _PhysicsProcess(double delta)
    {
        if (localId != ownerId)
			return;
		
		Vector2 vel = Vector2.Zero;
		vel.X = Mathf.Lerp(Velocity.X, 0, deceleration * (float)delta);
		vel.Y = Mathf.Lerp(Velocity.Y, 0, deceleration * (float)delta);
		Velocity = vel;

		MoveAndSlide();

		if (lastPos != GlobalPosition) {
			lastPos = GlobalPosition;

			Message msg =  Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.ItemMove);
			msg.AddUShort(thisId);
			msg.AddVector2(GlobalPosition);
			NetworkManager.I.Client.Send(msg);
		}
    }

	[MessageHandler((ushort)NetworkManager.MessageIds.ItemMove)]
	public static void ItemMove(Message msg) {
		ushort id = msg.GetUShort();
		Vector2 newPos = msg.GetVector2();

		if (Items.TryGetValue(id, out InventoryItemObject inventoryItemObject)) {
			inventoryItemObject.GlobalPosition = newPos;
		} else {
			GD.PushWarning("/!\\ Missing item! Could be a race condition, probably safe to ignore.");
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

	void SendSpawn() {
		Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.ItemSpawn);

		msg.AddUShort(localId);
		msg.AddUShort(thisId);
		msg.AddVector2(GlobalPosition);
		msg.AddVector2(Velocity);

		msg.AddInt((int)itemType);
		switch (itemType)
		{
			case ItemType.NONE:
				msg.AddInt(Item.itemId);
				break;
			case ItemType.WEAPON:
				msg.AddWeapon((Weapon)Item);
				break;
			case ItemType.AMMO:
				msg.AddAmmo((Ammo)Item);
				break;
			case ItemType.MEDICAL:
				msg.AddHealable((Healable)Item);
				break;
			case ItemType.GRENADE:
				msg.AddGrenade((Grenade)Item);
				break;
		}

		NetworkManager.I.Client.Send(msg);

		// TODO: Sound
	}
	
	[MessageHandler((ushort)NetworkManager.MessageIds.ItemSpawn)]
	public static void ReceiveSpawn(Message msg) {
		if (itemObject == null)
			itemObject = ResourceLoader.Load<PackedScene>("res://Objects/InventoryItemObject.tscn");

		ushort ownerId = msg.GetUShort();
		ushort currentId = msg.GetUShort();
		Vector2 pos = msg.GetVector2();
		Vector2 vel = msg.GetVector2();

		InventoryItemObject inventoryItemObject = itemObject.Instantiate<InventoryItemObject>();
		inventoryItemObject.GlobalPosition = pos;
		inventoryItemObject.thisId = currentId;

		if (Items == null)
			Items = new();

		Items.Add(currentId, inventoryItemObject);

		InventoryItem item = null;

		int itemType = msg.GetInt();
		switch ((ItemType)itemType)
		{
			case ItemType.NONE:
				item = (InventoryItem)GameManager.I.possibleItems[msg.GetInt()].Duplicate();
				break;
			case ItemType.WEAPON:
				item = msg.GetWeapon();
				break;
			case ItemType.AMMO:
				item = msg.GetAmmo();
				break;
			case ItemType.MEDICAL:
				item = msg.GetHealable();
				break;
			case ItemType.GRENADE:
				item = msg.GetGrenade();
				break;
		}

		inventoryItemObject.Setup(item, ownerId, vel, fromNetwork: true);
		
		GameManager.I.AddChild(inventoryItemObject);

		// TODO: Sound
	}

	public void Pickup() {
		Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.ItemRemove);

		msg.AddUShort(thisId);
		msg.AddUShort(Item.itemId);

		NetworkManager.I.Client.Send(msg);

		// TODO: Play a sound or some shit lol
		QueueFree();
	}

	[MessageHandler((ushort)NetworkManager.MessageIds.ItemRemove)]
	public static void ItemPickedUp(Message msg) {
		ushort objectId = msg.GetUShort();
		ushort itemId = msg.GetUShort();

		Items[objectId].QueueFree();
		Items.Remove(objectId);

		// Todo, play pickup sound
	}

    public override void _ExitTree()
    {
        try {
			Items.Remove(thisId);
		} catch (ArgumentNullException) {
			// do nothing cus wtv lol
		}
    }
}
