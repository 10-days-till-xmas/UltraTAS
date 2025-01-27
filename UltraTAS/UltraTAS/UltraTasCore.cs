using BepInEx;
using Configgy;
using System.Collections;
//using System.Numerics;
using System.Text;
using UltraTAS;

//using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using WindowsInput;
using WindowsInput.Native;
using static UnityEngine.InputSystem.DefaultInputActions;
using Random = UnityEngine.Random;
using PlayerActions = UltraTAS.PlayerActions;

namespace _UltraTAS
{
    [BepInPlugin("UltraTAS", "UltraTAS", "1.0.0")]
    public class UltraTasCore : BaseUnityPlugin
    {
        protected void Awake()
        {
            Random.InitState(0);
            UltraTasConfig.RefreshTASList();
            _ = Simulator;
            UltraTasConfig.CfgBuilder = new ConfigBuilder("UltraTAS", null);
            UltraTasConfig.CfgBuilder.BuildAll();
            StartCoroutine(CustUpdate());
            Logger.LogInfo("DolfeTAS Mod Loaded");
            //new Harmony("DolfeMODS.Ultrakill.UltraTAS").PatchAll();
        }

        protected void Update()
        {
            if (!LoopRunning)
            {
                StartCoroutine(CustUpdate());
                Logger.LogInfo("Started Loop");
            }
            if (Status == null)
            {
                SetUpGameObjects();
            }
            else
            {
                try
                {
                    StringBuilder stringBuilder = new();
                    stringBuilder.Clear();
                    foreach (VirtualKeyCode virtualKeyCode in pressedSpecialKeys)
                    {
                        stringBuilder.Append(string.Format("Key: {0} \n", virtualKeyCode));
                    }
                    foreach (Key key in pressedDownKeys)
                    {
                        stringBuilder.Append(string.Format("Key: {0} \n", key));
                    }
                    string text = stringBuilder.ToString();
                    Status.text = string.Concat(
                    [
                        "\r\nTime Paused: ", TimePaused.ToString(),
                        "\r\nRecording TAS: ", CaptureInputs.ToString(),
                        "\r\nCurrent TAS Recoding to: ", UltraTasConfig.TasName.Value,
                        "\r\nPlaying TAS: ", PlayingTAS.ToString(),
                        "\r\nSelected TAS Name: ", UltraTasConfig.TasReplayName?.currentIndex.HasValue == true ? UltraTAS.TempTAS[UltraTasConfig.TasReplayName.currentIndex.Value] : "N/A",
                        "\r\nAll Active Keys (Being played by TAS): \r\n", text,
                        "\r\n                "
                    ]);
                }
                catch (Exception)
                {
                    Logger.LogError("Error in Update");
                }
            }
            if (KeysStatus.Count == 0)
            {
                foreach (object obj in Enum.GetValues(typeof(Keys)))
                {
                    Keys item = (Keys)obj;
                    (VirtualKeyCode, bool) cval = ((VirtualKeyCode)item, false);
                    KeysStatus.Add(cval);
                }
            }
            if (inverseInputsDictionary.Count == 0 && MonoSingleton<InputManager>.Instance != null)
            {
                Dictionary<string, KeyCode> inputsDictionary = MonoSingleton<InputManager>.Instance.inputsDictionary;
                if (inputsDictionary == null)
                {
                    Debug.LogError("InputsDictionary is null");
                    return;
                }
                Dictionary<KeyCode, string> dictionary = inputsDictionary.ToDictionary(
                    kvp => kvp.Value,
                    kvp => kvp.Key);
                inverseInputsDictionary = dictionary;
            }
        }

