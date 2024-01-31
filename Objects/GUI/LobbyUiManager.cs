using Godot;
using Godot.Collections;
using Riptide;
using System;

public partial class LobbyUiManager : Control
{
	public static LobbyUiManager I;
	ushort id;
	int lastCount = -1;
	[Export] VBoxContainer playerListContainer;
	[Export] PackedScene playerListItem;
	[Export] PackedScene gameWorld;
	[Export] Button readyButton;

    public override void _Ready()
    {
		I = this;
		id = NetworkManager.I.Client.Id;
		NetworkManager.I.Client.ClientDisconnected += ClientPlayerLeft;
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

		if (NetworkManager.I.Server.IsRunning) {
			I.CheckReadyState();
		}	
	}

	void CheckReadyState() {
		if (readyPlayers.Count == NetworkManager.ConnectedPlayers.Count) {
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

    public override void _ExitTree()
    {
        NetworkManager.I.Client.ClientDisconnected -= ClientPlayerLeft;
    }
}
