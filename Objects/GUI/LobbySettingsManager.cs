using Godot;
using System;
using System.Collections.Generic;

public partial class LobbySettingsManager : Control
{

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

	bool isLoading = false;
	void Load() {
		isLoading = true;
		gamemode.Selected = int.Parse(ConfigManager.CurrentGamerules["gamemode"]);
		mapSize.Value = int.Parse(ConfigManager.CurrentGamerules["map_size"]);
		livesCount.Value = int.Parse(ConfigManager.CurrentGamerules["lives_count"]);
		infiniteLivesToggle.ButtonPressed = bool.Parse(ConfigManager.CurrentGamerules["infinite_lives"]);
		if (bool.Parse(ConfigManager.CurrentGamerules["infinite_lives"])) {
			livesCount.Hide();
		} else {
			livesCount.Show();
		}
		isLoading = false;
	}

	// Dummy value is just to get Godot to link a signal here.
	public void UpdateValues(float dummy = 0) {UpdateValues();}
	public void UpdateValues() {
		if (isLoading)
			return;
		
		ConfigManager.CurrentGamerules["gamemode"] = gamemode.Selected.ToString();
		ConfigManager.CurrentGamerules["map_size"] = mapSize.Value.ToString();
		ConfigManager.CurrentGamerules["lives_count"] = livesCount.Value.ToString();
		ConfigManager.CurrentGamerules["infinite_lives"] = infiniteLivesToggle.ButtonPressed.ToString();
		if (bool.Parse(ConfigManager.CurrentGamerules["infinite_lives"])) {
			livesCount.Hide();
		} else {
			livesCount.Show();
		}
		ConfigManager.SaveGamerules();
	}

	public void MapSizePreset(int value) {
		// Triggers signal automatically
		mapSize.Value = value;
	}

	public void ResetSettings() {
		GD.Print("Resetting game settings");
		ConfigManager.SaveGamerules(true);

		Load();
	}
}
