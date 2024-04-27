using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public List<int> rareChestItems = new();
    public List<int> midChestItems = new();
    public List<int> commonChestItems = new();
    public List<int> secondaries = new();
    public List<int> ammo = new();

    public static Dictionary<ushort, PlayerObject> PlayingPlayers = new();

    public virtual void StartGame() {
        SetupItems();
    }

    public void SetupItems() {
        // For network syncing 
        for (int i = 0; i < possibleItems.Length; i++)
        {
            possibleItems[i].itemId = (ushort)i;
        }

        foreach (InventoryItem item in possibleItems)
        {
            if (item is Ammo) {
                ammo.Add(item.itemId);
            }
            if (item is Healable || item is Grenade) {
                secondaries.Add(item.itemId);
            }
            if (item is Weapon) {
                Weapon weapon = (Weapon)item;
                switch (weapon.rarity) {
                    case Weapon.Rarity.Legendary:
                        rareChestItems.Add(item.itemId);
                        break;
                    case Weapon.Rarity.Rare:
                        midChestItems.Add(item.itemId);
                        break;
                    case Weapon.Rarity.Common:
                        commonChestItems.Add(item.itemId);
                        break;
                }
            }
        }
    }

    public static InventoryItem GetNewInventoryItem(ushort itemId) {
        return (InventoryItem)I.possibleItems[itemId].Duplicate(true);
    }
}