        private void SetUpGameObjects()
        {
            GameObject gameObject = new("StatusText");
            gameObject.transform.SetParent(GameObject.Find("Canvas").transform, false);

            _ = Status;

            CanvasGroup canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private IEnumerator CustUpdate()
        {
            LoopRunning = true;
            ToggleRecording(Input.GetKeyDown(UltraTasConfig.StartRecording.Value));
            PlayTAS();
            PauseGame();
            ManagePausedGame();
            AdvFrame(Input.GetKeyDown(UltraTasConfig.AdvFrame.Value));

            if (actionMappings.Count == 0 && MonoSingleton<InputManager>.Instance != null)
            {
                List<ValueTuple<string, KeyCode>> list2 = [];
                foreach (string action in PlayerActions.List)
                {
                    (string, KeyCode) invertedKeyCodeDict = (action, KeyCodeDictionaries.GetKeyCodeFromInputsDic(action));
                    list2.Add(invertedKeyCodeDict);
                }
                Dictionary<string, VirtualKeyCode> dictionary = [];
                foreach (ValueTuple<string, KeyCode> valueTuple2 in list2)
                {
                    VirtualKeyCode? virtualKeyCode = KeyCodeDictionaries.GetVirtualKeyCode(valueTuple2.Item2);
                    if (virtualKeyCode == null) continue;
                    dictionary.Add(valueTuple2.Item1, virtualKeyCode.Value);
                }
                actionMappings = dictionary;
            }
            if (CaptureInputs)
            {
                List<string> list3 = [];
                if (MonoSingleton<InputManager>.Instance != null)
                {
                    foreach (KeyValuePair<string, KeyCode> keyValuePair in MonoSingleton<InputManager>.Instance.inputsDictionary)
                    {
                        string? val = GetStringFromInputsDic(keyValuePair.Value);
                        if (keyValuePair.Key == null && Input.GetKey(keyValuePair.Value) && val != null)
                        {
                            list3.Add(val);
                            print("Pressing: Key: " + GetStringFromInputsDic(keyValuePair.Value));
                        }
                    }
                }
                if (!TimePaused || (TimePaused && Input.GetKeyDown(UltraTasConfig.AdvFrame.Value)))
                {
                    frame++;
                    print(string.Format("Frame: {0}", frame));
                    list3.Add("DOLF" + frame.ToString());
                    list3.Add("X" + MonoSingleton<CameraController>.Instance.rotationX.ToString());
                    list3.Add("Y" + MonoSingleton<CameraController>.Instance.rotationY.ToString());
                }
                SaveFrameData(list3);
            }
            yield return new WaitForEndOfFrame();
            StartCoroutine(CustUpdate());
            yield break;
        }

        private void AdvFrame(bool flag)
        {
            if (!flag) return;
            StartCoroutine(UltraTAS.AdvanceFrame());
        }

        private void PauseGame()
        {
            if (Input.GetKeyDown(UltraTasConfig.PauseGame.Value))
            {
                TimePaused = !TimePaused;
                Debug.Log($"Time Paused = {TimePaused}");
                if (TimePaused)
                {
                    prevTimeScale = Time.timeScale;
                }
            }
            
        }

        private void ManagePausedGame()
        {
            if (TimePaused)
            {
                Time.timeScale = 0f;
            }
            else if (Time.timeScale == 0f && !MonoSingleton<OptionsManager>.Instance.paused)
            {
                Time.timeScale = prevTimeScale;
            }
        }

        private void PlayTAS()
        {
            if (!Input.GetKeyDown(UltraTasConfig.PlayTAS.Value)) return;
            StartTASReplay();
        }

        private void ToggleRecording(bool flag)
        {
            if (!flag) return;

            CaptureInputs = !CaptureInputs;
            print(CaptureInputs);
            if (CaptureInputs)
            {
                MakeFileAndStuff();
                print("RECORDING TAS STARTED");
                return;
            }
            else
            {
                frame = 0;
                print("RECORDING TAS ENDED");
                List<string> list = ["END"];
                
                if (CurrentTASFile == null)
                {
                    Logger.LogError("CurrentTASFile is null");
                    return;
                }
                File.AppendAllLines(CurrentTASFile, list);
            }
        }

        private void SaveFrameData(List<string> TASLogFile)
        {
            this.WriteToFile(TASLogFile);
            TASLogFile.Clear();
        }

        private void WriteToFile(List<string> text)
        {
            if (CurrentTASFile == null)
            {
                Logger.LogError("CurrentTASFile is null");
            }
            else
            {
                File.AppendAllLines(CurrentTASFile, text);
            }
        }

        private void MakeFileAndStuff()
        {
            Directory.CreateDirectory(UltraTasConfig.FileSavePath);
            ConfigInputField<string> tasName = UltraTasConfig.TasName;
            string tasFilePath = UltraTAS.MakeNewSave($"{tasName}.DolfeTAS");
            using (File.Create(tasFilePath))
            {
            }
            CurrentTASFile = tasFilePath;
        }


        private void StartTASReplay()
        {
            if (this.PlayingTAS)
            {
                base.StartCoroutine(this.TASCheck());
                MonoBehaviour.print("Playing TAS");
                return;
            }
            this.PlayingTAS = true;
            base.StartCoroutine(this.ReplayTASDos());
        }

        private IEnumerator TASCheck()
        {
            bool keepRunning = true;
            yield return new WaitForSeconds(0.05f);
            while (keepRunning || this.PlayingTAS)
            {
                if (Input.GetKey(UltraTasConfig.PlayTAS.Value))
                {
                    this.ReplayINT = -1;
                    keepRunning = false;
                    this.PlayingTAS = false;
                    MonoBehaviour.print("Stopping TAS Replay");
                }
                yield return new WaitForEndOfFrame();
            }
            yield break;
        }


        internal IEnumerator ReplayTASDos()
        {
            MonoBehaviour.print("Playing TAS Started");
            UltraTAS.wasTSUsedThisScene = true;
            string[]? lines = null;
            if (UltraTasConfig.TasReplayName != null)
            {
                lines = File.ReadAllLines(UltraTasConfig.TasReplayName.Value);
            }
            else
            {
                throw new Exception("TAS Replay Name is null");
            }
            List<string> inputs = [];
            HashSet<KeyCode> pressedDownMouse = [];
            bool ShootPressed = false;
            bool preventRailcannonSpam = true;

            while (this.ReplayINT != -1)
            {
                this.ReplayINT++;
                inputs.Clear();
                string ThisFrame = "DOLF" + ReplayINT.ToString();
                string NextFrame = "DOLF" + (this.ReplayINT + 1).ToString();
                int num = Array.FindIndex(lines, (string line) => line.Contains(ThisFrame));
                int num2 = Array.FindIndex(lines, num, (string line) => line.Contains(NextFrame));
                if (num < 0 || num2 < 0)
                {
                    this.ReplayINT = -1;
                    break;
                }
                for (int i = num + 1; i < num2; i++)
                {
                    inputs.Add(lines[i].Trim());
                }
                if (inputs.Count > 2)
                {
                    MonoBehaviour.print("Frame: " + ReplayINT.ToString());
                }
                pressedDownKeys.Clear();
                inputActionStates.Clear();
                foreach (string text in inputs)
                {
                    if (text.StartsWith("X"))
                    {
                        if (float.TryParse(text.AsSpan(1), out float rotationX))
                        {
                            MonoSingleton<CameraController>.Instance.rotationX = rotationX;
                        }
                    }
                    else if (text.StartsWith("Y"))
                    {
                        if (float.TryParse(text.AsSpan(1), out float rotationY))
                        {
                            MonoSingleton<CameraController>.Instance.rotationY = rotationY;
                        }
                    }
                    else if (PlayerActions.List.Contains(text) && (text != "Fire1" || text != "Fire2"))
                    {
                        if (actionMappings.TryGetValue(text, out VirtualKeyCode virtualKeyCode))
                        {
                            inputActionStates.Add(virtualKeyCode);
                            pressedSpecialKeys.Add(virtualKeyCode);
                            this.SimulateSpecial(virtualKeyCode, true);
                        }
                        else
                        {
                            MonoBehaviour.print("Action has no value! " + text);
                        }
                    }
                    else if (text == "Fire1" && MonoSingleton<GunControl>.Instance.currentSlot != 4)
                    {
                        MonoSingleton<InputManager>.Instance.InputSource.Fire1.IsPressed = true;
                        MonoSingleton<InputManager>.Instance.InputSource.Fire1.PerformedFrame = Time.frameCount + 1;
                        ShootPressed = true;
                        print("Fire 1 is pressed");
                    }
                    else if (MonoSingleton<GunControl>.Instance.currentSlot == 4 && text == "Fire1")
                    {
                        if (!preventRailcannonSpam)
                        {
                            preventRailcannonSpam = true;
                            MonoSingleton<InputManager>.Instance.InputSource.Fire1.PerformedFrame = Time.frameCount + 1;
                        }
                    }
                    else
                    {
                        KeyCode? keyCodeFromInputsDic = KeyCodeDictionaries.GetKeyCodeFromInputsDic(text);
                        KeyCode valueOrDefault = keyCodeFromInputsDic.GetValueOrDefault();
                        Key? keyFromKeyCode = KeyCodeDictionaries.GetKeyFromKeyCode(valueOrDefault);
                        if (keyFromKeyCode != null && !KeyCodeDictionaries.IsMouseInput(valueOrDefault))
                        {
                            pressedDownKeys.Add(keyFromKeyCode.Value);
                            KeyCodeDictionaries.SimulateKeybord(keyFromKeyCode.Value, true);
                        }
                        else if (KeyCodeDictionaries.IsMouseInput(valueOrDefault))
                        {
                            pressedDownMouse.Add(valueOrDefault);
                            if (valueOrDefault == KeyCode.Mouse0)
                            {
                                MouseState mouseState = default;
                                InputDevice device = InputSystem.GetDevice<Mouse>();
                                mouseState.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, true);
                                InputSystem.QueueStateEvent(device, mouseState, -1.0);
                            }
                            else if (valueOrDefault == KeyCode.Mouse1)
                            {
                                KeyCodeDictionaries.SimulateMouseButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right, true);
                            }
                        }
                        else
                        {
                            MonoBehaviour.print(string.Format("Key: {0} is null or {1} is wrong or {2} is wrong", keyCodeFromInputsDic, valueOrDefault, keyFromKeyCode));
                        }
                    }
                }
                ReleaseUnpressedKeysMouseButtonsAndSpecialActions(inputs, pressedDownMouse, pressedSpecialKeys, ref ShootPressed, ref preventRailcannonSpam);
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSeconds(0.05f);
            for (int j = 0; j < KeysStatus.Count; j++)
            {
                KeysStatus[j] = new ValueTuple<VirtualKeyCode, bool>(KeysStatus[j].Item1, false);
                SimulateSpecial(KeysStatus[j].Item1, false);
            }
            foreach (Key key in this.pressedDownKeys)
            {
                KeyCodeDictionaries.SimulateKeybord(key, false);
            }
            pressedDownKeys.Clear();
            foreach (KeyCode keyCode in pressedDownMouse)
            {
                bool left = keyCode == KeyCode.Mouse0;
                KeyCodeDictionaries.SimulateMouseButton(left, false);
            }
            pressedDownMouse.Clear();
            ReplayINT = 0;
            PlayingTAS = false;
            MonoBehaviour.print("TAS REPLAY FINISHED");
            yield break;
        }

