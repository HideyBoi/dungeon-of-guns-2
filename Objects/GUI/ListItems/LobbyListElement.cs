using Godot;
using Steamworks;
using System;

public partial class LobbyListElement : Panel
{
	CSteamID currentLobbyId;
	[Export] Label usernameLabel;
	[Export] Label playerPingInfo;

	public void Setup(string username, string currentPlayers, string maxPlayers, CSteamID lobbyId) {
		usernameLabel.Text = " " + username;
		playerPingInfo.Text = $"{currentPlayers}/{maxPlayers}";
		currentLobbyId = lobbyId;
	}

	public void Join() {
		SteamLobbyManager.I.JoinLobby(currentLobbyId);
	}
}
