using Godot;
using Riptide;
using System;
using System.Security.Cryptography.X509Certificates;

public partial class HealthManager : Node2D
{

	public static HealthManager instance;
	[Export] Inventory inventory;

	int maxHealth;
	int currentHealth;
	int livesCount;
	public int killCount;
	ushort pId;

	[Export] TextureProgressBar healthBar;
	[Export] TextureProgressBar delayHealthBar;
	[Export] Label healthCount;
	[Export] PackedScene damageIndicator;

	enum HealthState { ALIVE, DEAD }
	HealthState healthState = HealthState.ALIVE;
	[Export] Control normalUi;
	[Export] Control spectatorUi;
	[Export] Label spectatorLabel;
	[Export] Label livesLabel;
	[Export] Label killCountLabel;
	[Export] Label livesCountLabel;
	[Export] Control livesCountRoot;

	public delegate void OnHealthStateChangeEventHandler(int state);

	public static event OnHealthStateChangeEventHandler OnChange;

	[Export] Node2D root;
	[Export] CollisionShape2D collision;

	[Export] Control publicKillFeed;
	[Export] Control privateKillFeed;
	[Export] PackedScene killElement;
	[Export] Healable[] heals;
	[Export] TextureProgressBar healIndicator;
 
    public override void _EnterTree()
    {
		instance = this;

		pId = NetworkManager.I.Client.Id;

		livesCount = int.Parse(ConfigManager.CurrentGamerules["lives_count"]);
		if (bool.Parse(ConfigManager.CurrentGamerules["infinite_lives"])) {
			livesCount = -1;
			livesCountRoot.Hide();
		}

        maxHealth = int.Parse(ConfigManager.CurrentGamerules["max_health"]);
		currentHealth = maxHealth;
		healthBar.MaxValue = maxHealth;
		healthBar.Value = maxHealth;
		delayHealthBar.MaxValue = maxHealth;
		delayHealthBar.Value = maxHealth;
    }

	bool isHealing = false;
	int healIndex;
	float timeToHeal;
	float currentTime;

	Vector2 lastPos;
    public override void _Process(double delta)
    {
		healthCount.Text = currentHealth.ToString();
        healthBar.Value = currentHealth;
		delayHealthBar.Value = Mathf.Lerp(delayHealthBar.Value, currentHealth, delta);

		if (healthState == HealthState.DEAD) {
			TickSpectating();
		}

		if (Input.IsActionJustPressed("medkit")) {
			if (inventory.heals[0] < 1) 
				return;
			isHealing = true;
			healIndex = 0;
			timeToHeal = heals[0].timeToUse;
			currentTime = 0;
		}

		if (Input.IsActionJustPressed("syringe")) {
			if (inventory.heals[1] < 1) 
				return;
			
			isHealing = true;
			healIndex = 1;
			timeToHeal = heals[1].timeToUse;
			currentTime = 0;
		}

		if (Input.IsActionJustReleased("medkit") || Input.IsActionJustReleased("syringe")) {
			currentTime = 0;
			isHealing = false;
		}

		if (isHealing) {
			currentTime += (float)delta;
			if (currentTime > timeToHeal) {
				inventory.UpdateUi();
				
				currentTime = 0;
				inventory.heals[healIndex]--;
				currentHealth += (int)Mathf.Floor(heals[healIndex].healAmount * float.Parse(ConfigManager.CurrentGamerules["med_multiplier"]));
				isHealing = false;
				if (currentHealth > maxHealth)
					currentHealth = maxHealth;
			}
		}

		if (Tools.Distance(lastPos, GlobalPosition) > 3) {
			lastPos = GlobalPosition;
			currentTime = 0;
			isHealing = false;
		}

		healIndicator.Visible = currentTime > 0;
		healIndicator.Value = currentTime;
		healIndicator.MaxValue = timeToHeal;
    }

    public override void _PhysicsProcess(double delta)
    {
        switch (healthState) {
			case HealthState.ALIVE:
				root.Visible = true;
				collision.Disabled = false;
				break;
			case HealthState.DEAD:
				root.Visible = false;
				collision.Disabled = true;
				break;
		}

		livesCountLabel.Text = $"x {livesCount}";
		killCountLabel.Text = $"x {killCount}";

		if (respawnTimer == null)
			return;

		if (livesCount > 1) {
			livesLabel.Text = $"You have {livesCount} lives remaining. Respawning in...{Mathf.Round(respawnTimer.TimeLeft)}";
		} else if (livesCount == 1) {
			livesLabel.Text = $"You're on your last life. Don't waste it. Respawning in...{Mathf.Round(respawnTimer.TimeLeft)}";
		} else if (livesCount == 0) {
			livesLabel.Text = $"You've run out of lives! You're out of the game.";
		} else if (livesCount == -1) {
			livesLabel.Text = $"Respawning in...{Mathf.Round(respawnTimer.TimeLeft)}";
		}
    }

