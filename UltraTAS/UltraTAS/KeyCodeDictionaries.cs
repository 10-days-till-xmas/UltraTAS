using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using WindowsInput.Native;

namespace _UltraTAS
{
    internal static class KeyCodeDictionaries
    {

        internal static readonly Dictionary<KeyCode, Key> keyMapping = new()
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

        internal static readonly Dictionary<KeyCode, VirtualKeyCode> UnityToVirtualKeyCode = new()
        {
            { (KeyCode)97, VirtualKeyCode.VK_A },
            { (KeyCode)98, VirtualKeyCode.VK_B },
            { (KeyCode)99, VirtualKeyCode.VK_C },
            { (KeyCode)100, VirtualKeyCode.VK_D },
            { (KeyCode)101, VirtualKeyCode.VK_E },
            { (KeyCode)102, VirtualKeyCode.VK_F },
            { (KeyCode)103, VirtualKeyCode.VK_G },
            { (KeyCode)104, VirtualKeyCode.VK_H },
            { (KeyCode)105, VirtualKeyCode.VK_I },
            { (KeyCode)106, VirtualKeyCode.VK_J },
            { (KeyCode)107, VirtualKeyCode.VK_K },
            { (KeyCode)108, VirtualKeyCode.VK_L },
            { (KeyCode)109, VirtualKeyCode.VK_M },
            { (KeyCode)110, VirtualKeyCode.VK_N },
            { (KeyCode)111, VirtualKeyCode.VK_O },
            { (KeyCode)112, VirtualKeyCode.VK_P },
            { (KeyCode)113, VirtualKeyCode.VK_Q },
            { (KeyCode)114, VirtualKeyCode.VK_R },
            { (KeyCode)115, VirtualKeyCode.VK_S },
            { (KeyCode)116, VirtualKeyCode.VK_T },
            { (KeyCode)117, VirtualKeyCode.VK_U },
            { (KeyCode)118, VirtualKeyCode.VK_V },
            { (KeyCode)119, VirtualKeyCode.VK_W },
            { (KeyCode)120, VirtualKeyCode.VK_X },
            { (KeyCode)121, VirtualKeyCode.VK_Y },
            { (KeyCode)122, VirtualKeyCode.VK_Z },
            { (KeyCode)48, VirtualKeyCode.VK_0 },
            { (KeyCode)49, VirtualKeyCode.VK_1 },
            { (KeyCode)50, VirtualKeyCode.VK_2 },
            { (KeyCode)51, VirtualKeyCode.VK_3 },
            { (KeyCode)52, VirtualKeyCode.VK_4 },
            { (KeyCode)53, VirtualKeyCode.VK_5 },
            { (KeyCode)54, VirtualKeyCode.VK_6 },
            { (KeyCode)55, VirtualKeyCode.VK_7 },
            { (KeyCode)56, VirtualKeyCode.VK_8 },
            { (KeyCode)57, VirtualKeyCode.VK_9 },
            { (KeyCode)13, VirtualKeyCode.RETURN },
            { (KeyCode)27, VirtualKeyCode.ESCAPE },
            { (KeyCode)8, VirtualKeyCode.BACK },
            { (KeyCode)9, VirtualKeyCode.TAB },
            { (KeyCode)32, VirtualKeyCode.SPACE },
            { (KeyCode)273, VirtualKeyCode.UP },
            { (KeyCode)274, VirtualKeyCode.DOWN },
            { (KeyCode)276, VirtualKeyCode.LEFT },
            { (KeyCode)275, VirtualKeyCode.RIGHT },
            { (KeyCode)304, VirtualKeyCode.LSHIFT },
            { (KeyCode)303, VirtualKeyCode.RSHIFT },
            { (KeyCode)306, VirtualKeyCode.LCONTROL },
            { (KeyCode)305, VirtualKeyCode.RCONTROL }
        };

        public static KeyCode GetKeyCodeFromInputsDic(string input) 
            => MonoSingleton<InputManager>.Instance.inputsDictionary[input];
        

        public static Key GetKeyFromKeyCode(KeyCode keyCode) 
            => keyMapping[keyCode];

