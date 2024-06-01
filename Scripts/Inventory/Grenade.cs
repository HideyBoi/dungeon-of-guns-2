using Godot;

public partial class Grenade : InventoryItem {
	[Export] public int count;
	[Export] public float maxThrowPower;
	[Export] public float minThrowPower;
	[Export] public PackedScene blastEffect;
	[Export] public float blastRadius;
	[Export] public Curve2D damageFalloff;
	[Export] public float damageAmount;

	GrenadeObject grenadeObject;
	public virtual void OnStartUse(GrenadeObject hostObject) {
		grenadeObject = hostObject;
	}
	public virtual void OnFinishedUse() {

	}

}