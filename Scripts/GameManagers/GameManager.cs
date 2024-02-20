using System.Collections.Generic;
using Godot;
using Riptide;

public partial class GameManager : Node2D {
    [System.Serializable]
    public class PlayerObject {
        public ushort pId;
        public Node2D playerNode;
        public bool isLocal;
    }

    public static Dictionary<ushort, PlayerObject> PlayingPlayers;

    public override void _Ready()
    {
        PlayingPlayers = new();
    }

    public virtual void StartGame() {
        GD.Print("Starting game");
    }
}