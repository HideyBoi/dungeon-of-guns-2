using Godot;
using Steamworks;
using System;

public partial class MainMenuUi : Control
{
	public static MainMenuUi current;

	[Export] PackedScene lobby;
	[Export] SpinBox maxPlayersSelector;
	[Export] OptionButton lobbyPrivacySelector;
	[Export] LineEdit lobbyIdInput;
	[Export] CanvasLayer connectingScreen;

	public override void _Ready()
	{
		current = this;
	}

	public void StartHosting() {
		ELobbyType type = ELobbyType.k_ELobbyTypePrivate;
		switch (lobbyPrivacySelector.Selected) {
			case 0:
				// Visible only to friends and people who are invited.
				type = ELobbyType.k_ELobbyTypeFriendsOnly;
				break;
			case 1:
				// Visible to everyone.
				type = ELobbyType.k_ELobbyTypePublic;
				break;
			case 2:
				// Invisible to everyone, invite only.
				type = ELobbyType.k_ELobbyTypePrivate;
				break;
			case 3:
				// Invisible to friends but visible to the server list.
				// Not supported.
				type = ELobbyType.k_ELobbyTypeInvisible;
				break;
		}
		
		SteamLobbyManager.I.CreateLobby(type, (int)maxPlayersSelector.Value);
		ShowConnectingScreen();
	}

	public void JoinLobby() {
		ulong id = 0;
		try {
			id = ulong.Parse(lobbyIdInput.Text);
		} catch {
			GD.Print("Invalid.");
			return;
		}

		SteamLobbyManager.I.JoinLobby(id);
		ShowConnectingScreen();
	}

	// Called by SteamLobbyManager if the lobby was successfully created/connected to
	// or if the game has ended the the menu screen is being returned to. //
	public void ShowLobbyScreen() {
		SceneManager.I.ChangeScene(lobby.ResourcePath);
	}

	void ShowConnectingScreen() {
		connectingScreen.Visible = true;
	}
}
