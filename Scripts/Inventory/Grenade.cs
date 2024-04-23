using Godot;

public partial class Grenade : InventoryItem {
	[Export] public int count;
	[Export] public GrenadeType grenadeType;

	// TODO: Grenades

	public enum GrenadeType { STANDARD, MINE };
}