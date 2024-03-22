using Godot;

public partial class Ammo : InventoryItem {
	[Export] public int count;
    [Export] public Weapon.AmmoType ammoType;
}