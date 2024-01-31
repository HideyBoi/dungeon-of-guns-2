using Godot;
using Riptide;
using System;
using System.Collections.Generic;

public partial class SceneManager : Node
{
	public static SceneManager I;

	Node currentScene;

	[Export] AnimationPlayer loadingScreenAnimator;
	[Export] Panel loadingScreenVis;
	string sceneToLoad = "res://Objects/GUI/MainMenuUi.tscn";

	bool checkLoaded = false;
	bool loadingRemotely = false;

	static Dictionary<ushort, bool> finishedPlayers = new();

	public override void _Ready()
	{
		I = this;
		ChangeScene(sceneToLoad, false);
		NetworkManager.I.Client.ClientDisconnected += ClientPlayerLeft;
	}

	[MessageHandler((ushort)NetworkManager.MessageIds.StartSceneLoad)]
	public static void StartRemoteLoad(Message msg) {
		I.loadingRemotely = true;
		I.ChangeScene(msg.GetString());
	}

	public void ChangeScene(string resourcePath, bool show = true, bool sync = false) {
		finishedPlayers.Clear();
		
		sceneToLoad = resourcePath;

		if (show)
			loadingScreenAnimator.Play("FadeIn");
		if (!show)
			loadingScreenVis.Modulate = Colors.White;
		
		ResourceLoader.LoadThreadedRequest(resourcePath);
		SceneTreeTimer timer = GetTree().CreateTimer(1f);
		timer.Timeout += LoadingAnimationFinished;

		if (!sync)
			return;
		
		finishedPlayers.Clear();

		Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.StartSceneLoad);
		msg.AddString(resourcePath);
		NetworkManager.I.Client.Send(msg);

		loadingRemotely = true;
	}

	void LoadingAnimationFinished() {
		if (currentScene != null)
			currentScene.QueueFree();

		if (ResourceLoader.LoadThreadedGetStatus(sceneToLoad) != ResourceLoader.ThreadLoadStatus.Loaded) {
			checkLoaded = true;
		} else {
			SetNewScene((PackedScene)ResourceLoader.LoadThreadedGet(sceneToLoad));
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (checkLoaded) {
			if (ResourceLoader.LoadThreadedGetStatus(sceneToLoad) == ResourceLoader.ThreadLoadStatus.Loaded) {
				checkLoaded = false;
				SetNewScene((PackedScene)ResourceLoader.LoadThreadedGet(sceneToLoad));
			}
		}
	}

	void SetNewScene(PackedScene scene) {
		currentScene = scene.Instantiate();
		AddChild(currentScene);
		if (!loadingRemotely) {
			loadingScreenAnimator.Play("FadeOut");
		} else {
			Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.DoneLoading);
			msg.AddUShort(NetworkManager.I.Client.Id);

			if (!NetworkManager.I.Server.IsRunning)
				NetworkManager.I.Client.Send(msg);
			else
				HandleDoneLoading(msg);

			loadingRemotely = false;
		}
			
	}

	[MessageHandler((ushort)NetworkManager.MessageIds.DoneLoading)]
	public static void HandleDoneLoading(Message msg) {
		ushort id = msg.GetUShort();

		finishedPlayers.Add(id, true);

		if (NetworkManager.I.Server.IsRunning) {
			if (finishedPlayers.Count == NetworkManager.ConnectedPlayers.Count) {
				// World gen
				// Player spawning can be handled by something in the game world because that's easy
				// Player starting positions can be obtained after the world is done being generated because that's easy
				
				Message msg2 = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.CompleteLoading);
				NetworkManager.I.Client.Send(msg2);

				I.loadingScreenAnimator.Play("FadeOut");
			}
		}
	}

	[MessageHandler((ushort)NetworkManager.MessageIds.CompleteLoading)]
	public static void CompleteLoadingHandler(Message msg) {
		I.loadingScreenAnimator.Play("FadeOut");
		// Start game somehow, probably grab a spawn point now since it's easy.
	}

	private void ClientPlayerLeft(object sender, ClientDisconnectedEventArgs e)
	{	   
		try {
			finishedPlayers.Remove(e.Id);
		} catch {
			GD.Print("Player left, but was not finished, so nothing to remove.");
		}
	}

    public override void _ExitTree()
    {
        NetworkManager.I.Client.ClientDisconnected -= ClientPlayerLeft;
    }
}
