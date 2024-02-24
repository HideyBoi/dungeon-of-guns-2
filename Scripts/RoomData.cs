using System.Collections.Generic;
using Godot;

public partial class RoomData {
	public Vector2I pos;

	// Top, Right, Bottom, Left
	public bool[] isConnected = new bool[4];

    public int roomId;

	public static int GetCorrespondingSide(int original) {
		return original switch
        {
            0 => 2,
            1 => 3,
            2 => 0,
            3 => 1,
            _ => throw new System.NotImplementedException(),
        };
	}

	public bool GetSide(int sideToGet) {
        return isConnected[sideToGet];
    }

	public void SetSide(int sideToSet, bool value) {
		isConnected[sideToSet] = value;
	}
}