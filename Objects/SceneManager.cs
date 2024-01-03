using Godot;
using System;

public partial class SceneManager : Node
{
    public static SceneManager I;

    Node currentScene;

    [Export] AnimationPlayer loadingScreenAnimator;
    [Export] Panel loadingScreenVis;
    string sceneToLoad = "res://Objects/GUI/MainMenuUi.tscn";

    bool checkLoaded = false;


    public override void _Ready()
    {
        I = this;
        ChangeScene(sceneToLoad, false);
    }

    public void ChangeScene(string resourcePath, bool show = true) {
        sceneToLoad = resourcePath;

        if (show)
            loadingScreenAnimator.Play("FadeIn");
        if (!show)
            loadingScreenVis.Modulate = Colors.White;
        
        ResourceLoader.LoadThreadedRequest(resourcePath);
        SceneTreeTimer timer = GetTree().CreateTimer(1f);
        timer.Timeout += LoadingAnimationFinished;
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
        loadingScreenAnimator.Play("FadeOut");
    }
}
