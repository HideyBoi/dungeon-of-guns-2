using Godot;

public partial class Weapon : InventoryItem {
    [Export] public int damage;
    [Export] public float damageFalloff; // How much the damage decreases for every in-game tile.
    [Export] public float bloom;
    [Export] public float screenShakeAmount;
    [Export] public MuzzleFlashSize flashSize;
    [Export(PropertyHint.Range, "1,16")] public int shotCount = 1;
    [Export] public float timeBetweenShots;
    [Export] public bool isAutomatic;
    [Export] public int maxAmmo;
    [Export] public AmmoType ammoType;
    [Export] public float reloadTime;
    [Export] public Rarity rarity;
    [Export] public Vector2 muzzleLocation; 
    public int currentAmmo;
    public bool canShoot;

    public enum Rarity { Common, Rare, Legendary }
    public enum AmmoType { Light, Medium, Heavy, Shell }
    public enum MuzzleFlashSize { Light, Medium, Heavy }

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