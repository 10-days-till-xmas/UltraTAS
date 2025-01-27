using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
//using System.Numerics;
using System.Resources;
using System.Text;
using System.Windows.Forms;
using BepInEx;
using Configgy;
//using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using WindowsInput;
using WindowsInput.Native;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using Random = UnityEngine.Random;

namespace _UltraTAS
{
    [BepInPlugin("UltraTAS", "UltraTAS", "1.0.0")]
    public class Class1 : BaseUnityPlugin
    {
        protected void Awake()
        {
            Random.InitState(0);
            UltraTasConfig.RefreshTASList();
            this.Simulator = new InputSimulator();
            UltraTasConfig.cfgB = new ConfigBuilder("UltraTAS", null);
            UltraTasConfig.cfgB.BuildAll();
            base.StartCoroutine(this.CustUpdate());
            base.Logger.LogInfo("DolfeTAS Mod Loaded");
            //new Harmony("DolfeMODS.Ultrakill.UltraTAS").PatchAll();
        }

        protected void Update()
        {
            if (!this.LoopRunning)
            {
                base.StartCoroutine(this.CustUpdate());
                base.Logger.LogInfo("Started Loop");
            }
            if (this.status == null)
            {
                GameObject gameObject = new("StatusText");
                gameObject.transform.SetParent(GameObject.Find("Canvas").transform, false);
                this.status = gameObject.AddComponent<UnityEngine.UI.Text>();
                this.status.font = Resources.GetBuiltinResource<UnityEngine.Font>("Arial.ttf");
                this.status.color = UnityEngine.Color.white;
                this.status.fontSize = 20;
                this.status.alignment = 0;
                RectTransform component = this.status.GetComponent<RectTransform>();
                component.anchorMin = new Vector2(0f, 1f);
                component.anchorMax = new Vector2(0f, 1f);
                component.pivot = new Vector2(0f, 1f);
                component.anchoredPosition = new Vector2(0f, -1f);
                component.sizeDelta = new Vector2(2200f, 10000f);
                component.anchoredPosition += new Vector2(0f, 0f);
                CanvasGroup canvasGroup = gameObject.AddComponent<CanvasGroup>();
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (this.status != null)
            {
                try
                {
                    StringBuilder stringBuilder = new();
                    stringBuilder.Clear();
                    foreach (VirtualKeyCode virtualKeyCode in this.pressedSpecialKeys)
                    {
                        stringBuilder.Append(string.Format("Key: {0} \n", virtualKeyCode));
                    }
                    foreach (Key key in this.pressedDownKeys)
                    {
                        stringBuilder.Append(string.Format("Key: {0} \n", key));
                    }
                    string text = stringBuilder.ToString();
                    this.status.text = string.Concat(new string[]
                    {
                        "\r\nTime Paused: ", this.TimePaused.ToString(),
                        "\r\nRecording TAS: ", this.CaptureInputs.ToString(),
                        "\r\nCurrent TAS Recoding to: ", UltraTasConfig.TasName.Value,
                        "\r\nPlaying TAS: ", this.PlayingTAS.ToString(),
                        "\r\nSelected TAS Name: ", UltraTasConfig.TasReplayName?.currentIndex.HasValue == true ? UltraTAS.TempTAS[UltraTasConfig.TasReplayName.currentIndex.Value] : "N/A",
                        "\r\nAll Active Keys (Being played by TAS): \r\n", text,
                        "\r\n                "
                    });
                }
                catch (Exception)
                {
                }
            }
            if (this.KeysStatus.Count == 0)
            {
                foreach (object obj in Enum.GetValues(typeof(Keys)))
                {
                    Keys item = (Keys)obj;
                    (VirtualKeyCode, bool) cval = ((VirtualKeyCode)item, false);
                    this.KeysStatus.Add(cval);
                }
            }
            if (this.dic2.Count == 0 && MonoSingleton<InputManager>.Instance != null)
            {
                Dictionary<string, KeyCode> inputsDictionary = MonoSingleton<InputManager>.Instance.inputsDictionary;
                if (inputsDictionary != null)
                {
                    Dictionary<KeyCode, string> dictionary = inputsDictionary.ToDictionary((KeyValuePair<string, KeyCode> x) => x.Value, (KeyValuePair<string, KeyCode> x) => x.Key);
                    this.dic2 = new Dictionary<KeyCode, string>(dictionary);
                    return;
                }
                UnityEngine.Debug.LogError("inputsDictionary is null");
            }
        }

        private IEnumerator CustUpdate()
        {
            this.LoopRunning = true;
            if (Input.GetKeyDown(UltraTasConfig.StartRecording.Value))
            {
                this.CaptureInputs = !this.CaptureInputs;
                MonoBehaviour.print(this.CaptureInputs.ToString());
                if (CaptureInputs)
                {
                    this.MakeFileAndStuff();
                    MonoBehaviour.print("RECORDING TAS STARTED");
                }
                else
                {
                    this.frame = 0;
                    MonoBehaviour.print("RECORDING TAS ENDED");
                    List<string> list = new()
                    {
                        "END"
                    };
                    File.AppendAllLines(this.CurrentTASFile, list);
                }
            }
            if (Input.GetKeyDown(UltraTasConfig.PlayTAS.Value))
            {
                this.StartTASReplay();
            }
            if (Input.GetKeyDown(UltraTasConfig.PauseGame.Value))
            {
                this.TimePaused = !this.TimePaused;
                UnityEngine.Debug.Log("Time Paused = " + this.TimePaused.ToString());
                if (this.TimePaused)
                {
                    this.prevTimeScale = Time.timeScale;
                }
            }
            if (this.TimePaused)
            {
                Time.timeScale = 0f;
            }
            if (Time.timeScale == 0f && !this.TimePaused && !MonoSingleton<OptionsManager>.Instance.paused)
            {
                Time.timeScale = this.prevTimeScale;
            }
            if (Input.GetKeyDown(UltraTasConfig.AdvFrame.Value))
            {
                base.StartCoroutine(this.AdvanceFrame());
            }
            if (this.actionMappings.Count == 0 && MonoSingleton<InputManager>.Instance != null)
            {
                List<ValueTuple<string, KeyCode>> list2 = new List<ValueTuple<string, KeyCode>>();
                foreach (string text in this.PlayerActions)
                {
                    ValueTuple<string, KeyCode?> valueTuple = new ValueTuple<string, KeyCode?>(text, UltraTAS.GetKeyCodeFromInputsDic(text));
                    if (valueTuple.Item2 != null)
                    {
                        list2.Add(new ValueTuple<string, KeyCode>(valueTuple.Item1, valueTuple.Item2.Value));
                    }
                }
                Dictionary<string, VirtualKeyCode> dictionary = new Dictionary<string, VirtualKeyCode>();
                foreach (ValueTuple<string, KeyCode> valueTuple2 in list2)
                {
                    VirtualKeyCode? virtualKeyCode = UltraTAS.GetVirtualKeyCode(valueTuple2.Item2);
                    if (virtualKeyCode != null)
                    {
                        dictionary.Add(valueTuple2.Item1, virtualKeyCode.Value);
                    }
                }
                this.actionMappings = dictionary;
            }
            if (this.CaptureInputs)
            {
                List<string> list3 = new List<string>();
                if (MonoSingleton<InputManager>.Instance != null)
                {
                    foreach (KeyValuePair<string, KeyCode> keyValuePair in MonoSingleton<InputManager>.Instance.inputsDictionary)
                    {
                        string? val = GetStringFromInputsDic(keyValuePair.Value);
                        if (keyValuePair.Key == null && Input.GetKey(keyValuePair.Value) && val != null)
                        {
                            list3.Add(val);
                            MonoBehaviour.print("Pressing: Key: " + GetStringFromInputsDic(keyValuePair.Value));
                        }
                    }
                }
                if (!this.TimePaused || (this.TimePaused && Input.GetKeyDown(UltraTasConfig.AdvFrame.Value)))
                {
                    this.frame++;
                    MonoBehaviour.print(string.Format("Frame: {0}", this.frame));
                    list3.Add("DOLF" + this.frame.ToString());
                    list3.Add("X" + MonoSingleton<CameraController>.Instance.rotationX.ToString());
                    list3.Add("Y" + MonoSingleton<CameraController>.Instance.rotationY.ToString());
                }
                this.SaveFrameData(list3);
            }
            yield return new WaitForEndOfFrame();
            base.StartCoroutine(this.CustUpdate());
            yield break;
        }

        private void SaveFrameData(List<string> TASLogFile)
        {
            this.WriteToFile(TASLogFile);
            TASLogFile.Clear();
        }

        private void WriteToFile(List<string> text)
        {
            File.AppendAllLines(this.CurrentTASFile, text);
        }

        private string makeNewSave(string TASName)
        {
            return UltraTasConfig.FileSavePath + TASName;
        }


        private void MakeFileAndStuff()
        {
            Directory.CreateDirectory(UltraTasConfig.FileSavePath);
            ConfigInputField<string> tasName = UltraTasConfig.TasName;
            string text = this.makeNewSave(((tasName != null) ? tasName.ToString() : null) + ".DolfeTAS");
            using (File.Create(text))
            {
            }
            this.CurrentTASFile = text;
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


        private IEnumerator AdvanceFrame()
        {
            Time.timeScale = 1f;
            yield return new WaitForEndOfFrame();
            Time.timeScale = 0f;
            yield break;
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
            List<string> inputs = new();
            HashSet<KeyCode> pressedDownMouse = new();
            bool ShootPressed = false;
            bool preventRailcannonSpam = true;

            while (this.ReplayINT != -1)
            {
                this.ReplayINT++;
                inputs.Clear();
                string ThisFrame = "DOLF" + this.ReplayINT.ToString();
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
                    MonoBehaviour.print("Frame: " + this.ReplayINT.ToString());
                }
                this.pressedDownKeys.Clear();
                this.inputActionStates.Clear();
                foreach (string text in inputs)
                {
                    if (text.StartsWith("X"))
                    {
                        float rotationX;
                        if (float.TryParse(text.Substring(1), out rotationX))
                        {
                            MonoSingleton<CameraController>.Instance.rotationX = rotationX;
                        }
                    }
                    else if (text.StartsWith("Y"))
                    {
                        float rotationY;
                        if (float.TryParse(text.Substring(1), out rotationY))
                        {
                            MonoSingleton<CameraController>.Instance.rotationY = rotationY;
                        }
                    }
                    else if (this.PlayerActions.Contains(text) && (text != "Fire1" || text != "Fire2"))
                    {
                        VirtualKeyCode virtualKeyCode;
                        if (this.actionMappings.TryGetValue(text, out virtualKeyCode))
                        {
                            this.inputActionStates.Add(virtualKeyCode);
                            this.pressedSpecialKeys.Add(virtualKeyCode);
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
                        MonoBehaviour.print("Fire 1 is pressed");
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
                        KeyCode? keyCodeFromInputsDic = UltraTAS.GetKeyCodeFromInputsDic(text);
                        KeyCode valueOrDefault = keyCodeFromInputsDic.GetValueOrDefault();
                        Key? keyFromKeyCode = this.GetKeyFromKeyCode(valueOrDefault);
                        if (keyFromKeyCode != null && !UltraTAS.IsMouseInput(valueOrDefault))
                        {
                            this.pressedDownKeys.Add(keyFromKeyCode.Value);
                            this.SimulateKeybord(keyFromKeyCode.Value, true);
                        }
                        else if (UltraTAS.IsMouseInput(valueOrDefault))
                        {
                            pressedDownMouse.Add(valueOrDefault);
                            if (valueOrDefault == KeyCode.Mouse0)
                            {
                                MouseState mouseState = default(MouseState);
                                InputDevice device = InputSystem.GetDevice<Mouse>();
                                mouseState.WithButton(0, true);
                                InputSystem.QueueStateEvent<MouseState>(device, mouseState, -1.0);
                            }
                            else if (valueOrDefault == KeyCode.Mouse1)
                            {
                                this.SimulateMouseButton(false, true);
                            }
                        }
                        else
                        {
                            MonoBehaviour.print(string.Format("Key: {0} is null or {1} is wrong or {2} is wrong", keyCodeFromInputsDic, valueOrDefault, keyFromKeyCode));
                        }
                    }
                }
                this.ReleaseUnpressedKeysMouseButtonsAndSpecialActions(inputs, pressedDownMouse, this.pressedSpecialKeys, ref ShootPressed, ref preventRailcannonSpam);
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSeconds(0.05f);
            for (int j = 0; j < this.KeysStatus.Count; j++)
            {
                this.KeysStatus[j] = new ValueTuple<VirtualKeyCode, bool>(this.KeysStatus[j].Item1, false);
                this.SimulateSpecial(this.KeysStatus[j].Item1, false);
            }
            foreach (Key key in this.pressedDownKeys)
            {
                this.SimulateKeybord(key, false);
            }
            this.pressedDownKeys.Clear();
            foreach (KeyCode keyCode in pressedDownMouse)
            {
                bool left = keyCode == KeyCode.Mouse0;
                this.SimulateMouseButton(left, false);
            }
            pressedDownMouse.Clear();
            this.ReplayINT = 0;
            this.PlayingTAS = false;
            MonoBehaviour.print("TAS REPLAY FINISHED");
            yield break;
        }

        private void ReleaseUnpressedKeysMouseButtonsAndSpecialActions(List<string> inputs, HashSet<KeyCode> pressedDownMouse, HashSet<VirtualKeyCode> pressedSpecialKeys, ref bool ShootPressed, ref bool preventRailcannonSpam)
        {
            HashSet<Key> hashSet = new();
            HashSet<VirtualKeyCode> hashSet2 = new();
            foreach (string text in inputs)
            {
                KeyCode valueOrDefault = UltraTAS.GetKeyCodeFromInputsDic(text);
                Key? keyFromKeyCode = this.GetKeyFromKeyCode(valueOrDefault);
                if (keyFromKeyCode != null && !UltraTAS.IsMouseInput(valueOrDefault))
                {
                    hashSet.Add(keyFromKeyCode.Value);
                }
                VirtualKeyCode item;
                if (this.actionMappings.TryGetValue(text.Trim(), out item))
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
                MonoBehaviour.print("Unpressed Fire1");
            }
            foreach (Key key in this.pressedDownKeys)
            {
                if (!hashSet.Contains(key))
                {
                    MonoBehaviour.print(string.Format("Releasing key: {0}", key));
                    this.SimulateKeybord(key, false);
                }
            }
            foreach (object obj in Enum.GetValues(typeof(KeyCode)))
            {
                KeyCode keyCode = (KeyCode)obj;
                if (UltraTAS.IsMouseInput(keyCode) && pressedDownMouse.Contains(keyCode) && !inputs.Contains(keyCode.ToString()))
                {
                    bool left = keyCode == KeyCode.Mouse0;
                    this.SimulateMouseButton(left, false);
                }
            }
            List<VirtualKeyCode> list = new List<VirtualKeyCode>();
            foreach (VirtualKeyCode virtualKeyCode in pressedSpecialKeys)
            {
                if (!hashSet2.Contains(virtualKeyCode))
                {
                    MonoBehaviour.print(string.Format("Releasing special action: {0}", virtualKeyCode));
                    this.SimulateSpecial(virtualKeyCode, false);
                    list.Add(virtualKeyCode);
                }
            }
            foreach (VirtualKeyCode item2 in list)
            {
                pressedSpecialKeys.Remove(item2);
            }
        }

        internal void SimulateKeybord(Key[] keys, bool press)
        {
            KeyboardState keyboardState = default(KeyboardState);
            Keyboard device = InputSystem.GetDevice<Keyboard>();
            int i = 0;
            while (i < keys.Length)
            {
                Key key = keys[i];
                Key key2 = key;
                Key? keyFromKeyCode = this.GetKeyFromKeyCode(UltraTasConfig.PauseGame.Value);
                if (key2 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null)
                {
                    goto IL_B8;
                }
                Key key3 = key;
                keyFromKeyCode = this.GetKeyFromKeyCode(UltraTasConfig.AdvFrame.Value);
                if (key3 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null)
                {
                    goto IL_B8;
                }
                Key key4 = key;
                keyFromKeyCode = this.GetKeyFromKeyCode(UltraTasConfig.PlayTAS.Value);
                if (key4 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null)
                {
                    goto IL_B8;
                }
                Key key5 = key;
                keyFromKeyCode = this.GetKeyFromKeyCode(UltraTasConfig.StartRecording.Value);
                if (key5 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null)
                {
                    goto IL_B8;
                }
                if (device != null)
                {
                    keyboardState.Set(key, press);
                }
            IL_D1:
                i++;
                continue;
            IL_B8:
                MonoBehaviour.print("Key Is assigned key so skipping");
                goto IL_D1;
            }
            InputSystem.QueueStateEvent<KeyboardState>(device, keyboardState, -1.0);
            InputSystem.Update();
        }

        internal void SimulateKeybord(List<Key> keys, bool press)
        {
            KeyboardState keyboardState = default(KeyboardState);
            Keyboard device = InputSystem.GetDevice<Keyboard>();
            foreach (Key key in keys)
            {
                Key key2 = key;
                Key? keyFromKeyCode = this.GetKeyFromKeyCode(UltraTasConfig.PauseGame.Value);
                if (!(key2 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
                {
                    Key key3 = key;
                    keyFromKeyCode = this.GetKeyFromKeyCode(UltraTasConfig.AdvFrame.Value);
                    if (!(key3 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
                    {
                        Key key4 = key;
                        keyFromKeyCode = this.GetKeyFromKeyCode(UltraTasConfig.PlayTAS.Value);
                        if (!(key4 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
                        {
                            Key key5 = key;
                            keyFromKeyCode = this.GetKeyFromKeyCode(UltraTasConfig.StartRecording.Value);
                            if (!(key5 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
                            {
                                if (device != null)
                                {
                                    keyboardState.Set(key, press);
                                    continue;
                                }
                                continue;
                            }
                        }
                    }
                }
                MonoBehaviour.print("Key Is assigned key so skipping");
            }
            InputSystem.QueueStateEvent<KeyboardState>(device, keyboardState, -1.0);
            InputSystem.Update();
        }

        internal void SimulateKeybord(Key key, bool press)
        {
            Key? keyFromKeyCode = this.GetKeyFromKeyCode(UltraTasConfig.PauseGame.Value);
            if (!(key == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
            {
                keyFromKeyCode = this.GetKeyFromKeyCode(UltraTasConfig.AdvFrame.Value);
                if (!(key == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
                {
                    keyFromKeyCode = this.GetKeyFromKeyCode(UltraTasConfig.PlayTAS.Value);
                    if (!(key == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
                    {
                        keyFromKeyCode = this.GetKeyFromKeyCode(UltraTasConfig.StartRecording.Value);
                        if (!(key == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
                        {
                            goto IL_9E;
                        }
                    }
                }
            }
            MonoBehaviour.print("Key Is assigned key so skipping");
        IL_9E:
            Keyboard device = InputSystem.GetDevice<Keyboard>();
            if (device != null)
            {
                KeyboardState keyboardState = default(KeyboardState);
                if (press)
                {
                    keyboardState = new KeyboardState(new Key[]
                    {
                        key
                    });
                }
                else
                {
                    keyboardState = default(KeyboardState);
                }
                InputSystem.QueueStateEvent<KeyboardState>(device, keyboardState, -1.0);
                InputSystem.Update();
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

        internal void SimulateMouseButton(bool left, bool press)
        {
            Mouse device = InputSystem.GetDevice<Mouse>();
            MouseState mouseState = default(MouseState);
            if (device != null)
            {
                if (left)
                {
                    if (press)
                    {
                        mouseState.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, true);
                        InputSystem.QueueStateEvent<MouseState>(device, mouseState, -1.0);
                    }
                    else
                    {
                        mouseState.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, false);
                        InputSystem.QueueStateEvent<MouseState>(device, mouseState, -1.0);
                    }
                }
                else if (!left)
                {
                    if (press)
                    {
                        mouseState.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right, true);
                        InputSystem.QueueStateEvent<MouseState>(device, mouseState, -1.0);
                    }
                    else
                    {
                        mouseState.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right, false);
                        InputSystem.QueueStateEvent<MouseState>(device, mouseState, -1.0);
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("Mouse not found.");
            }
            InputSystem.QueueStateEvent<MouseState>(device, mouseState, -1.0);
        }

        public string GetStringFromInputsDic(KeyCode input)
        {
            return dic2[input];
        }

        public Key GetKeyFromKeyCode(KeyCode keyCode)
        {
            return keyMapping[keyCode];
        }

        internal void TranslateMapping(string input, out VirtualKeyCode action)
        {
            VirtualKeyCode value;
            if (this.actionMappings.TryGetValue(input, out value))
            {
                action = value;
            }
            else
            {
                throw new Exception("Action not found in mapping");
            }
        }

        internal bool CaptureInputs;
        internal string CurrentTASFile;
        internal InputSimulator Simulator;

        // Token: 0x04000010 RID: 16
        internal int frame;

        // Token: 0x04000011 RID: 17
        internal readonly List<string> PlayerActions = new List<string>
        {
            "Jump",
            "Dodge",
            "Slide",
            "Punch",
            "Hook",
            "LastUsedWeapon",
            "ChangeVariation",
            "ChangeFist",
            "Slot1",
            "Slot2",
            "Slot3",
            "Slot4",
            "Slot5",
            "Slot6"
        };

        internal UnityEngine.UI.Text status;

        internal bool LoopRunning;

        internal bool TimePaused;

        internal bool TimePausedUndone = true;

        internal float prevTimeScale = 1f;

        internal int ReplayINT;

        internal bool PlayingTAS;

        internal List<Key> pressedDownKeys = new();

        internal List<VirtualKeyCode> inputActionStates = new();


        internal HashSet<VirtualKeyCode> pressedSpecialKeys = new();

        internal List<ValueTuple<VirtualKeyCode, bool>> KeysStatus = new();

        internal Dictionary<KeyCode, string> dic2 = new();

        internal Dictionary<string, VirtualKeyCode> actionMappings = new();
        private Dictionary<KeyCode, Key> keyMapping = new()
        {
            { (KeyCode)97, (Key)15 },
            { (KeyCode)98, (Key)16 },
            { (KeyCode)99, (Key)17 },
            { (KeyCode)100, (Key)18 },
            { (KeyCode)101, (Key)19 },
            { (KeyCode)102, (Key)20 },
            { (KeyCode)103, (Key)21 },
            { (KeyCode)104, (Key)22 },
            { (KeyCode)105, (Key)23 },
            { (KeyCode)106, (Key)24 },
            { (KeyCode)107, (Key)25 },
            { (KeyCode)108, (Key)26 },
            { (KeyCode)109, (Key)27 },
            { (KeyCode)110, (Key)28 },
            { (KeyCode)111, (Key)29 },
            { (KeyCode)112, (Key)30 },
            { (KeyCode)113, (Key)31 },
            { (KeyCode)114, (Key)32 },
            { (KeyCode)115, (Key)33 },
            { (KeyCode)116, (Key)34 },
            { (KeyCode)117, (Key)35 },
            { (KeyCode)118, (Key)36 },
            { (KeyCode)119, (Key)37 },
            { (KeyCode)120, (Key)38 },
            { (KeyCode)121, (Key)39 },
            { (KeyCode)122, (Key)40 },
            { (KeyCode)48, (Key)50 },
            { (KeyCode)49, (Key)41 },
            { (KeyCode)50, (Key)42 },
            { (KeyCode)51, (Key)43 },
            { (KeyCode)52, (Key)44 },
            { (KeyCode)53, (Key)45 },
            { (KeyCode)54, (Key)46 },
            { (KeyCode)55, (Key)47 },
            { (KeyCode)56, (Key)48 },
            { (KeyCode)57, (Key)49 },
            { (KeyCode)32, (Key)1 },
            { (KeyCode)13, (Key)2 },
            { (KeyCode)27, (Key)60 },
            { (KeyCode)8, (Key)65 },
            { (KeyCode)9, (Key)3 },
            { (KeyCode)304, (Key)51 },
            { (KeyCode)303, (Key)52 },
            { (KeyCode)306, (Key)55 },
            { (KeyCode)305, (Key)56 },
            { (KeyCode)308, (Key)53 },
            { (KeyCode)307, (Key)54 },
            { (KeyCode)273, (Key)63 },
            { (KeyCode)274, (Key)64 },
            { (KeyCode)276, (Key)61 },
            { (KeyCode)275, (Key)62 }
        };
    }
}
