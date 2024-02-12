using Godot;
using System;

public partial class Room : TileMap
{
	[Export] Node2D[] sides = new Node2D[4];
	Vector2I roomPos;

	public void SetupRoom(Vector2I pos, bool[] sideStatues) {
		roomPos = pos;
		
		for (int i = 0; i < sideStatues.Length; i++)
		{
			if (sideStatues[i]) {
				sides[i].GetNode<Node2D>("./True").Visible = true;
			} else {
				sides[i].GetNode<Node2D>("./False").Visible = true;
				sides[i].GetNode<CollisionShape2D>("./False/Wall/Collider").SetDeferred("Disabled", false);
			}
		}
	}

	public void RoomExited(Node2D body, int sideToGet) {
		if (!body.IsInGroup("Player"))
			return;
		
		GD.Print("Leaving room");

		Vector2I newPos = roomPos;

		switch(sideToGet) {
			case 0:
				newPos.Y++;
				break;
			case 1:
				newPos.X++;
				break;
			case 2:
				newPos.Y--;
				break;
			case 3:
				newPos.X--;
				break;
		}

		Room newRoom = DungeonGenerator.I.GameMap[newPos.X, newPos.Y];
		newRoom.GetNode("./" + sideToGet + "/True/playerDropIn");

		body.Position = newRoom.Position;
	}
}