        private void ReleaseUnpressedKeysMouseButtonsAndSpecialActions(List<string> inputs, HashSet<KeyCode> pressedDownMouse, HashSet<VirtualKeyCode> pressedSpecialKeys, ref bool ShootPressed, ref bool preventRailcannonSpam)
        {
            HashSet<Key> hashSet = [];
            HashSet<VirtualKeyCode> hashSet2 = [];
            foreach (string text in inputs)
            {
                KeyCode valueOrDefault = KeyCodeDictionaries.GetKeyCodeFromInputsDic(text);
                Key? keyFromKeyCode = KeyCodeDictionaries.GetKeyFromKeyCode(valueOrDefault);
                if (keyFromKeyCode != null && !KeyCodeDictionaries.IsMouseInput(valueOrDefault))
                {
                    hashSet.Add(keyFromKeyCode.Value);
                }
                if (actionMappings.TryGetValue(text.Trim(), out VirtualKeyCode item))
                {
                    hashSet2.Add(item);
                }
            }
            if (preventRailcannonSpam && !inputs.Contains("Fire1"))
            {
                preventRailcannonSpam = false;
            }
            if (ShootPressed && !inputs.Contains("Fire1"))
            {
                MonoSingleton<InputManager>.Instance.InputSource.Fire1.IsPressed = false;
                ShootPressed = false;
                print("Unpressed Fire1");
            }
            foreach (Key key in this.pressedDownKeys)
            {
                if (!hashSet.Contains(key))
                {
                    print(string.Format("Releasing key: {0}", key));
                    KeyCodeDictionaries.SimulateKeybord(key, false);
                }
            }
            foreach (object obj in Enum.GetValues(typeof(KeyCode)))
            {
                KeyCode keyCode = (KeyCode)obj;
                if (KeyCodeDictionaries.IsMouseInput(keyCode) && pressedDownMouse.Contains(keyCode) && !inputs.Contains(keyCode.ToString()))
                {
                    bool left = keyCode == KeyCode.Mouse0;
                    KeyCodeDictionaries.SimulateMouseButton(left, false);
                }
            }
            List<VirtualKeyCode> list = [];
            foreach (VirtualKeyCode virtualKeyCode in pressedSpecialKeys)
            {
                if (!hashSet2.Contains(virtualKeyCode))
                {
                    MonoBehaviour.print(string.Format("Releasing special action: {0}", virtualKeyCode));
                    SimulateSpecial(virtualKeyCode, false);
                    list.Add(virtualKeyCode);
                }
            }
            foreach (VirtualKeyCode item2 in list)
            {
                pressedSpecialKeys.Remove(item2);
            }
        }

