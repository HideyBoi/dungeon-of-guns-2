using Godot;

public partial class Grenade : InventoryItem {
	[Export] public int count;
	[Export] public float throwPower;
	[Export] public float drag = 2;
	[Export(PropertyHint.Range, "0,1")] public float bounceAmount = 0.3f;
	[Export] public PackedScene blastEffect;
	[Export] public float blastRadius;
	[Export] public Curve damageFalloff;
	[Export] public float damageAmount;
	[Export] bool useFuse;
	[Export] Vector2 fusePos;

	public GrenadeObject grenadeObject;
	public virtual void OnStartUse(GrenadeObject hostObject) {
		grenadeObject = hostObject;
	}
	public virtual void OnFinishedUse() {

	}

}