using Godot;
using System;

public partial class BasicParticleEffect : GpuParticles2D
{
    public override void _EnterTree()
    {
        Emitting = true;
    }

	public void OnFinished() {
		QueueFree();
	}
}
