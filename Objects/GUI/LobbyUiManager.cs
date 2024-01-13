using Godot;
using Godot.Collections;
using System;

public partial class LobbyUiManager : Control
{
    int lastCount = -1;
    [Export] VBoxContainer playerListContainer;
    [Export] PackedScene playerListItem;

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
}
