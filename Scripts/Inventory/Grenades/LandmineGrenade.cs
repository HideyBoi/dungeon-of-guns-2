using System;
using Godot;

public partial class LandmineGrenade : Grenade {

	SceneTreeTimer timeTillStart;
	[Export] float timeUntilStart = 1;

    public override void OnFinishedUse()
    {
		timeTillStart = grenadeObject.GetTree().CreateTimer(timeUntilStart);
		timeTillStart.Timeout += StartDetection;
    }

	void StartDetection() {
		timeTillStart.Timeout -= StartDetection;
		grenadeObject.StartScanning();
	}
}