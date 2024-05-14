using Godot;
using Riptide;
using System;

public partial class HealthManager : Node2D
{

	static HealthManager instance;

	int maxHealth;
	int currentHealth;
	ushort pId;

	[Export] TextureProgressBar healthBar;
	[Export] TextureProgressBar delayHealthBar;
	[Export] Label healthCount;

    public override void _EnterTree()
    {
		instance = this;

		pId = NetworkManager.I.Client.Id;

        maxHealth = int.Parse(ConfigManager.CurrentGamerules["max_health"]);
		currentHealth = maxHealth;
		healthBar.MaxValue = maxHealth;
		healthBar.Value = maxHealth;
		delayHealthBar.MaxValue = maxHealth;
		delayHealthBar.Value = maxHealth;
    }

    public override void _Process(double delta)
    {
		healthCount.Text = currentHealth.ToString();
        healthBar.Value = currentHealth;
		delayHealthBar.Value = Mathf.Lerp(delayHealthBar.Value, currentHealth, delta);
    }

    [MessageHandler((ushort)NetworkManager.MessageIds.DamagePlayer)]
	public static void OnPlayerDamaged(Message msg) {
		ushort fromId = msg.GetUShort();
		ushort toId = msg.GetUShort();
		ushort itemId = msg.GetUShort();
		int damageAmount = msg.GetInt();

		if (toId != instance.pId)
			return;

		instance.OnPlayerDamaged(fromId, itemId, damageAmount);
	}

	void OnPlayerDamaged(ushort fromId, ushort itemId, int damageAmount) {
		currentHealth -= damageAmount;

		// TODO: damage pointer
		// TODO: screen shake!!! (also adding that to the weapons??)
		// TODO: AUDIO: Hit sounds!!

		if (currentHealth <= 0) {
			GD.Print("Player died! Now what?");
			// TODO: Death (ez)
			// TODO: Spectator mode (not ez)
			// TODO: Chat system because yeah we need that
			// it was in the original except it was bad. 
		}
	}
}
