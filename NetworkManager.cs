using Godot;
using System;

public partial class NetworkManager : Node
{
	public static NetworkManager CurrentInstance;

    public override void _EnterTree()
    {
		if (CurrentInstance == null)
			CurrentInstance = this;

		GD.Print(CurrentInstance);
    }

	internal enum MessageIDs : ushort {
		PlayerInfo = 1
	}
}
