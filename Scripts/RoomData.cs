using System.Collections.Generic;
using Godot;

public partial class RoomData {
	public Vector2I pos;

	public enum Sides {TOP, RIGHT, BOTTOM, LEFT}

	// Top, Right, Bottom, Left
	public bool[] isConnected = new bool[4];

    public int roomId;

	public static Sides GetCorrespondingSide(Sides original) {
		return original switch
        {
            Sides.TOP => Sides.BOTTOM,
            Sides.RIGHT => Sides.LEFT,
            Sides.BOTTOM => Sides.TOP,
            Sides.LEFT => Sides.RIGHT,
            _ => throw new System.NotImplementedException(),
        };
	}

	public bool GetSide(Sides sideToGet) {
        return sideToGet switch
        {
            Sides.TOP => isConnected[0],
            Sides.RIGHT => isConnected[1],
            Sides.BOTTOM => isConnected[2],
            Sides.LEFT => isConnected[3],
            _ => false,
        };
    }

	public void SetSide(Sides sideToSet, bool value) {
		isConnected[(int)sideToSet] = value;
	}
}