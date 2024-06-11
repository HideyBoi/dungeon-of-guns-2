using System;
using Discord;
using Godot;
using Steamworks;

public partial class DiscordHandler : Node
{

	Discord.Discord discord;
	ActivityManager activityManager;

	Discord.Discord.SetLogHookHandler discordLogger;

    public override void _EnterTree()
    {
		return;
		
        discord = new(1045184055521579038, (ulong)Discord.CreateFlags.Default);
		activityManager = discord.GetActivityManager();
		activityManager.RegisterCommand();
    }

	void logger(LogLevel level, string message) {GD.Print($"[Discord - {level}] {message}");}

	NetworkManager.GameState setState = NetworkManager.GameState.GAME_END;
    public override void _PhysicsProcess(double delta)
    {
		return;

		// I don't understand this shit and quite honestly I give up for now lol

		discord.RunCallbacks();

		if (NetworkManager.CurrentState == setState)
			return;
		
		setState = NetworkManager.CurrentState;

        switch (NetworkManager.CurrentState)
		{
			case NetworkManager.GameState.NOT_CONNECTED:
				//time = System.DateTime.MinValue;
				MainMenuStatus();
				break;
			case NetworkManager.GameState.LOBBY:
				//time = System.DateTime.MinValue;
				LobbyStatus();
				break;
			case NetworkManager.GameState.LOADING_GAME:
				//time = System.DateTime.MinValue;
				//LoadingStatus();
				break;
			case NetworkManager.GameState.IN_GAME:

				//InGameStatus();
				break;
			case NetworkManager.GameState.GAME_END:
				//time = System.DateTime.MinValue;
				//LoadingStatus();
				break;	
		}
    }

    public override void _ExitTree()
    {
        discord.Dispose();
    }

    #region statuses

    void MainMenuStatus() {
		activityManager.UpdateActivity(new Activity {
			Details = "In the Main Menu...",
			Assets =
			{
				LargeImage = "newicon",
				LargeText = "Dungeon of Guns",
				SmallImage = "hbd-logo",
				SmallText = "save yourself."
			}	
		}, result => {
			GD.Print(result);
		});
	}

	void LobbyStatus() {
		activityManager.UpdateActivity(new Activity {
			Details = "In the lobby...",
			Assets =
			{
				LargeImage = "newicon",
				LargeText = "Dungeon of Guns",
				SmallImage = "hbd-logo",
				SmallText = "save yourself."
			},
			Party = {
				Id = "totally a real id fucking accept it now why are you a bitch",
				Size = {
					CurrentSize = 0,
					MaxSize = 69
				},
				Privacy = ActivityPartyPrivacy.Public
			},
			Secrets = {
				Join = SteamLobbyManager.I.LobbyId.ToString(),
			},
			Instance = true
		}, result => {
			GD.Print(result);
		});
	}

	#endregion
}
