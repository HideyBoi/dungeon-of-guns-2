using Godot;

public partial class Weapon : InventoryItem {
    [Export] public int damage;
    [Export] public int currentAmmo;
    [Export] public int maxAmmo;
    [Export] public Rarity rarity;
    [Export] public AmmoType ammoType;

    public enum Rarity { Common, Rare, Legendary }
    public enum AmmoType { Light, Medium, Heavy, Shell }

    public static string GetRarityText(Rarity rarity) {
        switch (rarity)
        {
            case Rarity.Common:
                return "Common";
            case Rarity.Rare:
                return "Rare";
            case Rarity.Legendary:
                return "Legendary";
            default:
                return "you're fucked.";
        }
    }
}