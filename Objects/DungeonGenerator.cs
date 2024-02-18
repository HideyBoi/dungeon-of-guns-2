using Godot;
using Riptide;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public partial class DungeonGenerator : Node2D
{
	public static DungeonGenerator I;

	private Dictionary<string, string> ruleData;
	
	[Export] int distance;
	[Export] double connectionChance;
	[Export] PackedScene[] possibleRooms;


	[ExportGroup("Debug")]
	[Export] bool force = false;

	RoomData[,] currentMap; 
	public Room[,] GameMap { get; private set;}
	int desiredSize = 0;
	int maxRooms;
	int minRooms;
	int minConnections;

	public override void _Ready()
	{
		I = this;

		if (NetworkManager.I.Server.IsRunning || force) {
			ruleData = ConfigManager.LoadGamerulePreset();

			Thread thread = new(StartGen);
			thread.Start();	
		}			
	}

    void StartGen() {
		desiredSize = int.Parse(ruleData["map_size"]);

		if (desiredSize < 3) {
			GD.PrintErr("Houston, we fucked up.");
			
			//NetworkManager.I.Server.Stop();
			//return;
		}

		maxRooms = desiredSize * 2;
		minRooms = (int)(desiredSize * 1.6);
		minConnections = desiredSize * 4;

		currentMap = new RoomData[desiredSize * 2, desiredSize * 2];
		GameMap = new Room[desiredSize * 2, desiredSize * 2];

		GD.Print(currentMap.GetLength(0));

		// Set the first room
		RoomData firstRoom = new();
		// GetLength(0) is the X axis, GetLength(1) is the Y axis
		// Set the new room in the middle of the map
		int x = currentMap.GetLength(0) / 2;
		int y = currentMap.GetLength(1) / 2;
		currentMap[x, y] = firstRoom;
		firstRoom.pos = new Vector2I(x, y);

		roomsToTick = new List<Vector2I>
        {
            firstRoom.pos
        };

		bool stillWorking = true;
		while(stillWorking) {
			TickCreateRoomData();
			stillWorking = roomsToTick.Count > 0;
		}

		if (roomCount < minRooms) {
			GD.Print($"Expected {minRooms}, got {roomCount}. Restarting generation.");
			StartGen();		
			return;
		}

		Message headerMessage = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.MapDataHeader);
		headerMessage.AddInt(currentMap.GetLength(0));

		if (NetworkManager.I.Server.IsRunning) {
			NetworkManager.I.Client.Send(headerMessage);
		}

		MainThreadInvoker.InvokeOnMainThread(() => {
			for (int i = 0; i < currentMap.GetLength(0); i++)
			{
				for (int j = 0; j < currentMap.GetLength(1); j++)
				{
					if (currentMap[i, j] != null) {

						Room room = possibleRooms[GD.Randi() % possibleRooms.Length].Instantiate<Room>(); 
						room.SetupRoom(new Vector2I(i, j), currentMap[i, j].isConnected);
						room.Position = new Vector2(i, j) * distance;
						GameMap[i,j] = room;
						AddChild(room);
					}
				}
			}

			GD.Print("Room count: " + roomCount);

			Node2D player = (Node2D)GetTree().GetNodesInGroup("Player")[0];
			player.Position = firstRoom.pos * distance;
		});
	}

	List<Vector2I> roomsToTick;
	int roomCount = 1;

	void TickCreateRoomData() {
		List<Vector2I> tickingRooms = new(); 
		tickingRooms.AddRange(roomsToTick); // Copy the array so that i don't have to figure out those for loop things
		roomsToTick.Clear(); // Erase the original array so that we don't do the same work twice.

		for (int i = 0; i < tickingRooms.Count; i++)
		{
            RoomData newData = new()
            {
                pos = tickingRooms[i],
				// Checks connections and adds additional connections to other rooms
				isConnected = CheckSides(tickingRooms[i]),
            };

			currentMap[tickingRooms[i].X, tickingRooms[i].Y] = newData;
        }
	}

	bool[] CheckSides(Vector2I pos) {
		
		bool[] sides = new bool[4];

		for (int i = 0; i < 4; i++)
		{
			Vector2I workingPos;
			try {
				// Get the room slot in the direction of that is being checked
				workingPos = GetDir(i, pos);

				// Check if a room is already connecting to this room
				RoomData room = currentMap[workingPos.X, workingPos.Y];
				if (room != null) {
					if (room.GetSide(RoomData.GetCorrespondingSide((RoomData.Sides)i))) {
						sides[i] = true;
						continue;
					}
				}
			} catch (IndexOutOfRangeException) {
				continue;
			}

			// Move on if not adding a room here
			double chance = GD.RandRange(0, 100);
			if (chance > connectionChance) {
				sides[i] = false;
			} else {
				RoomData room = currentMap[workingPos.X, workingPos.Y];

				// Check if room already exists in this position.
				if (room == null) {	
					if (roomCount < maxRooms) {
						roomsToTick.Add(workingPos);
						roomCount++;
						sides[i] = true;  
					}          			
				} else {
					GD.Print(i + " -> " + RoomData.GetCorrespondingSide((RoomData.Sides)i));
					room.SetSide(RoomData.GetCorrespondingSide((RoomData.Sides)i), true);
					sides[i] = true;
				}
			}
		}

		return sides;
	}

	Vector2I GetDir(int sideNum, Vector2I current) {
		Vector2I newPos = current;

		switch(sideNum) {
			case 0:
				newPos.Y--;
				break;
			case 1:
				newPos.X++;
				break;
			case 2:
				newPos.Y++;
				break;
			case 3:
				newPos.X--;
				break;
		}

		// Make sure position is on the board
		if (newPos.Y > currentMap.GetLength(1) || newPos.Y < 0) {
			throw new IndexOutOfRangeException();
		}
			
		if (newPos.X > currentMap.GetLength(0) || newPos.X < 0) {
			throw new IndexOutOfRangeException();
		}
			
		return newPos;
	}
}
