using Godot;

public partial class ProcGenGameManager : GameManager {

    public override void StartGame()
    {
        // Start the dungeon generator and send clients map data
        DungeonGenerator.I.Start();
    }
}