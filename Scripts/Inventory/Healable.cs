using Godot;

public partial class Healable : InventoryItem {
	[Export] public int count;
    [Export] public float healAmount;
	[Export] public float timeToUse;
	[Export] public HealType healType;

	public enum HealType { MEDKIT, SYRINGE };
}