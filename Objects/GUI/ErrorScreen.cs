using Godot;
using System;

public partial class ErrorScreen : CanvasLayer
{
	public static ErrorScreen instance;

	[Export] Control steamConnectErrorScreen;
	[Export] Control steamLibraryErrorScreen;

    public override void _EnterTree()
    {
        instance = this;
    }

    public void ShowSteamConnectError() {
		steamConnectErrorScreen.Show();
	}

	public void ShowSteamLibraryError() {
		steamLibraryErrorScreen.Show();
	}

	public void Quit() {
		GD.Print("User closed game via critical error screen.");
		GetTree().Quit();
	}
}
