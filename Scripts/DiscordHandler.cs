using Godot;
using DiscordRPC;
using DiscordRPC.Logging;
using System.Linq;

public partial class DiscordHandler : Node {

	public DiscordRpcClient client;

    public override void _EnterTree()
	{
		GD.Print("Starting Discord");

        // Start up discord
        client = new DiscordRpcClient("1045184055521579038")
        {
            //Set the logger
            Logger = new ConsoleLogger() { Level = LogLevel.Trace }
        };

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

		MainMenuStatus();
    }

	bool unreachablecodemyass = true;
    public override void _PhysicsProcess(double delta)
    {
		if (unreachablecodemyass) {
			// Disable
			return;
		}

        switch (NetworkManager.CurrentState)
		{
			case NetworkManager.GameState.NOT_CONNECTED:
				
				break;
			case NetworkManager.GameState.LOBBY:
				
				break;
			case NetworkManager.GameState.LOADING_GAME:
				
				break;
			case NetworkManager.GameState.IN_GAME:

				break;
			case NetworkManager.GameState.GAME_END:

				break;
			
		}
    }

	void MainMenuStatus() {
		DiscordRPC.Button[] button = new DiscordRPC.Button[1];
		button.Append(new DiscordRPC.Button() {
					Label = "Button",
					Url = "https://www.google.com"
				});

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
			Buttons = button
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

	long time = 0;

	void InGameStatus() {
		if (time == 0)
			time = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
	}

    public override void _ExitTree()
    {
        client.Dispose();
    }
}