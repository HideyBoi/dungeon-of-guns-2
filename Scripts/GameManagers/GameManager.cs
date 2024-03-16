using System.Collections.Generic;
using Godot;
using Riptide;

public partial class GameManager : Node2D {
    public static GameManager I;

    [System.Serializable]
    public class PlayerObject {
        public ushort pId;
        public Node2D playerNode;
        public bool isLocal;
    }

    [Export] public InventoryItem[] possibleItems;

    public static Dictionary<ushort, PlayerObject> PlayingPlayers = new();

    public virtual void StartGame() {
        // For network syncing 
        for (int i = 0; i < possibleItems.Length; i++)
        {
            possibleItems[i].itemId = (ushort)i;
        }
    }
}