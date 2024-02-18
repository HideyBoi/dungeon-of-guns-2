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
				sides[i].GetNode<CollisionShape2D>("./False/Wall/Collider").SetDeferred("disabled", false);
			}
		}
	}

	public void RoomExited(int sideToGet, Node2D body, Vector2 origin) {
		if (!body.IsInGroup("Player"))
			return;
		
		//GD.Print("Leaving room");

		AnimationPlayer anim = body.GetNode<AnimationPlayer>("./transition");

		Vector2I newPos = roomPos;
		Room newRoom = null;
		Node2D dropIn = null;

		switch(sideToGet) {
			case 0:
				newPos.Y--;
				
				newRoom = DungeonGenerator.I.GameMap[newPos.X, newPos.Y];
				dropIn = newRoom.GetNode<Node2D>("./" + 2 + "/True/playerDropIn");

				body.GlobalPosition = new Vector2(dropIn.GlobalPosition.X + (body.GlobalPosition.X - origin.X), dropIn.GlobalPosition.Y);
				anim.Play("goUp");

				break;
			case 1:
				newPos.X++;
								
				newRoom = DungeonGenerator.I.GameMap[newPos.X, newPos.Y];
				dropIn = newRoom.GetNode<Node2D>("./" + 3 + "/True/playerDropIn");

				body.GlobalPosition = new Vector2(dropIn.GlobalPosition.X, dropIn.GlobalPosition.Y + (body.GlobalPosition.Y - origin.Y));
				anim.Play("goRight");

				break;
			case 2:
				newPos.Y++;

				newRoom = DungeonGenerator.I.GameMap[newPos.X, newPos.Y];
				dropIn = newRoom.GetNode<Node2D>("./" + 0 + "/True/playerDropIn");
			
				body.GlobalPosition = new Vector2(dropIn.GlobalPosition.X + (body.GlobalPosition.X - origin.X), dropIn.GlobalPosition.Y);
				anim.Play("goDown");

				break;
			case 3:
				newPos.X--;
												
				newRoom = DungeonGenerator.I.GameMap[newPos.X, newPos.Y];
				dropIn = newRoom.GetNode<Node2D>("./" + 1 + "/True/playerDropIn");

				body.GlobalPosition = new Vector2(dropIn.GlobalPosition.X, dropIn.GlobalPosition.Y + (body.GlobalPosition.Y - origin.Y));
				anim.Play("goLeft");

				break;
		}
	}
}
