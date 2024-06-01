using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Riptide;
using Riptide.Transports.Steam;
using Riptide.Utils;

partial class NetworkManager : Node {
	public bool isSteamServer = false;

	[Serializable]
	public class Player {
		public ushort Id = 0;
		public string name = "";
	}
	
	public enum MessageIds : ushort
	{
		Hello = 1,
		GameruleData,
		ReadyState,
		StartSceneLoad,
		DoneLoading,
		CompleteLoading,
		MapData,
		MapDataCompleted,
		SpawnNewPlayer,
		PlayerPosRot,
		ItemSpawn,
		ItemRemove,
		ItemMove,
		ChestOpened,
		ChestRegenerated,
		AmmoOpened,
		AmmoRegenerated,
		PlayerShot,
		DamagePlayer,
		PlayerDead,
		PlayerRespawn,
	}

	public const byte MessageHandlerGroupId = 206;

	public static NetworkManager I;
	
	internal Server Server { get; private set; }
	internal Client Client { get; private set; }

	public static Dictionary<ushort, Player> ConnectedPlayers = new();

	public enum GameState { NOT_CONNECTED, LOBBY, LOADING_GAME, IN_GAME, GAME_END }
	public static GameState CurrentState = GameState.NOT_CONNECTED; 

	public override void _Ready()
	{
		String[] args = OS.GetCmdlineUserArgs();
		if (isSteamServer) {
			isSteamServer = !args.Contains("--local_mode");
		}

		I = this;

		if (!SteamManager.Initialized)
		{
			GD.PrintErr("Steam is not initialized!");
			return;
		}

		RiptideLogger.Initialize(GD.Print, false);


		if (isSteamServer) {
			SteamServer steamServer = new SteamServer();
			Server = new Server(steamServer);
			Client = new Client(new SteamClient(steamServer));
		} else {
			GD.PushWarning("/!\\ LOADING LOCAL MODE. IS THIS WHAT YOU WANTED TO DO?");
			Server = new();
			Client = new();
		}

		Server.ClientConnected += NewPlayerConnected;

		Client.Connected += DidConnect;
		Client.ConnectionFailed += FailedToConnect;
		Client.ClientDisconnected += ClientPlayerLeft;
		Client.Disconnected += DidDisconnect;

		List<ushort> msgIdsToRelay = new();

		foreach (MessageIds id in Enum.GetValues(typeof(MessageIds)))
		{
			msgIdsToRelay.Add((ushort)id);
		}

		// Value needs to be 1 more than the highest ID in MessageIds
		MessageRelayFilter filter = new MessageRelayFilter(msgIdsToRelay.Count + 1);

		foreach (var id in msgIdsToRelay)
		{
			filter.EnableRelay(id);
		}

		Server.RelayFilter = filter;
	}

	public override void _Process(double delta)
	{
		if (Client == null)
			return;
		
		if (Server.IsRunning)
			Server.Update();

		Client.Update();
	}

	public override void _ExitTree()
	{
		StopServer();
		Server.ClientConnected -= NewPlayerConnected;

		DisconnectClient();
		Client.Connected -= DidConnect;
		Client.ConnectionFailed -= FailedToConnect;
		Client.ClientDisconnected -= ClientPlayerLeft;
		Client.Disconnected -= DidDisconnect;
	}

	// Tell local server to stop
	internal void StopServer()
	{
		Server.Stop();
	}

	// Tell local client to disconnect
	internal void DisconnectClient()
	{
		Client.Disconnect();
	}

	private void NewPlayerConnected(object sender, ServerConnectedEventArgs e)
	{
		if (CurrentState != GameState.LOBBY && Server.IsRunning) {
			Server.DisconnectClient(e.Client.Id);
		}

		Message msg = Message.Create(MessageSendMode.Reliable, MessageIds.Hello);
		msg.AddUShort(Client.Id);
		msg.AddString(Steamworks.SteamFriends.GetPersonaName());
		Client.Send(msg);
	}

	private void DidConnect(object sender, EventArgs e)
	{
		Message msg = Message.Create(MessageSendMode.Reliable, MessageIds.Hello);
		msg.AddUShort(Client.Id);
		msg.AddString(Steamworks.SteamFriends.GetPersonaName());
		Client.Send(msg);
		GD.Print("Connected to " + SteamLobbyManager.I.LobbyId);
		ConnectedPlayers.Add(Client.Id, new Player {
			name = Steamworks.SteamFriends.GetPersonaName(),
			Id = Client.Id
		});
	}

	[MessageHandler((ushort)MessageIds.Hello)]
	static void NewPlayer(Message msg) {
		ushort Id = msg.GetUShort();
		string name = msg.GetString();

		// Player already handled, disregard.
		if (ConnectedPlayers.ContainsKey(Id))
			return;

		Player newPlayer = new()
		{
			Id = Id,
			name = name
		};
		ConnectedPlayers.Add(Id, newPlayer);

		foreach (Player player in ConnectedPlayers.Values) {
			GD.Print(player.name + " ID: " + player.Id);
		}
	}

	// Failed to connect to game, go back to main menu.
	private void FailedToConnect(object sender, EventArgs e)
	{
		SceneManager.I.ChangeScene("res://Objects/GUI/MainMenuUi.tscn");
		GD.Print(sender);
		GD.Print(e);
		// TODO: Tell em why you couldn't connect.
	}

	// Remote player left, what to do... what to do...
	private void ClientPlayerLeft(object sender, ClientDisconnectedEventArgs e)
	{	   
		ConnectedPlayers.Remove(e.Id);
	}

	// Local player disconnected, what to do... what to do...
	private void DidDisconnect(object sender, EventArgs e)
	{
		CurrentState = GameState.NOT_CONNECTED;
		ConnectedPlayers.Clear();

		SceneManager.I.ChangeScene("res://Objects/GUI/MainMenuUi.tscn");
	}

}