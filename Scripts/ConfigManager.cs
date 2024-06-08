using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using Riptide;

public class ConfigManager 
{
	static string gameSettingsVersion = "1";
	static string gameruleFileVersion = "6";

	public static Dictionary<string, string> CurrentGameSettings;
	static string gameSettingsPath = OS.GetUserDataDir() + "/GAME_SETTINGS.sav";
	public static Dictionary<string, string> CurrentGamerules;
	static string gamerulesPath = OS.GetUserDataDir() + "/GAMERULES.sav";
	
	public static void CheckVersions() {
		if (File.Exists(gamerulesPath)) {
			LoadGamerules();
			if (CurrentGamerules["version"] != gameruleFileVersion)
				UpdateGamerulesFile();
		} else {
			CreateGameruleFile();
		}

		if (File.Exists(gameSettingsPath)) {
			LoadSettings();
			if (CurrentGameSettings["version"] != gameSettingsVersion) {
				UpdateSettingsFile();
			}
		} else {
			CreateSettingsFile();
		}
	}

	public static void SaveGamerules(bool saveNew = false) {
		if (!saveNew) {
			File.WriteAllText(gamerulesPath, JsonSerializer.Serialize(CurrentGamerules));
		} else {
			File.WriteAllText(gamerulesPath, JsonSerializer.Serialize(GetDefaultGamerules()));
		}

		if (NetworkManager.I != null) {
			if (!NetworkManager.I.Server.IsRunning) {
				return;
			}

			Message msg = Message.Create(MessageSendMode.Reliable, NetworkManager.MessageIds.GameruleData);
			msg.AddString(JsonSerializer.Serialize(CurrentGamerules));
			NetworkManager.I.Client.Send(msg);
		}
	}

	public static void LoadGamerules(bool forceNew = false) {
		if (forceNew || CurrentGamerules == null) {
			CurrentGamerules = new();	
			CurrentGamerules = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(gamerulesPath));
		}
	}

	static void CreateGameruleFile() {
		SaveGamerules(true);
	}

	static Dictionary<string, string> GetDefaultGamerules() {
		return new()
		{
			// Add gamerules here. Set them up to be configured in the gamerule menu later.
			{ "version", gameruleFileVersion },
			{ "gamemode", "0" },
			{ "map_size", "3" },
			{ "lives_count", "3" },
			{ "infinite_lives", "False" },
			{ "respawn_time", "5.0"},
			{ "max_health", "100" },
			{ "med_multiplier", "1" },
			{ "legendary_chance", "35"},
			{ "rare_chance", "50"},
			{ "secondary_chance", "70"},
			{ "secondary_max_count", "2"},
			{ "heal_multiplier", "1"},
			{ "bonus_healable_chance", "4"},
			{ "ammo_multiplier", "1.65"},
			{ "ammo_crate_drop_count", "1"},
			{ "chests_regenerate", "True"},
			{ "chests_regeneration_time", "100"}
		};
	}

	static void UpdateGamerulesFile() {
		Dictionary<string, string> defaults = GetDefaultGamerules();
		CurrentGamerules["version"] = gameruleFileVersion;
		foreach (string key in defaults.Keys)
		{
			// Check if there's a missing key
			if (!CurrentGamerules.ContainsKey(key)) {
				// Add the new key
				CurrentGamerules.Add(key, defaults[key]);
			}
		}

		SaveGamerules();
	}

	[MessageHandler((ushort)NetworkManager.MessageIds.GameruleData)]
	public static void HandleGameruleData(Message msg) {
		string dataString = msg.GetString();
		CurrentGamerules = JsonSerializer.Deserialize<Dictionary<string, string>>(dataString);
	}

	public static void SaveSettings(bool saveNew = false) {
		if (!saveNew) {
			File.WriteAllText(gameSettingsPath, JsonSerializer.Serialize(CurrentGameSettings));
		} else {
			File.WriteAllText(gameSettingsPath, JsonSerializer.Serialize(GetDefaultSettings()));
		}
	}

	public static void LoadSettings(bool forceNew = false) {
		if (forceNew || CurrentGameSettings == null) {
			CurrentGameSettings = new();	
			CurrentGameSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(gameSettingsPath));
		}
	}

	static void CreateSettingsFile() {
		SaveSettings(true);
	}

	static Dictionary<string, string> GetDefaultSettings() {
		return new()
		{
			// Add settings here
			{ "version", gameSettingsVersion },
			{ "sfx_vol", "0" },
			{ "amb_vol", "4" },
			{ "fullscreen", "False" },
			{ "pickup_heals_ammo", "True"}
		};
	}

	static void UpdateSettingsFile() {
		Dictionary<string, string> defaults = GetDefaultSettings();
		CurrentGameSettings["version"] = gameSettingsVersion;
		foreach (string key in defaults.Keys)
		{
			// Check if there's a missing key
			if (!CurrentGameSettings.ContainsKey(key)) {
				// Add the new key
				CurrentGameSettings.Add(key, defaults[key]);
			}
		}

		SaveSettings();
	}
}