        internal void SimulateSpecial(VirtualKeyCode input, bool press)
        {
            int num = this.KeysStatus.FindIndex((ValueTuple<VirtualKeyCode, bool> k) => k.Item1 == input);
            if (!press)
            {
                this.Simulator.Keyboard.KeyUp(input);
                return;
            }
            this.Simulator.Keyboard.KeyDown(input);
            if (num >= 0)
            {
                this.KeysStatus[num] = new ValueTuple<VirtualKeyCode, bool>(input, true);
                return;
            }
            this.KeysStatus.Add(new ValueTuple<VirtualKeyCode, bool>(input, true));
            if (num >= 0)
            {
                this.KeysStatus[num] = new ValueTuple<VirtualKeyCode, bool>(input, false);
                return;
            }
            this.KeysStatus.Add(new ValueTuple<VirtualKeyCode, bool>(input, false));
        }

        public string GetStringFromInputsDic(KeyCode input)
        {
            return inverseInputsDictionary[input];
        }

        internal void TranslateMapping(string input, out VirtualKeyCode action)
        {
            if (!actionMappings.TryGetValue(input, out action))
            {
                throw new Exception("Action not found in mapping");
            }
        }

        internal bool CaptureInputs;
        internal string? CurrentTASFile
        {
            get;
            set;
        }
        private InputSimulator? _simulator;
        internal InputSimulator Simulator
        { 
            get
            {
                _simulator ??= new InputSimulator();
                return _simulator;
            }
        }