    [MessageHandler((ushort)NetworkManager.MessageIds.DamagePlayer)]
	public static void OnPlayerDamaged(Message msg) {
		Vector2 damageOrigin = msg.GetVector2();
		ushort fromId = msg.GetUShort();
		ushort toId = msg.GetUShort();
		ushort itemId = msg.GetUShort();
		int damageAmount = msg.GetInt();

		if (toId != instance.pId)
			return;

		instance.OnPlayerDamaged(damageOrigin, fromId, itemId, damageAmount);
	}

	public void OnPlayerDamaged(Vector2 damageOrigin, ushort fromId, ushort itemId, int damageAmount) {
		currentHealth -= damageAmount;

		DamageIndicator indicator = damageIndicator.Instantiate<DamageIndicator>();
		indicator.GlobalRotation = GlobalPosition.AngleToPoint(damageOrigin);
		AddChild(indicator);

		// TODO: screen shake!!! (also adding that to the weapons??)
		// TODO: AUDIO: Hit sounds!!

		if (currentHealth <= 0) {
			// For technical reasons, players cant kill themselves because i'm too lazy to fix that
			if (fromId == pId) {
				currentHealth = 1;
				return;
			}

			ShowKillChat(fromId, pId, itemId); 

			ChangeHealthState(HealthState.DEAD);

			spectateTarget = GameManager.PlayingPlayers[fromId].playerNode;
			spectateTargetName = NetworkManager.ConnectedPlayers[GameManager.PlayingPlayers[fromId].pId].name;

			if (livesCount != -1) {
				livesCount--;
				if (livesCount != 0) {
					StartRespawn();
				}
			} else {
				StartRespawn();
			}

			Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.PlayerDead);
			msg.AddUShort(pId);
			msg.AddUShort(fromId);
			msg.AddUShort(itemId);
			NetworkManager.I.Client.Send(msg);
		}
	}

	SceneTreeTimer respawnTimer;
	void StartRespawn() {
		respawnTimer = GetTree().CreateTimer(float.Parse(ConfigManager.CurrentGamerules["respawn_time"]));
		respawnTimer.Timeout += Respawn;
	}

	void Respawn() {
		respawnTimer.Timeout -= Respawn;
		GameManager.I.RespawnPlayer(GetParent<Node2D>());
		ChangeHealthState(HealthState.ALIVE);
		// Can you believe that i fucking forgot to reset the player's health????? CAN YOU FUCKING BELIEVE I DIDN'T IMPLEMENT PROBABLY THE MOST IMPORTANT PART OF RESPAWNING????
		currentHealth = maxHealth;

		Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.PlayerRespawn);
		msg.AddUShort(pId);
		NetworkManager.I.Client.Send(msg);
	}

	Node2D spectateTarget;
	string spectateTargetName;

	void TickSpectating() {
		GetParent<Node2D>().GlobalPosition = spectateTarget.GlobalPosition;
		spectatorLabel.Text = $"Spectating: {spectateTargetName}";
	}

	void ChangeHealthState(HealthState newState) {
		healthState = newState;
		OnChange.Invoke((int)newState);

		switch (newState) {
			case HealthState.ALIVE:
				normalUi.Show();
				spectatorUi.Hide();
				break;
			case HealthState.DEAD:
				normalUi.Hide();
				spectatorUi.Show();
				break;
		}
	}

	public void GotKill(ushort victimId) {
		killCount++;
		
		KillElement newElement = killElement.Instantiate<KillElement>();
		newElement.SetupPrivateKill(NetworkManager.ConnectedPlayers[GameManager.PlayingPlayers[victimId].pId].name);
		privateKillFeed.AddChild(newElement);
	}

	public void ShowKillChat(ushort killerId, ushort victimId, ushort itemId) {
		KillElement newElement = killElement.Instantiate<KillElement>();

		string killerName = NetworkManager.ConnectedPlayers[GameManager.PlayingPlayers[killerId].pId].name;
		string victimName = NetworkManager.ConnectedPlayers[GameManager.PlayingPlayers[victimId].pId].name;
		string gunName = GameManager.GetNewInventoryItem(itemId).itemName;

		newElement.SetupPublicKill(victimName, killerName, gunName);

		publicKillFeed.AddChild(newElement);
	}
}
