using Godot;
using System;
using System.Collections.Generic;

public partial class LobbySettingsManager : Panel
{
	Dictionary<string, string> gamerules;

	[Export] OptionButton gamemode;
	[Export] SpinBox mapSize;
	[Export] SpinBox livesCount;
	[Export] CheckBox infiniteLivesToggle;

	public override void _Ready()
	{
		if (NetworkManager.I.Server.IsRunning) {
			Load();
		} else { 
			Hide();
		}
	}

	void Load() {
		gamerules = ConfigManager.LoadGamerulePreset();

		gamemode.Selected = int.Parse(gamerules["gamemode"]);
		mapSize.Value = int.Parse(gamerules["map_size"]);
		livesCount.Value = int.Parse(gamerules["lives_count"]);
		infiniteLivesToggle.ButtonPressed = bool.Parse(gamerules["infinite_lives"]);
		if (bool.Parse(gamerules["infinite_lives"])) {
			livesCount.Hide();
		} else {
			livesCount.Show();
		}
	}

	// Dummy value is just to get Godot to link a signal here.
	public void UpdateValues(float dummy = 0) {UpdateValues(); GD.Print("new" + dummy);}
	public void UpdateValues() {
		gamerules["gamemode"] = gamemode.Selected.ToString();
		gamerules["map_size"] = mapSize.Value.ToString();
		gamerules["lives_count"] = livesCount.Value.ToString();
		gamerules["infinite_lives"] = infiniteLivesToggle.ButtonPressed.ToString();
		if (bool.Parse(gamerules["infinite_lives"])) {
			livesCount.Hide();
		} else {
			livesCount.Show();
		}
		ConfigManager.SaveGamerulePreset(gamerules);
	}

	public void MapSizePreset(int value) {
		// Triggers signal automatically
		mapSize.Value = value;
	}

	public void ResetSettings() {
		GD.Print("Resetting game settings");
		ConfigManager.CreateGameruleFile();
		gamerules = ConfigManager.LoadGamerulePreset();

		Load();
	}
}