        internal int frame;



        readonly static PlayerActionsEnum[] actionsArray = (PlayerActionsEnum[])Enum.GetValues(typeof(PlayerActionsEnum));
        internal static string[] ActionNames => actionsArray.Select(action => action.ToString()).ToArray();

        private UnityEngine.UI.Text _status;
        internal UnityEngine.UI.Text Status 
        {
            get
            {
                if (_status == null)
                {
                    InitialiseStatus();
                }
                return _status;
            }
            set { _status = value} 
        }

        private void InitialiseStatus()
        {
            _status = gameObject.AddComponent<UnityEngine.UI.Text>();
            _status.font = Resources.GetBuiltinResource<UnityEngine.Font>("Arial.ttf");
            _status.color = UnityEngine.Color.white;
            _status.fontSize = 20;
            _status.alignment = 0;
            RectTransform component = _status.GetComponent<RectTransform>();
            component.anchorMin = new Vector2(0f, 1f);
            component.anchorMax = new Vector2(0f, 1f);
            component.pivot = new Vector2(0f, 1f);
            component.anchoredPosition = new Vector2(0f, -1f);
            component.sizeDelta = new Vector2(2200f, 10000f);
            component.anchoredPosition += new Vector2(0f, 0f);
        }

        internal bool LoopRunning;

        internal bool TimePaused;

        internal bool TimePausedUndone = true;

        internal float prevTimeScale = 1f;

        internal int ReplayINT;

        internal bool PlayingTAS;

        internal List<Key> pressedDownKeys = [];

        internal List<VirtualKeyCode> inputActionStates = [];


        internal HashSet<VirtualKeyCode> pressedSpecialKeys = [];

        internal List<ValueTuple<VirtualKeyCode, bool>> KeysStatus = [];

        internal Dictionary<KeyCode, string> inverseInputsDictionary = [];

        internal Dictionary<string, VirtualKeyCode> actionMappings = [];
    }
}
