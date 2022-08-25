using NvkCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using static NvkCommon.Log;

namespace VoiceMod
{
    partial class VoiceModUserControl
    {
        private Dictionary<string, PredefinedWrapper> mapKeyBindToPredefinedWrapper = new Dictionary<string, PredefinedWrapper>();

        private void InitializeKeyBind()
        {
            ParentForm.Deactivate += ParentForm_Deactivate;
        }

        private void ParentForm_Deactivate(object sender, EventArgs e)
        {
            Log.PrintLine(TAG, LogLevel.Information, $"ParentForm_Deactivate: e={e}");
            HotKeyListenerStop();
        }

        private bool HasKeyBinds
        {
            get
            {
                return mapKeyBindToPredefinedWrapper.Count > 0;
            }
        }

        private bool KeyBindGet(string keyBind, out PredefinedWrapper predefinedWrapper)
        {
            return mapKeyBindToPredefinedWrapper.TryGetValue(keyBind, out predefinedWrapper);
        }

        private bool KeyBindPut(string keyBind, PredefinedWrapper predefinedWrapper)
        {
            if (String.IsNullOrEmpty(keyBind) || predefinedWrapper == null) return false;

            predefinedWrapper.Predefined.KeyBind = keyBind;
            SavePredefineds();

            var isNewEntry = !mapKeyBindToPredefinedWrapper.ContainsKey(keyBind);

            mapKeyBindToPredefinedWrapper.Add(keyBind, predefinedWrapper);

            if (isNewEntry)
            {
                if (mapKeyBindToPredefinedWrapper.Count == 1)
                {
                    HotKeyListenerStart();
                }
            }

            return isNewEntry;
        }

        private bool KeyBindRemove(string keyBind)
        {
            if (String.IsNullOrEmpty(keyBind)) return false;

            PredefinedWrapper predefinedWrapper;
            if (mapKeyBindToPredefinedWrapper.TryGetValue(keyBind, out predefinedWrapper))
            {
                predefinedWrapper.Predefined.KeyBind = String.Empty;
                SavePredefineds();

                mapKeyBindToPredefinedWrapper.Remove(keyBind);
                if (mapKeyBindToPredefinedWrapper.Count == 0)
                {
                    HotKeyListenerStop();
                }

                return true;
            }

            return false;
        }


        private bool IsKeyBindRecording
        {
            get
            {
                return textBoxPredefinedKeyBind.BackColor == Color.Red;
            }
            set
            {
                if (value)
                {
                    if (textBoxPredefinedKeyBind.BackColor != Color.Red)
                    {
                        textBoxPredefinedKeyBind.BackColor = Color.Red;
                        buttonPredefinedKeyBindAdd.Text = "*";
                        textBoxPredefinedKeyBind.Text = null;
                        HotKeyListenerStart();
                    }
                }
                else
                {
                    if (textBoxPredefinedKeyBind.BackColor != Color.Green)
                    {
                        textBoxPredefinedKeyBind.BackColor = Color.Green;
                        buttonPredefinedKeyBindAdd.Text = "+";
                        if (!HasKeyBinds)
                        {
                            HotKeyListenerStop();
                        }
                    }
                }
            }
        }

        private KeyboardHook keyboardHook = new KeyboardHook();

        private void HotKeyListenerStart()
        {
            Log.PrintLine(TAG, LogLevel.Information, $"HotKeyListenerStart()");
            if (keyboardHook.IsStarted) return;
            keyboardHook.KeysChanged += KeyboardHook_KeyIntercepted;
            keyboardHook.Start();
        }

        private void HotKeyListenerStop()
        {
            Log.PrintLine(TAG, LogLevel.Information, $"HotKeyListenerStop()");
            keyboardHook.KeysChanged -= KeyboardHook_KeyIntercepted;
            keyboardHook.Stop();
            IsKeyBindRecording = false;
        }

        private void KeyboardHook_KeyIntercepted(KeyboardHook.KeysChangedEventArgs e)
        {
            //Log.PrintLine(TAG, LogLevel.Information, $"KeyboardHook_KeyIntercepted: e={e}");
            //Log.PrintLine(TAG, LogLevel.Information, $"KeyboardHook_KeyIntercepted: e.KeysCurrentlyDown={e.KeysCurrentlyDown}");
            //Log.PrintLine(TAG, LogLevel.Information, $"KeyboardHook_KeyIntercepted: e.CapsLockOn={e.CapsLockOn}");
            var keyBind = String.Join("+", e.KeysCurrentlyDown);
            Log.PrintLine(TAG, LogLevel.Information, $"KeyboardHook_KeyIntercepted: keyBind={Utils.Quote(keyBind)}");

            if (IsKeyBindRecording)
            {
                if (e.MostRecentKeyDirection == KeyboardHook.KeyDirection.Down && !String.IsNullOrEmpty(keyBind))
                {
                    textBoxPredefinedKeyBind.Text = keyBind;
                }
            }
            else
            {
                if (String.IsNullOrEmpty(keyBind))
                {
                    comboBoxPredefined.SelectedIndex = 0;
                }
                else
                {
                    PredefinedWrapper predefinedWrapper;
                    if (KeyBindGet(keyBind, out predefinedWrapper))
                    {
                        Log.PrintLine(TAG, LogLevel.Information, $"KeyboardHook_KeyIntercepted: predefinedWrapper={predefinedWrapper}");
                        CurrentlySelectedPredefinedWrapper = predefinedWrapper;
                    }
                }
            }
        }

        private void buttonPredefinedKeyBindAdd_Click(object sender, EventArgs e)
        {
            if (IsKeyBindRecording)
            {
                var keyBind = textBoxPredefinedKeyBind.Text;

                var predefinedWrapper = CurrentlySelectedPredefinedWrapper;
                if (predefinedWrapper != null)
                {
                    KeyBindPut(keyBind, predefinedWrapper);
                }

                IsKeyBindRecording = false;
            }
            else
            {
                IsKeyBindRecording = true;
            }
        }

        private void buttonPredefinedKeyBindRemove_Click(object sender, EventArgs e)
        {
            if (IsKeyBindRecording)
            {
                IsKeyBindRecording = false;

                var predefinedWrapper = CurrentlySelectedPredefinedWrapper;
                if (predefinedWrapper != null)
                {
                    textBoxPredefinedKeyBind.Text = predefinedWrapper?.Predefined.KeyBind;
                }
            }
            else
            {
                var keyBindToRemove = textBoxPredefinedKeyBind.Text;
                textBoxPredefinedKeyBind.Text = null;

                var predefinedWrapper = CurrentlySelectedPredefinedWrapper;
                if (predefinedWrapper != null)
                {
                    KeyBindRemove(keyBindToRemove);
                }
            }
        }

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    }
}
