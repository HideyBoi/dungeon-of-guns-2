using Godot;
using Riptide;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

public partial class DungeonGenerator : Node2D
{
	public static DungeonGenerator I;

	private Dictionary<string, string> ruleData;
	
	[Export] int distance;
	[Export] double connectionChance;
	[Export] PackedScene[] possibleRooms;


	[ExportGroup("Debug")]
	[Export] bool force = false;

	RoomData[,] CurrentMap;
	int desiredSize = 0;
	int maxRooms;
	int minRooms;
	int minConnections;

	public override void _Ready()
	{
		I = this;

		if (force) {
			Start();
		}			
	}

	public void Start() {
		ruleData = ConfigManager.LoadGamerulePreset();

		Thread thread = new(StartGen);
		thread.Start();	
	}

    void StartGen() {
		desiredSize = int.Parse(ruleData["map_size"]);

		if (desiredSize < 3) {
			GD.PrintErr("Houston, we fucked up.");
			
			NetworkManager.I.Server.Stop();
			return;
		}

		maxRooms = desiredSize * 2;
		minRooms = (int)(desiredSize * 1.6);
		minConnections = desiredSize * 4;

		CurrentMap = new RoomData[desiredSize * 2, desiredSize * 2];

		GD.Print(CurrentMap.GetLength(0));

		// Set the first room
		RoomData firstRoom = new();
		// GetLength(0) is the X axis, GetLength(1) is the Y axis
		// Set the new room in the middle of the map
		int x = CurrentMap.GetLength(0) / 2;
		int y = CurrentMap.GetLength(1) / 2;
		CurrentMap[x, y] = firstRoom;
		firstRoom.pos = new Vector2I(x, y);
		roomCount = 1;

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

		if (NetworkManager.I.Server.IsRunning) {
			Message headerMessage = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.MapDataHeader);
			headerMessage.AddInt(CurrentMap.GetLength(0));
			headerMessage.AddInt(roomCount);
			NetworkManager.I.Client.Send(headerMessage);
		}

		MainThreadInvoker.InvokeOnMainThread(() => {
			for (int i = 0; i < CurrentMap.GetLength(0); i++)
			{
				for (int j = 0; j < CurrentMap.GetLength(1); j++)
				{
					if (CurrentMap[i, j] != null) {
						int roomId = (int)(GD.Randi() % possibleRooms.Length);
						CurrentMap[i, j].roomId = roomId;

						Room room = possibleRooms[roomId].Instantiate<Room>(); 
						room.SetupRoom(new Vector2I(i, j), CurrentMap[i, j].isConnected);
						room.Position = new Vector2(i, j) * distance;
						AddChild(room);

						Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.MapDataPayload);
						msg.AddInt(i); // x
						msg.AddInt(j); // y
						msg.AddInt(roomId);
						msg.AddBools(CurrentMap[i, j].isConnected);
						NetworkManager.I.Client.Send(msg);
					}
				}
			}

			GD.Print("Room count: " + roomCount);

			//Node2D player = (Node2D)GetTree().GetNodesInGroup("Player")[0];
			//player.Position = (firstRoom.pos * distance) + new Vector2(64, 64);
		});
	}

	[MessageHandler((ushort)NetworkManager.MessageIds.MapDataHeader)]
	public static void HandleMapDataHeader(Message msg) {
		int dataSize = msg.GetInt();

		I.CurrentMap = new RoomData[dataSize, dataSize];

		I.roomCount = msg.GetInt();

		I.finishedPlayers = new();
	}

	[MessageHandler((ushort)NetworkManager.MessageIds.MapDataPayload)]
	public static void HandleMapDataPayload(Message msg) {
		Vector2I dataPos = new Vector2I(msg.GetInt(), msg.GetInt());
		int roomId = msg.GetInt();
		bool[] sides = msg.GetBools();

        RoomData roomData = new()
        {
            pos = dataPos,
            isConnected = sides,
            roomId = roomId
        };
		I.CurrentMap[dataPos.X, dataPos.Y] = roomData;

        Room room = I.possibleRooms[roomId].Instantiate<Room>(); 
		room.SetupRoom(new Vector2I(dataPos.X, dataPos.X), sides);
		room.Position = new Vector2(dataPos.X, dataPos.Y) * I.distance;
		I.AddChild(room);

		I.roomCount++;

		// Do we have all the rooms now?
		if ((I.payloadCount != 0) && (I.roomCount == I.payloadCount)) {
			I.finishedPlayers.Add(NetworkManager.I.Client.Id, true);

			Message done = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.MapDataCompleted);
			done.AddUShort(NetworkManager.I.Client.Id);
			NetworkManager.I.Client.Send(done);
		}
	}

	int payloadCount;
	public Dictionary<ushort, bool> finishedPlayers;
	[MessageHandler((ushort)NetworkManager.MessageIds.MapDataCompleted)]
	public static void HandleMapDataCompleted(Message msg) {
		if (!NetworkManager.I.Server.IsRunning)
			return;
		
		I.finishedPlayers.Add(msg.GetUShort(), true);

		if (I.finishedPlayers.Count == NetworkManager.ConnectedPlayers.Count) {
			Message msg2 = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.CompleteLoading);
			NetworkManager.I.Client.Send(msg2);

			SceneManager.CompleteLoading();
		}
	}

	List<Vector2I> roomsToTick;
	int roomCount;

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

			CurrentMap[tickingRooms[i].X, tickingRooms[i].Y] = newData;
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
				RoomData room = CurrentMap[workingPos.X, workingPos.Y];
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
				RoomData room = CurrentMap[workingPos.X, workingPos.Y];

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
		if (newPos.Y > CurrentMap.GetLength(1) || newPos.Y < 0) {
			throw new IndexOutOfRangeException();
		}
			
		if (newPos.X > CurrentMap.GetLength(0) || newPos.X < 0) {
			throw new IndexOutOfRangeException();
		}
			
		return newPos;
	}
}
