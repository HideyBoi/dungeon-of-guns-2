using Godot;
using Riptide;
using System;
using System.Collections.Generic;

public partial class InventoryItemObject : CharacterBody2D
{
	public static Dictionary<ushort, InventoryItemObject> objects;
	static PackedScene itemObject;

	enum ItemType { NONE, WEAPON, MEDICAL, ASSIST }

	public InventoryItem Item;
	[Export] Sprite2D itemSprite;
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

		if (objects == null)
			objects = new();

		bool foundId = false;
		while(!foundId) {
			thisId = (ushort)Tools.RandIntRange(0, 69696969);
			
			foundId = !objects.TryGetValue(thisId, out _);
		}

		objects.Add(thisId, this);

		Item = item;
		itemSprite.Texture = Item.itemSprite;

		switch (item)
		{
			case Weapon weapon:
				itemType = ItemType.WEAPON;
				GD.Print($"Item {item.itemName} is a weapon.");
				itemText.Text = $"{weapon.itemName}\n{weapon.currentAmmo} - {Weapon.GetRarityText(weapon.rarity)}";
				break;
			default:
				itemType = ItemType.NONE;
				GD.Print($"Item {item.itemName} did not match any items.");
				break;
		};

		if (!fromNetwork)
			SendSpawn();
	}

	Vector2 lastPos;

    public override void _PhysicsProcess(double delta)
    {
        if (localId != ownerId)
			return;
		
		Vector2 vel = Vector2.Zero;
		vel.X = Mathf.Lerp(Velocity.X, 0, deceleration);
		vel.Y = Mathf.Lerp(Velocity.Y, 0, deceleration);
		Velocity = vel;

		if (lastPos != GlobalPosition) {
			lastPos = GlobalPosition;

			// Send item move packet
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
				break;
			case ItemType.WEAPON:
				msg.AddWeapon((Weapon)Item);
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
		objects.Add(currentId, inventoryItemObject);

		InventoryItem item = null;

		int itemType = msg.GetInt();
		switch ((ItemType)itemType)
		{
			case ItemType.NONE:
				break;
			case ItemType.WEAPON:
				item = msg.GetWeapon();
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

		objects[objectId].QueueFree();
		objects.Remove(objectId);

		// Todo, play pickup sound
	}

    public override void _ExitTree()
    {
        try {
			objects.Remove(thisId);
		} catch (ArgumentNullException) {
			// do nothing cus wtv lol
		}
    }
}
