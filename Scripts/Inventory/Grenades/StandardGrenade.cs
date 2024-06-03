using System;
using Godot;

public partial class StandardGrenade : Grenade {

	[Export] float timeUntilBoom;


	SceneTreeTimer boomTimer;	
    public override void OnStartUse(GrenadeObject hostObject)
    {
        base.OnStartUse(hostObject);

		boomTimer = hostObject.GetTree().CreateTimer(timeUntilBoom);
		boomTimer.Timeout += Timeup;
    }

	void Timeup() {
		boomTimer.Timeout -= Timeup;
		grenadeObject.Explode();
	}

}