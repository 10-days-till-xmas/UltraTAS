using Configgy;
using UnityEngine;

namespace _UltraTAS
{
    internal static class UltraTasConfig
    {

        internal static ConfigBuilder? cfgB;
        internal static string FileSavePath => $"{BepInEx.Paths.ConfigPath}/UltraTAS/";

        [Configgable("", "Tas NAME", 1, "IF YOU DONT PUT A NAME OLD TAS's OF THE SAME NAME WILL BE OVERWRITTEN")]
        internal static ConfigInputField<string> TasName = new("READ DESCRIPTION", null, null);

        [Configgable("Keybinds", "Start Recoring", 0, null)]
        internal static ConfigInputField<KeyCode> StartRecording = new(KeyCode.K, null, null);

        [Configgable("Keybinds", "Pause Game", 0, null)]
        internal static ConfigInputField<KeyCode> PauseGame = new(KeyCode.P, null, null);

        [Configgable("Keybinds", "Advance Frame", 0, null)]
        internal static ConfigInputField<KeyCode> AdvFrame = new(KeyCode.O, null, null);

        [Configgable("TAS Replay", "TAS Name", 1, "")]
        internal static ConfigDropdown<string>? TasReplayName;

        [Configgable("Keybinds", "Play TAS", 0, null)]
        internal static ConfigInputField<KeyCode> PlayTAS = new(KeyCode.M, null, null);

        [Configgable("TAS Replay", "TAS Name", 1, "")]
        internal static ConfigButton Refr = new(new Action(UltraTasConfig.RefreshTASList), "Refresh TAS List");

        internal static void RefreshTASList()
        {
            if (!Directory.Exists(UltraTasConfig.FileSavePath))
            {
                Directory.CreateDirectory(UltraTasConfig.FileSavePath);
            }
            UltraTAS.TASList = Directory.GetFiles(UltraTasConfig.FileSavePath, "*DolfeTAS");
            if (UltraTAS.TASList.Length != 0)
            {
                foreach (string path in UltraTAS.TASList)
                {
                    UltraTAS.TempTAS.Add(Path.GetFileName(path));
                }
                if (UltraTasConfig.cfgB != null)
                {
                    UltraTasConfig.TasReplayName?.SetOptions(UltraTAS.TASList, UltraTAS.TempTAS.ToArray(), 0, 0);
                    UltraTasConfig.cfgB.Rebuild();
                    UltraTasConfig.cfgB.BuildAll();
                    return;
                }
                else
                {
                    UltraTasConfig.TasReplayName = new ConfigDropdown<string>(UltraTAS.TASList, UltraTAS.TempTAS.ToArray(), 0);
                }
            }
            else
            {
                List<string> list = new()
                {
                    "PLEASE RECORD A TAS FIRST"
                };
                UltraTasConfig.TasReplayName = new ConfigDropdown<string>(list.ToArray(), list.ToArray(), 0);
            }
        }
    }
}