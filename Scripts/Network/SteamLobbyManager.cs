using System;
using Godot;
using Steamworks;

public partial class SteamLobbyManager: Node {
    public static SteamLobbyManager I;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEnter;

    private const string HostAddressKey = "HostAddress";
    public CSteamID LobbyId {get; private set; }

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
            // TODO: Lobby creation failed, tell the user and tell em why.
            return;
        }

        LobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        NetworkManager.I.Server.Start(0, 5);
        NetworkManager.I.Client.Connect("127.0.0.1");
        NetworkManager.CurrentState = NetworkManager.GameState.LOBBY;

        MainMenuUi.current.ShowLobbyScreen();
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

        LobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        CSteamID hostId = SteamMatchmaking.GetLobbyOwner(LobbyId);

        NetworkManager.I.Client.Connect(hostId.ToString());

        MainMenuUi.current.ShowLobbyScreen();
    }

    public void LeaveLobby()
    {
        NetworkManager.I.StopServer();
        NetworkManager.I.DisconnectClient();
        SteamMatchmaking.LeaveLobby(LobbyId);

        SceneManager.I.ChangeScene("res://Objects/GUI/MainMenuUi.tscn");
    }
}