        public static VirtualKeyCode GetVirtualKeyCode(KeyCode key)
            => UnityToVirtualKeyCode[key];
        
        internal static bool IsMouseInput(KeyCode key)
        {
            return KeyCode.Mouse0 <= key && key <= KeyCode.Mouse6;
        }

        internal static void SimulateKeybord(Key[] keys, bool press)
        {
            KeyboardState keyboardState = default;
            Keyboard device = InputSystem.GetDevice<Keyboard>();
            int i = 0;
            while (i < keys.Length)
            {
                Key key = keys[i];
                Key key2 = key;
                Key? keyFromKeyCode = GetKeyFromKeyCode(UltraTasConfig.PauseGame.Value);
                if (key2 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null)
                {
                    goto IL_B8;
                }
                Key key3 = key;
                keyFromKeyCode = GetKeyFromKeyCode(UltraTasConfig.AdvFrame.Value);
                if (key3 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null)
                {
                    goto IL_B8;
                }
                Key key4 = key;
                keyFromKeyCode = GetKeyFromKeyCode(UltraTasConfig.PlayTAS.Value);
                if (key4 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null)
                {
                    goto IL_B8;
                }
                Key key5 = key;
                keyFromKeyCode = GetKeyFromKeyCode(UltraTasConfig.StartRecording.Value);
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

        internal static void SimulateKeybord(List<Key> keys, bool press)
        {
            KeyboardState keyboardState = default;
            Keyboard device = InputSystem.GetDevice<Keyboard>();
            foreach (Key key in keys)
            {
                Key key2 = key;
                Key? keyFromKeyCode = GetKeyFromKeyCode(UltraTasConfig.PauseGame.Value);
                if (!(key2 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
                {
                    Key key3 = key;
                    keyFromKeyCode = GetKeyFromKeyCode(UltraTasConfig.AdvFrame.Value);
                    if (!(key3 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
                    {
                        Key key4 = key;
                        keyFromKeyCode = GetKeyFromKeyCode(UltraTasConfig.PlayTAS.Value);
                        if (!(key4 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
                        {
                            Key key5 = key;
                            keyFromKeyCode = GetKeyFromKeyCode(UltraTasConfig.StartRecording.Value);
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

        internal static void SimulateKeybord(Key key, bool press)
        {
            KeyCode[] AssignedKeyCodeList =
            [
                UltraTasConfig.PauseGame.Value,
                UltraTasConfig.AdvFrame.Value,
                UltraTasConfig.PlayTAS.Value,
                UltraTasConfig.StartRecording.Value
            ];

            static Key? quickKeyFromKeyCode(KeyCode keycode) => GetKeyFromKeyCode(keycode);


            bool IsKeyAssigned()
            {
                foreach (KeyCode keyCode in AssignedKeyCodeList)
                {
                    if (key == quickKeyFromKeyCode(keyCode).GetValueOrDefault() && quickKeyFromKeyCode(keyCode) != null)
                    {
                        return true;
                    }
                }
                return false;
            }

            if (IsKeyAssigned())
            {
                MonoBehaviour.print("Key is an assigned key so skipping");
            }

            Keyboard device = InputSystem.GetDevice<Keyboard>();
            if (device != null)
            {
                KeyboardState keyboardState;
                if (press)
                {
                    keyboardState = new([key]);
                }
                else
                {
                    keyboardState = default;
                }
                InputSystem.QueueStateEvent(device, keyboardState, -1.0);
                InputSystem.Update();
            }
        }

        internal static void SimulateMouseButton(bool left, bool press)
        {
            SimulateMouseButton(left ? MouseButton.Left : MouseButton.Right, press);
        }

        internal static void SimulateMouseButton(MouseButton button, bool press)
        {
            Mouse device = InputSystem.GetDevice<Mouse>();
            MouseState mouseState = default;
            if (device == null)
            {
                Debug.LogWarning("Mouse not found.");
            }
            else
            {
                mouseState.WithButton(button, press);
                InputSystem.QueueStateEvent(device, mouseState, -1.0);
            }
            InputSystem.QueueStateEvent(device, mouseState, -1.0);
        }
    }
}