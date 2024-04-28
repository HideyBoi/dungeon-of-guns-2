using Godot;
using Riptide;
using System;
using System.Collections.Generic;

public partial class AmmoCrate : StaticBody2D
{
	public static Dictionary<ushort, AmmoCrate> AmmoCrates = new();
	ushort thisId;
	[Export] AnimatedSprite2D boxSprite;
	[Export] SpriteFrames crateSprites;  
	[Export] SpriteFrames brokenSprite;
	[Export] PackedScene itemObject;
	[Export] float[] impulse;
	[Export] CollisionShape2D collider;
	[Export] Control popupRoot;

	float legendaryChance;
	float rareChance;

    public override void _Ready()
    {
		thisId = (ushort)GlobalPosition.GetHashCode();
		AmmoCrates.Add(thisId, this);

		legendaryChance = float.Parse(ConfigManager.CurrentGamerules["legendary_chance"]);
		rareChance = float.Parse(ConfigManager.CurrentGamerules["rare_chance"]);

		boxSprite.SpriteFrames = crateSprites;
		boxSprite.Frame = Tools.RandIntRange(0, crateSprites.GetFrameCount("default"));
    }

	SceneTreeTimer timeTillRegen;
	public void Open(bool fromNetwork = false) {
		// TODO: Art stuffs to figure out later lol
		collider.Disabled = true;
		boxSprite.SpriteFrames = brokenSprite;
		timeTillRegen = GetTree().CreateTimer(60);
		timeTillRegen.Timeout += AmmoRegen;

		if (!fromNetwork) {
			SpawnLoot();

			Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.AmmoOpened);
			msg.AddUShort(thisId);
			NetworkManager.I.Client.Send(msg);
		}
	}

	[MessageHandler((ushort)NetworkManager.MessageIds.AmmoOpened)]
	public static void AmmoOpenedRemotely(Message msg) {
		ushort AmmoId = msg.GetUShort();

		if (AmmoCrates.TryGetValue(AmmoId, out AmmoCrate ammoCrate)) {
			ammoCrate.Open(true);
		} else {
			GD.PrintErr("Missing Ammo!!!!");
		}
	}

	void AmmoRegen() {
		// TODO: some sort of sound effect/particle/animation
		collider.Disabled = false;
		boxSprite.SpriteFrames = crateSprites;
		boxSprite.Frame = Tools.RandIntRange(0, crateSprites.GetFrameCount("default"));
	}

	void SpawnLoot() {

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
        AmmoCrates.Remove(thisId);
    }
}
