using Godot;
using Riptide;
using System;
using System.Collections.Generic;
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

	RoomData[,] currentMap;
	public Room[,] GameMap {get; private set;}
	int desiredSize = 0;
	int maxRooms;
	int minRooms;
	int minConnections;

	public delegate void OnDungeonCompleteEventHandler();
	public static event OnDungeonCompleteEventHandler OnComplete;

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

		currentMap = new RoomData[desiredSize * 2, desiredSize * 2];
		GameMap = new Room[desiredSize * 2, desiredSize * 2];

		// GetLength(0) is the X axis, GetLength(1) is the Y axis
		// Set the new room in the middle of the map
		int x = currentMap.GetLength(0) / 2;
		int y = currentMap.GetLength(1) / 2;

		roomsToTick = new List<Vector2I>
        {
            new(x,y)
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

		finishedPlayers = new();
		if (NetworkManager.ConnectedPlayers.Count == 1) {
			Message done = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.MapDataCompleted);
			done.AddUShort(NetworkManager.I.Client.Id);
			HandleMapDataCompleted(done);
		} else {
			finishedPlayers.Add(NetworkManager.I.Client.Id, true);
		}
      		

		List<int> mapX = new();
		List<int> mapY = new();
		List<int> mapRoomId = new();
		List<bool> mapConnections = new();

		MainThreadInvoker.InvokeOnMainThread(() => {
			int localCount = 0;

			for (int i = 0; i < currentMap.GetLength(0); i++)
			{
				for (int j = 0; j < currentMap.GetLength(1); j++)
				{
					if (currentMap[i, j] != null) {
						localCount++;

						int roomId = (int)(GD.Randi() % possibleRooms.Length);
						currentMap[i, j].roomId = roomId;

						Room room = possibleRooms[roomId].Instantiate<Room>(); 
						room.SetupRoom(new Vector2I(i, j), currentMap[i, j].isConnected);
						room.Position = new Vector2(i, j) * distance;
						GameMap[i,j] = room;
						AddChild(room);

						// add room data to payload
						mapX.Add(i);
						mapY.Add(j);
						mapRoomId.Add(roomId);
						for (int x = 0; x < currentMap[i,j].isConnected.Length; x++)
						{
							mapConnections.Add(currentMap[i,j].isConnected[x]);
						}
					}
				}
			}

			GD.Print("Room count: " + roomCount);
			GD.Print("Room local count: " + localCount);

			//Node2D player = (Node2D)GetTree().GetNodesInGroup("Player")[0];
			//player.Position = (firstRoom.pos * distance) + new Vector2(64, 64);

			Message roomData = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.MapData);
			// header
			roomData.AddInt(currentMap.GetLength(0));
			roomData.AddInt(localCount);
			// payload
			roomData.AddInts(mapX.ToArray());
			roomData.AddInts(mapY.ToArray());
			roomData.AddInts(mapRoomId.ToArray());
			roomData.AddBools(mapConnections.ToArray());

			NetworkManager.I.Client.Send(roomData);

			OnComplete.Invoke();
		});
	}

	[MessageHandler((ushort)NetworkManager.MessageIds.MapData)]
	public static void HandleMapData(Message msg) {
		int dataLength = msg.GetInt();
		int roomCount = msg.GetInt();

		// Setup standard data
		I.currentMap = new RoomData[dataLength, dataLength];
		I.GameMap = new Room[dataLength, dataLength];

		int[] roomX = msg.GetInts();
		int[] roomY = msg.GetInts();
		int[] roomId = msg.GetInts();
		bool[] connections = msg.GetBools();
		
		GD.Print(roomCount);
		GD.Print(connections.Length);

		for (int i = 0; i < roomCount; i++)
		{			
			// Assemble bool array from composite bool array
			List<bool> roomConnections = new();
			for (int j = i * 4; j < (i * 4) + 4; j++)
			{
				roomConnections.Add(connections[j]);
			}

			RoomData roomData = new() {
				pos = new Vector2I(roomX[i], roomY[i]),
				roomId = roomId[i],
				isConnected = roomConnections.ToArray()
			};

			I.currentMap[roomX[i], roomY[i]] = roomData;

			Room room = I.possibleRooms[roomId[i]].Instantiate<Room>();
			room.SetupRoom(new Vector2I(roomX[i], roomY[i]), roomConnections.ToArray());
			room.Position = new Vector2(roomX[i], roomY[i]) * I.distance;

			I.AddChild(room);
		}

		Message done = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.MapDataCompleted);
		done.AddUShort(NetworkManager.I.Client.Id);
		NetworkManager.I.Client.Send(done);
	}

	public Dictionary<ushort, bool> finishedPlayers;

	[MessageHandler((ushort)NetworkManager.MessageIds.MapDataCompleted)]
	public static void HandleMapDataCompleted(Message msg) {
		if (!NetworkManager.I.Server.IsRunning)
			return;

		ushort pId = msg.GetUShort();
		GD.Print("Player " + pId + " finished loading the map.");
		I.finishedPlayers.Add(pId, true);

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
			roomCount++;
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
