using Godot;
using System;

public partial class IntroScene : Node2D
{

	[Export] PackedScene sceneManager;

    public override void _EnterTree()
    {
        ConfigManager.CheckVersions();
		// Pass scene handling onto the scene manager
		AddChild(sceneManager.Instantiate());
    }
}
