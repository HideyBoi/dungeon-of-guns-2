using System;
using Godot;
using Steamworks;

public partial class SteamLobbyManager: Node {
    public static SteamLobbyManager I;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEnter;

    private const string HostAddressKey = "HostAddress";
    public CSteamID lobbyId;

    public override void _Ready()
    {
        I = this;

        if (!SteamManager.Initialized)
        {
            GD.PrintErr("Steam is not initialized!");
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
    }

    public void CreateLobby(ELobbyType lobbyType, int maxPlayers)
    {
        SteamMatchmaking.CreateLobby(lobbyType, maxPlayers);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            // TODO: Lobby creation failed, tell the user and probably tell em why.
            return;
        }

        lobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        // TODO: Lobby was created. Now show that

        NetworkManager.I.Server.Start(0, 5, NetworkManager.MessageHandlerGroupId);
        NetworkManager.I.Client.Connect("127.0.0.1", messageHandlerGroupId: NetworkManager.MessageHandlerGroupId);
        NetworkManager.CurrentState = NetworkManager.GameState.LOBBY;
    }

    public void JoinLobby(ulong lobbyId)
    {       
        SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        if (NetworkManager.I.Client.IsNotConnected)
            SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEnter(LobbyEnter_t callback)
    {
        if (NetworkManager.I.Server.IsRunning)
            return;

        lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        CSteamID hostId = SteamMatchmaking.GetLobbyOwner(lobbyId);

        NetworkManager.I.Client.Connect(hostId.ToString(), messageHandlerGroupId: NetworkManager.MessageHandlerGroupId);
        // TODO: Game was connected, now what?
    }

    internal void LeaveLobby()
    {
        NetworkManager.I.StopServer();
        NetworkManager.I.DisconnectClient();
        SteamMatchmaking.LeaveLobby(lobbyId);
    }
}