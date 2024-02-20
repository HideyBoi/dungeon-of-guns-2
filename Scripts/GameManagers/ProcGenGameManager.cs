using System;
using System.Linq;
using Godot;
using Riptide;

public partial class ProcGenGameManager : GameManager {
    public static ProcGenGameManager I;
    
    ushort localId;
    [Export] PackedScene localPlayer;
    [Export] PackedScene remotePlayer;

    public override void _EnterTree()
    {
        I = this;
        NetworkManager.I.Client.ClientDisconnected += DisconnectPlayer;
        localId = NetworkManager.I.Client.Id;

        NetworkManager.CurrentState = NetworkManager.GameState.IN_GAME;
    }

    public override void StartGame()
    {
        // Start the dungeon generator and send clients map data
        DungeonGenerator.I.Start();
        DungeonGenerator.OnComplete += PostGen;
    }

    void PostGen() {
        // Prepare player objects
        NetworkManager.Player[] players = NetworkManager.ConnectedPlayers.Values.ToArray();
        Node2D[] spawnPoints = GetTree().GetNodesInGroup("Spawnpoint").Cast<Node2D>().ToArray();

        for (int i = 0; i < players.Length; i++)
        {
            PlayerObject player = new() {
                pId = players[i].Id,
                playerNode = AddPlayerNode(players[i].Id)
            };
            player.isLocal = player.pId == localId;

            PlayingPlayers.Add(player.pId, player);

            Vector2 furthestSpawn = spawnPoints[0].GlobalPosition;
            float biggestDist = 0;

            for (int j = 0; j < spawnPoints.Length; j++)
            {
                foreach (PlayerObject otherPlayers in PlayingPlayers.Values)
                {
                    float dist = Distance(otherPlayers.playerNode.GlobalPosition, spawnPoints[j].GlobalPosition);
                    if (dist > biggestDist) {
                        biggestDist = dist;
                        furthestSpawn = spawnPoints[j].GlobalPosition;
                    }
                }
            }

            if (player.isLocal) {
                player.playerNode.GlobalPosition = furthestSpawn;
            }

            Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.SpawnNewPlayer);
            msg.AddUShort(player.pId);
            msg.AddFloat(furthestSpawn.X);
            msg.AddFloat(furthestSpawn.Y);
            NetworkManager.I.Client.Send(msg);
        }

        foreach (ushort id in PlayingPlayers.Keys)
        {
            GD.Print(id);
        }
    }

    float Distance(Vector2 v1, Vector2 v2) {
        return Mathf.Sqrt(Mathf.Pow(v1.X - v2.X, 2) + Mathf.Pow(v1.Y - v2.Y, 2));
    }

    [MessageHandler((ushort)NetworkManager.MessageIds.SpawnNewPlayer)]
    public static void HandleNewPlayerSpawn(Message msg) {
        ushort id = msg.GetUShort();
        Vector2 pos = new(msg.GetFloat(), msg.GetFloat());

        PlayerObject player = new() {
            pId = id,
            playerNode = I.AddPlayerNode(id)
        };
        player.isLocal = player.pId == I.localId;

        player.playerNode.GlobalPosition = pos;

        PlayingPlayers.Add(id, player);
    }

    Node2D AddPlayerNode(ushort pId) {
        if (pId == localId) {
            LocalPlayer player = localPlayer.Instantiate<LocalPlayer>();
            player.SetupPlayer(pId);
            AddChild(player);
            return player;
        } else {
            RemotePlayer player = remotePlayer.Instantiate<RemotePlayer>();
            player.SetupPlayer(pId);
            AddChild(player);
            return player;
        }
    }

    void DisconnectPlayer(Object sender, ClientDisconnectedEventArgs e) {
        try {
            PlayingPlayers[e.Id].playerNode.QueueFree();
            PlayingPlayers.Remove(e.Id);
        } catch {
        }
    }
}