using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using Riptide;

public class ConfigManager 
{
	static string gameSettingsVersion = "VERSION_0";
	static string gamerulePresetVersion = "VERSION_0";

	static Dictionary<string, string> gameSettings;
	static string gameSettingsPath = OS.GetUserDataDir() + "/GAME_SETTINGS.sav";
	static Dictionary<string, string> currentGamerules;
	static string gamerulesPath = OS.GetUserDataDir() + "/GAMERULE_PRESET.sav";
	
	// TODO: Run at startup
	public static void CheckVersions() {
		if (File.Exists(gamerulesPath + "VERSION")) {
			if (File.ReadAllText(gamerulesPath + "VERSION") != gamerulePresetVersion)
				CreateGameruleFile();
		} else {
			CreateGameruleFile();
		}
		File.WriteAllText(gamerulesPath + "VERSION", gamerulePresetVersion);
	}

	public static void SaveGamerulePreset(Dictionary<string, string> toSave) {
		File.WriteAllText(gamerulesPath, JsonSerializer.Serialize(toSave));
		currentGamerules = toSave;

		if (!NetworkManager.I.Server.IsRunning) {
			return;
		}

		Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.GameruleData);
		msg.AddString(JsonSerializer.Serialize(toSave));
		NetworkManager.I.Client.Send(msg);
	}

	public static Dictionary<string, string> LoadGamerulePreset() {
		if (!File.Exists(gamerulesPath)) {
			CreateGameruleFile();
		}

		currentGamerules = new();
	
		currentGamerules = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(gamerulesPath));

		return currentGamerules;
	}

	public static void CreateGameruleFile() {
		currentGamerules = new()
		{
			// Add gamerules here. Set them up to be configured in the gamerule menu later.
			{ "gamemode", "0" },
			{ "map_size", "4" },
			{ "lives_count", "3" },
			{ "infinite_lives", "false" }
		};

		SaveGamerulePreset(currentGamerules);
	}

	[MessageHandler((ushort)NetworkManager.MessageIds.GameruleData)]
	public static void HandleGameruleData(Message msg) {
		string dataString = msg.GetString();
		currentGamerules = JsonSerializer.Deserialize<Dictionary<string, string>>(dataString);
	}
}