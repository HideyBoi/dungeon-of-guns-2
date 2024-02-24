using Godot;
using System;

public partial class Room : TileMap
{
	[Export] Node2D[] sides = new Node2D[4];
	Vector2I roomPos;
	public bool[] sideStates;

	public void SetupRoom(Vector2I pos, bool[] sideStatues) {
		roomPos = pos;
		sideStates = sideStatues;
		
		for (int i = 0; i < sideStatues.Length; i++)
		{
			if (sideStatues[i]) {
				sides[i].GetNode<Node2D>("./True").Visible = true;
			} else {
				sides[i].GetNode<Node2D>("./False").Visible = true;
				sides[i].GetNode<CollisionShape2D>("./False/Wall/Collider").SetDeferred("disabled", false);
			}
		}
	}

	public void RoomExited(int sideToGet, Node2D body, Vector2 origin) {
		if (!body.IsInGroup("CanLeaveRoom"))
			return;
		
		//GD.Print("Leaving room");

		body.GetNode<AnimationPlayer>("./transition");

		Vector2I newPos = roomPos;
		Vector2 position;
		Room newRoom = null;
		Node2D dropIn = null;

		switch(sideToGet) {
			case 0:
				newPos.Y--;
				
				newRoom = DungeonGenerator.I.GameMap[newPos.X, newPos.Y];
				dropIn = newRoom.GetNode<Node2D>("./" + 2 + "/True/playerDropIn");

				position = new Vector2(dropIn.GlobalPosition.X + (body.GlobalPosition.X - origin.X), dropIn.GlobalPosition.Y);

				if (body.IsInGroup("Player")) {
					body.GetNode<TransitionAnimation>("./transition").Transition("goDown", position);
				} else {
					body.GlobalPosition = position;
				}

				break;
			case 1:
				newPos.X++;
								
				newRoom = DungeonGenerator.I.GameMap[newPos.X, newPos.Y];
				dropIn = newRoom.GetNode<Node2D>("./" + 3 + "/True/playerDropIn");

				position = new Vector2(dropIn.GlobalPosition.X, dropIn.GlobalPosition.Y + (body.GlobalPosition.Y - origin.Y));

				if (body.IsInGroup("Player")) {
					body.GetNode<TransitionAnimation>("./transition").Transition("goRight", position);
				} else {
					body.GlobalPosition = position;
				}

				break;
			case 2:
				newPos.Y++;

				newRoom = DungeonGenerator.I.GameMap[newPos.X, newPos.Y];
				dropIn = newRoom.GetNode<Node2D>("./" + 0 + "/True/playerDropIn");
			
				position = new Vector2(dropIn.GlobalPosition.X + (body.GlobalPosition.X - origin.X), dropIn.GlobalPosition.Y);
			
				if (body.IsInGroup("Player")) {
					body.GetNode<TransitionAnimation>("./transition").Transition("goUp", position);
				} else {
					body.GlobalPosition = position;
				}

				break;
			case 3:
				newPos.X--;
												
				newRoom = DungeonGenerator.I.GameMap[newPos.X, newPos.Y];
				dropIn = newRoom.GetNode<Node2D>("./" + 1 + "/True/playerDropIn");

				position = new Vector2(dropIn.GlobalPosition.X, dropIn.GlobalPosition.Y + (body.GlobalPosition.Y - origin.Y));

				if (body.IsInGroup("Player")) {
					body.GetNode<TransitionAnimation>("./transition").Transition("goLeft", position);
				} else {
					body.GlobalPosition = position;
				}

				break;
		}
	}
}
