using Godot;
using Godot.Collections;
using Riptide;
using Steamworks;
using System;
using TextCopy;

public partial class LobbyUiManager : Control
{
	public static LobbyUiManager I;
	ushort id;
	int lastCount = -1;
	[Export] VBoxContainer playerListContainer;
	[Export] PackedScene playerListItem;
	[Export] PackedScene gameWorld;
	[Export] Button readyButton;
	[Export] Button hostSettingsButton;
	[Export] ConfirmationDialog leaveGameDialog;
	[Export] OptionButton privacySelector;

    public override void _Ready()
    {
		I = this;
		id = NetworkManager.I.Client.Id;
		NetworkManager.I.Client.ClientDisconnected += ClientPlayerLeft;

		if (!NetworkManager.I.Server.IsRunning) {
			hostSettingsButton.Hide();
		} else {
			switch (SteamLobbyManager.I.firstLobbyType) {
				case ELobbyType.k_ELobbyTypeFriendsOnly:
					privacySelector.Selected = 0;
					break;
				case ELobbyType.k_ELobbyTypePublic:
					privacySelector.Selected = 1;
					break;
				case ELobbyType.k_ELobbyTypePrivate:
					privacySelector.Selected = 2;
					break;
			}
		}
    }

    public override void _Process(double delta)
	{
		if (NetworkManager.ConnectedPlayers.Count == lastCount)
			return;

		Array<Node> nodes = playerListContainer.GetChildren();

		// Check if a new list item is needed.
		foreach (NetworkManager.Player player in NetworkManager.ConnectedPlayers.Values)
		{
			bool added = false;
			foreach (Node listItem in nodes)
			{
				if (player.Id.ToString() == listItem.Name) {
					added = true;
					break;
				}
			}

			if (!added) {
				PlayerlistItem newItem = playerListItem.Instantiate<PlayerlistItem>();
				newItem.Setup(player.name, player.Id);
				playerListContainer.AddChild(newItem);

				return;
			}		 
		}

		// Check if a list item needs to be removed.
		foreach (Node node in nodes)
		{
			bool missing = true;
			foreach (NetworkManager.Player player in NetworkManager.ConnectedPlayers.Values)
			{
				if (node.Name == player.Id.ToString()) {
					missing = false;
				}
			}

			if (missing) {
				node.QueueFree();
			}
		}
	}

	public void ShowLeaveGameConfirmation() {
		leaveGameDialog.Show();
		if (NetworkManager.I.Client.IsConnected) {
			leaveGameDialog.DialogText = "Are you sure you want to return to the main menu and disconnect all connected players?";
		} else {
			leaveGameDialog.DialogText = "Are you sure you want to disconnect from the lobby and return to the main menu?";
		}
	}
	public void LeaveGame() {
		SteamLobbyManager.I.LeaveLobby();
	}

	public void CopyLobbyId() {
		ClipboardService.SetText(SteamLobbyManager.I.LobbyId.ToString());
	}

	bool ready = false;
	Dictionary<ushort, bool> readyPlayers = new();

	public void ReadyButton() {
		if (ready) {
			readyPlayers.Remove(id);
			readyButton.Text = "Ready up!";
		} else {
			readyPlayers.Add(id, true);
			readyButton.Text = "Unready";
		}
		ready = !ready;

		Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.ReadyState);
		msg.AddUShort(id);
		msg.AddBool(ready);
		NetworkManager.I.Client.Send(msg);

		if (NetworkManager.I.Server.IsRunning) {
			CheckReadyState();
		}

		playerListContainer.GetNode<PlayerlistItem>(id.ToString()).SetReady(ready);
	}

	[MessageHandler((ushort)NetworkManager.MessageIds.ReadyState)]
	public static void ReadyStateChange(Message msg) {
		ushort pId = msg.GetUShort();
		bool isReady = msg.GetBool();

		GD.Print(pId + " is ready " + isReady);

		if (isReady) {
			I.readyPlayers.Add(pId, isReady);
		} else {
			I.readyPlayers.Remove(pId);
		}

		I.playerListContainer.GetNode<PlayerlistItem>(pId.ToString()).SetReady(isReady);

		if (NetworkManager.I.Server.IsRunning) {
			I.CheckReadyState();
		}	
	}

	void CheckReadyState() {
		if (readyPlayers.Count == NetworkManager.ConnectedPlayers.Count) {
			// Trigger sync
			ConfigManager.SaveGamerules();
			SceneManager.I.ChangeScene(gameWorld.ResourcePath, sync: true);
		}
	}

	private void ClientPlayerLeft(object sender, ClientDisconnectedEventArgs e)
	{	   
		try {
			readyPlayers.Remove(e.Id);
		} catch {
			GD.Print("Player left, but was not ready, so nothing to remove.");
		}
	}

	public void OnLobbyPrivacyChanged(int newPrivacy) {
		ELobbyType type = ELobbyType.k_ELobbyTypePrivate;
		switch (newPrivacy) {
			case 0:
				// Visible only to friends and people who are invited.
				type = ELobbyType.k_ELobbyTypeFriendsOnly;
				break;
			case 1:
				// Visible to everyone.
				type = ELobbyType.k_ELobbyTypePublic;
				break;
			case 2:
				// Invisible to everyone, invite only.
				type = ELobbyType.k_ELobbyTypePrivate;
				break;
			case 3:
				// Invisible to friends but visible to the server list.
				// Not supported.
				type = ELobbyType.k_ELobbyTypeInvisible;
				break;
		}
		bool success = SteamMatchmaking.SetLobbyType(SteamLobbyManager.I.LobbyId, type);
		GD.Print($"Lobby privacy was changed: {success}");
	}

    public override void _ExitTree()
    {
        NetworkManager.I.Client.ClientDisconnected -= ClientPlayerLeft;
    }
}
