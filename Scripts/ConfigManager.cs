using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

public class ConfigManager 
{
    static string gameSettingsVersion = "VERSION_0";
    static string gamerulePresetVersion = "VERSION_0";

    public static Dictionary<string, string> gameSettings;
    static string gameSettingsPath = OS.GetDataDir() + "/GAME_SETTINGS";
    public static Dictionary<string, string> currentGamerules;
    static string gamerulesPath = OS.GetDataDir() + "/GAMERULE_PRESET";
    
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

    public static void SaveGamerulePreset() {
        File.WriteAllText(gamerulesPath, JsonSerializer.Serialize(currentGamerules));
    }

    public static void LoadGamerulePreset() {
        if (!File.Exists(gamerulesPath))
            CreateGameruleFile();

        currentGamerules.Clear();

        currentGamerules = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(gamerulesPath));
    }

    static void CreateGameruleFile() {
        // Add gamerules here. Set them up to be configured in the gamerule menu later.
        currentGamerules.Add("gamemode", "ffa");

        SaveGamerulePreset();
    }
}