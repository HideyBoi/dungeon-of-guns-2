// I think this library was the worst thing to ever grace this beautifully fucked world.

/*
using Godot;
using DiscordRPC;
using DiscordRPC.Logging;
using System.Linq;
using Microsoft.VisualBasic;
using Riptide;
using System.Text;

public partial class DiscordHandler : Node {

	public DiscordRpcClient client;

    public override void _EnterTree()
	{
		GD.Print("Starting Discord");

        // Start up discord
        client = new DiscordRpcClient("1045184055521579038")
        {
            //Set the logger
            Logger = new ConsoleLogger() { Level = LogLevel.Info }
        };


		client.RegisterUriScheme(executable: null, steamAppID: null);

        //Subscribe to events
        client.OnReady += (sender, e) =>
		{
			GD.Print("Received Ready from user: " + e.User.Username);
		};
			
		client.OnPresenceUpdate += (sender, e) =>
		{
			GD.Print("Received Update! " + e.Presence);
		};
		
		//Connect to the RPC
		client.Initialize();
    }

	NetworkManager.GameState setState = NetworkManager.GameState.GAME_END;
    public override void _PhysicsProcess(double delta)
    {
		if (NetworkManager.CurrentState == setState)
			return;
		
		setState = NetworkManager.CurrentState;

        switch (NetworkManager.CurrentState)
		{
			case NetworkManager.GameState.NOT_CONNECTED:
				time = System.DateTime.MinValue;
				MainMenuStatus();
				break;
			case NetworkManager.GameState.LOBBY:
				time = System.DateTime.MinValue;
				LobbyStatus();
				break;
			case NetworkManager.GameState.LOADING_GAME:
				time = System.DateTime.MinValue;
				LoadingStatus();
				break;
			case NetworkManager.GameState.IN_GAME:

				InGameStatus();
				break;
			case NetworkManager.GameState.GAME_END:
				time = System.DateTime.MinValue;
				LoadingStatus();
				break;	
		}
    }

	void MainMenuStatus() {
		client.SetPresence(new RichPresence()
		{
			Details = "In the Main Menu...",
			Assets = new Assets()
			{
				LargeImageKey = "newicon",
				LargeImageText = "Dungeon of Guns",
				SmallImageKey = "hbd-logo",
				SmallImageText = "save yourself."
			},
			Buttons = new DiscordRPC.Button[] {
				new DiscordRPC.Button() { Label = "More Info", Url = "https://hideyboi.pages.dev"}
			}
		});	
	}

	void LobbyStatus() {
		client.SetPresence(new RichPresence()
		{
			Details = "In the lobby.",
			Assets = new Assets()
			{
				LargeImageKey = "newicon",
				LargeImageText = "Dungeon of Guns",
				SmallImageKey = "hbd-logo",
				SmallImageText = "save yourself."
			},
			Party = new Party() {
				ID = SteamLobbyManager.I.LobbyId.ToString(),
				Size = 5,
				Max = 16,
				Privacy = Party.PrivacySetting.Public
			}
		});
	}

	System.DateTime time = System.DateTime.MinValue;
	void InGameStatus() {
		if (time == System.DateTime.MinValue)
			time = System.DateTime.Now;
		
		client.SetPresence(new RichPresence()
		{
			Details = "Fighting for their life.",
			Assets = new Assets()
			{
				LargeImageKey = "newicon",
				LargeImageText = "Dungeon of Guns",
				SmallImageKey = "hbd-logo",
				SmallImageText = "save yourself."
			},
			Buttons = new DiscordRPC.Button[] {
				new DiscordRPC.Button() { Label = "More Info", Url = "https://hideyboi.pages.dev"}
			},
			Timestamps = new Timestamps() {
				Start = time
			}
		});	
	}

		void LoadingStatus() {
			client.SetPresence(new RichPresence()
			{
				Details = "Loading...",
				Assets = new Assets()
				{
					LargeImageKey = "newicon",
					LargeImageText = "Dungeon of Guns",
					SmallImageKey = "hbd-logo",
					SmallImageText = "save yourself."
				}
			});	
		}

    public override void _ExitTree()
    {
		client.ClearPresence();

        client.Dispose();
    }
}
*/