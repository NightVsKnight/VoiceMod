using NvkCommon;
using CSCore.Streams.Effects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static NvkCommon.Log;

namespace VoiceMod
{
    partial class VoiceModUserControl
    {
        private PredefinedWrapper CurrentlySelectedPredefinedWrapper
        {
            get
            {
                return comboBoxPredefined.SelectedItem as PredefinedWrapper;
            }
            set
            {
                comboBoxPredefined.SelectedItem = value;
            }
        }

        private Predefined CurrentlySelectedPredefined
        {
            get
            {
                return CurrentlySelectedPredefinedWrapper?.Predefined;
            }
        }

        class Effect
        {
            public bool IsEnabled = false;
        }

        class EffectNoiseSuppression : Effect
        {
            public float Intensity = 1.0f;
        }

        class EffectCompressor : Effect
        {
            public float Ratio = DmoCompressorEffect.RatioDefault;
            public float Threshold = DmoCompressorEffect.ThresholdDefault;
            public float Predelay = DmoCompressorEffect.PredelayDefault;
            public float Attack = DmoCompressorEffect.AttackMin;
            public float Release = DmoCompressorEffect.ReleaseMax;
            public float Gain = DmoCompressorEffect.GainDefault;
        }

        class EffectPitch : Effect
        {
            public float Pitch = 1;
        }

        class EffectGain : Effect
        {
            public float Gain = 1;
        }

        class EffectChorus : Effect
        {
            public float Delay = DmoChorusEffect.DelayDefault;
            public float Depth = DmoChorusEffect.DepthDefault;
            public float Feedback = DmoChorusEffect.FeedbackDefault;
            public float Frequency = DmoChorusEffect.FrequencyDefault;
            public float WetDryMix = DmoChorusEffect.WetDryMixDefault;
        }

        class EffectReverb : Effect
        {
            public float Ratio = DmoWavesReverbEffect.HighFrequencyRTRatioDefault;
            public float InputGain = DmoWavesReverbEffect.InGainDefault;
            public float Mix = DmoWavesReverbEffect.ReverbMixDefault;
            public float Time = DmoWavesReverbEffect.ReverbTimeDefault;
        }

        class EffectHighPass : Effect
        {
            public float HighPass = 750;
        }

        class EffectDistortion : Effect
        {
            public float Gain = DmoDistortionEffect.GainDefault;
            public float Edge = DmoDistortionEffect.EdgeDefault;
            public float Center = DmoDistortionEffect.PostEQCenterFrequencyDefault;
            public float Bandwidth = DmoDistortionEffect.PostEQBandwidthDefault;
            public float LowPass = DmoDistortionEffect.PreLowPassCutoffDefault;
        }

        class Predefined
        {
            public string KeyBind = String.Empty;
            public bool IsEnabled = true;
            public EffectNoiseSuppression NoiseSuppression = new EffectNoiseSuppression();
            public EffectCompressor Compressor = new EffectCompressor();
            public EffectPitch Pitch = new EffectPitch();
            public EffectGain Gain = new EffectGain();
            public EffectChorus Chorus = new EffectChorus();
            public EffectReverb Reverb = new EffectReverb();
            public EffectHighPass HighPass = new EffectHighPass();
            public EffectDistortion Distortion = new EffectDistortion();
        }

        private const int PredefinedSchemaVersion = 1;

        private string[] PREDEFINEDS_CANNOT_REMOVE = { "Default", "Off", "Radio" };

        private static Predefined Default = new Predefined()
        {
            NoiseSuppression = new EffectNoiseSuppression()
            {
                IsEnabled = true,
            },
            Compressor = new EffectCompressor()
            {
                IsEnabled = true,
                Attack = DmoCompressorEffect.AttackMin,
                Release = DmoCompressorEffect.ReleaseMax,
            }
        };


        class Predefineds
        {
            public int SchemaVersion = 0;
            public Predefined Default = VoiceModUserControl.Default;
            public Predefined Off = new Predefined()
            {
                KeyBind = "RShiftKey+Scroll",
                IsEnabled = false,
            };
            public Predefined Radio = new Predefined()
            {
                KeyBind = "Scroll",
                NoiseSuppression = VoiceModUserControl.Default.NoiseSuppression,
                Compressor = VoiceModUserControl.Default.Compressor,
                HighPass = new EffectHighPass()
                {
                    IsEnabled = true,
                    HighPass = 750f,
                },
                Distortion = new EffectDistortion()
                {
                    IsEnabled = true,
                    Bandwidth = DmoDistortionEffect.PostEQBandwidthMin,
                },
            };
        }

        class PredefinedWrapper
        {
            public string Name;
            public Predefined Predefined;

            public PredefinedWrapper(string name) : this(name, new Predefined()) { }

            public PredefinedWrapper(string name, Predefined predefined)
            {
                Name = name;
                Predefined = predefined;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        const string FILENAME_PREDEFINEDS_JSON = "predefineds.json";

        private void LoadPredefineds()
        {
            Predefineds predefineds;
            try
            {
                var predefinedsJson = File.ReadAllText(FILENAME_PREDEFINEDS_JSON);
                predefineds = JsonConvert.DeserializeObject<Predefineds>(predefinedsJson);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is JsonReaderException)
            {
                predefineds = new Predefineds();
            }

            if (predefineds.SchemaVersion != PredefinedSchemaVersion)
            {
                // TODO: Upgrade existing file to new SchemaVersion...
                //...

                predefineds.SchemaVersion = PredefinedSchemaVersion;
            }

            comboBoxPredefined.Items.Clear();
            comboBoxPredefined.BeginUpdate();
            foreach (var field in predefineds.GetType().GetFields())
            {
                Log.PrintLine(TAG, LogLevel.Information, $"LoadPredefineds: field={field}");
                var fieldName = field.Name;
                var fieldValue = field.GetValue(predefineds);
                if (fieldValue is Predefined)
                {
                    var predefined = fieldValue as Predefined;

                    var predefinedWrapper = new PredefinedWrapper(fieldName, predefined);
                    comboBoxPredefined.Items.Add(predefinedWrapper);

                    KeyBindPut(predefined.KeyBind, predefinedWrapper);
                }
            }
            comboBoxPredefined.SelectedIndex = 0;
            comboBoxPredefined.EndUpdate();

            SavePredefineds();

            UpdateControlsFromSelectedPredefinedWrapper();
        }

        private void SavePredefineds()
        {
            var predefineds = new Dictionary<string, object>();
            predefineds["SchemaVersion"] = PredefinedSchemaVersion;
            foreach (PredefinedWrapper item in comboBoxPredefined.Items)
            {
                predefineds[item.Name] = item.Predefined;
            }
            var predefinedsJson = JsonConvert.SerializeObject(predefineds, Formatting.Indented);
            //Log.PrintLine(TAG, LogLevel.Information, $"SavePredefineds: predefinedsJson={Utils.Quote(predefinedsJson)}");
            File.WriteAllText(FILENAME_PREDEFINEDS_JSON, predefinedsJson);
        }

        private void buttonPredefinedsSave_Click(object sender, EventArgs e)
        {
            SavePredefineds();
        }

        private void buttonPredefinedAdd_Click(object sender, EventArgs e)
        {
            var selectedIndex = comboBoxPredefined.SelectedIndex;
            //Log.PrintLine(TAG, LogLevel.Information, $"buttonPredefinedAdd_Click: selectedIndex={selectedIndex}");
            PredefinedWrapper predefinedWrapper;
            if (selectedIndex == -1)
            {
                predefinedWrapper = new PredefinedWrapper(comboBoxPredefined.Text);
            }
            else
            {
                predefinedWrapper = CurrentlySelectedPredefinedWrapper;
            }
            Log.PrintLine(TAG, LogLevel.Information, $"buttonPredefinedAdd_Click: predefinedWrapper.Name={predefinedWrapper.Name}");
            UpdatePredefinedWrapperFromControls(predefinedWrapper);
            SavePredefineds();
            if (selectedIndex == -1)
            {
                selectedIndex = comboBoxPredefined.Items.Add(predefinedWrapper);
                comboBoxPredefined.SelectedIndex = selectedIndex;
            }
        }

        private void buttonPredefinedRemove_Click(object sender, EventArgs e)
        {
            var selectedIndex = comboBoxPredefined.SelectedIndex;
            Log.PrintLine(TAG, LogLevel.Information, $"buttonPredefinedRemove_Click: selectedIndex={selectedIndex}");
            comboBoxPredefined.Items.RemoveAt(selectedIndex);
            SavePredefineds();
            comboBoxPredefined.SelectedIndex = Math.Max(0, selectedIndex - 1);
            if (!HasKeyBinds)
            {
                HotKeyListenerStop();
            }
        }

        private void comboBoxPredefined_SelectedValueChanged(object sender, EventArgs e)
        {
            var selectedPredefinedWrapper = CurrentlySelectedPredefinedWrapper;
            Log.PrintLine(TAG, LogLevel.Information, $"comboBoxPredefined_SelectedValueChanged: selectedPredefinedWrapper={selectedPredefinedWrapper}");
            buttonPredefinedRemove.Enabled = !PREDEFINEDS_CANNOT_REMOVE.Contains(selectedPredefinedWrapper?.Name);
            UpdateControlsFromSelectedPredefinedWrapper();
        }

        private void UpdateControlsFromSelectedPredefinedWrapper()
        {
            var selectedPredefinedWrapper = CurrentlySelectedPredefinedWrapper;
            if (selectedPredefinedWrapper == null) return;

            Log.PrintLine(TAG, LogLevel.Information, $"UpdateControlsFromSelectedPredefinedWrapper: selectedPredefinedWrapper.Name={selectedPredefinedWrapper.Name}");

            var predefined = selectedPredefinedWrapper.Predefined;

            ResetEffects(true);
            textBoxPredefinedKeyBind.Text = null;
            var isDefault = selectedPredefinedWrapper == comboBoxPredefined.Items[0];
            textBoxPredefinedKeyBind.Enabled = buttonPredefinedKeyBindAdd.Enabled = buttonPredefinedKeyBindRemove.Enabled = !isDefault;

            var predefinedFields = predefined.GetType().GetFields();
            foreach (var predefinedField in predefinedFields)
            {
                //Log.PrintLine(TAG, LogLevel.Information, $"UpdateControlsFromSelectedPredefinedWrapper: predefinedField={predefinedField}");
                var predefinedFieldName = predefinedField.Name;
                //Log.PrintLine(TAG, LogLevel.Information, $"UpdateControlsFromSelectedPredefinedWrapper: predefinedFieldName={predefinedFieldName}");
                var predefinedFieldValue = predefinedField.GetValue(predefined);
                //Log.PrintLine(TAG, LogLevel.Information, $"UpdateControlsFromSelectedPredefinedWrapper: predefinedFieldValue={predefinedFieldValue}");
                switch (predefinedFieldName)
                {
                    case "KeyBind":
                        {
                            var keyBind = predefinedFieldValue as string;
                            //Log.PrintLine(TAG, LogLevel.Information, $"UpdateControlsFromSelectedPredefinedWrapper: keyBind={Utils.Quote(keyBind)}");
                            textBoxPredefinedKeyBind.Text = keyBind;
                            break;
                        }
                    case "IsEnabled":
                        if (predefinedFieldValue is bool)
                        {
                            checkBoxEffectsEnabled.Checked = (bool)predefinedFieldValue;
                        }
                        break;
                    default:
                        {
                            EffectsWrapper effectsWrapper;
                            if (mapEffectNameToEffectsWrapper.TryGetValue(predefinedFieldName, out effectsWrapper))
                            {
                                //Log.PrintLine(TAG, LogLevel.Information, $"UpdateControlsFromSelectedPredefinedWrapper: effectsWrapper={effectsWrapper}");
                                var predefinedParameterFields = predefinedFieldValue.GetType().GetFields();
                                foreach (var predefinedParameterField in predefinedParameterFields)
                                {
                                    //Log.PrintLine(TAG, LogLevel.Information, $"UpdateControlsFromSelectedPredefinedWrapper: predefinedParameterField={predefinedParameterField}");
                                    var predefinedParameterFieldName = predefinedParameterField.Name;
                                    //Log.PrintLine(TAG, LogLevel.Information, $"UpdateControlsFromSelectedPredefinedWrapper: predefinedParameterFieldName={predefinedParameterFieldName}");
                                    var predefinedParameterFieldValue = predefinedParameterField.GetValue(predefinedFieldValue);
                                    //Log.PrintLine(TAG, LogLevel.Information, $"UpdateControlsFromSelectedPredefinedWrapper: predefinedParameterFieldValue={predefinedParameterFieldValue}");
                                    switch (predefinedParameterFieldName)
                                    {
                                        case "IsEnabled":
                                            if (predefinedParameterFieldValue is bool)
                                            {
                                                effectsWrapper.CheckBox.Checked = (bool)predefinedParameterFieldValue;
                                            }
                                            break;
                                        default:
                                            {
                                                TrackBar trackBar;
                                                if (effectsWrapper.TrackBars.TryGetValue(predefinedParameterFieldName, out trackBar))
                                                {
                                                    //Log.PrintLine(TAG, LogLevel.Information, $"UpdateControlsFromSelectedPredefinedWrapper: trackBar={trackBar}");
                                                    if (predefinedParameterFieldValue is float)
                                                    {
                                                        var predefinedParameterValue = (float)predefinedParameterFieldValue;
                                                        //Log.PrintLine(TAG, LogLevel.Information, $"UpdateControlsFromSelectedPredefinedWrapper: predefinedParameterValue={predefinedParameterValue}");
                                                        TrackBarWrapper.SetValue(trackBar, predefinedParameterValue);
                                                    }
                                                }
                                                break;
                                            }
                                    }
                                }
                            }
                            break;
                        }
                }
            }
        }

        private void UpdatePredefinedWrapperFromControls(PredefinedWrapper predefinedWrapper)
        {
            Log.PrintLine(TAG, LogLevel.Information, $"UpdatePredefinedWrapperFromControls: predefinedWrapper.Name={predefinedWrapper.Name}");

            var predefined = predefinedWrapper.Predefined;

            var predefinedFields = predefined.GetType().GetFields();
            foreach (var predefinedField in predefinedFields)
            {
                //Log.PrintLine(TAG, LogLevel.Information, $"UpdatePredefinedWrapperFromControls: predefinedField={predefinedField}");
                var predefinedFieldName = predefinedField.Name;
                //Log.PrintLine(TAG, LogLevel.Information, $"UpdatePredefinedWrapperFromControls: predefinedFieldName={predefinedFieldName}");
                switch (predefinedFieldName)
                {
                    case "KeyBind":
                        {
                            var keyBind = textBoxPredefinedKeyBind.Text;
                            //Log.PrintLine(TAG, LogLevel.Information, $"UpdatePredefinedWrapperFromControls: keyBind={Utils.Quote(keyBind)}");
                            predefinedField.SetValue(predefined, keyBind);
                            break;
                        }
                    case "IsEnabled":
                        {
                            var isEnabled = checkBoxEffectsEnabled.Checked;
                            //Log.PrintLine(TAG, LogLevel.Information, $"UpdatePredefinedWrapperFromControls: isEnabled={isEnabled}");
                            predefinedField.SetValue(predefined, isEnabled);
                            break;
                        }
                    default:
                        {
                            var predefinedFieldValue = predefinedField.GetValue(predefined);
                            //Log.PrintLine(TAG, LogLevel.Information, $"UpdatePredefinedWrapperFromControls: predefinedFieldValue={predefinedFieldValue}");

                            EffectsWrapper effectsWrapper;
                            if (mapEffectNameToEffectsWrapper.TryGetValue(predefinedFieldName, out effectsWrapper))
                            {
                                //Log.PrintLine(TAG, LogLevel.Information, $"UpdatePredefinedWrapperFromControls: effectsWrapper={effectsWrapper}");
                                var predefinedParameterFields = predefinedFieldValue.GetType().GetFields();
                                foreach (var predefinedParameterField in predefinedParameterFields)
                                {
                                    //Log.PrintLine(TAG, LogLevel.Information, $"UpdatePredefinedWrapperFromControls: predefinedParameterField={predefinedParameterField}");
                                    var predefinedParameterFieldName = predefinedParameterField.Name;
                                    //Log.PrintLine(TAG, LogLevel.Information, $"UpdatePredefinedWrapperFromControls: predefinedParameterFieldName={predefinedParameterFieldName}");
                                    switch (predefinedParameterFieldName)
                                    {
                                        case "IsEnabled":
                                            {
                                                var isEnabled = effectsWrapper.CheckBox.Checked;
                                                //Log.PrintLine(TAG, LogLevel.Information, $"UpdatePredefinedWrapperFromControls: isEnabled={isEnabled}");
                                                predefinedParameterField.SetValue(predefinedFieldValue, isEnabled);
                                                break;
                                            }
                                        default:
                                            {
                                                TrackBar trackBar;
                                                if (effectsWrapper.TrackBars.TryGetValue(predefinedParameterFieldName, out trackBar))
                                                {
                                                    //Log.PrintLine(TAG, LogLevel.Information, $"UpdatePredefinedWrapperFromControls: trackBar={EffectsWrapper.ToString(trackBar)}");
                                                    var predefinedParameterValue = TrackBarWrapper.GetValue(trackBar);
                                                    //Log.PrintLine(TAG, LogLevel.Information, $"UpdatePredefinedWrapperFromControls: predefinedParameterValue={predefinedParameterValue:0.00}");
                                                    predefinedParameterField.SetValue(predefinedFieldValue, predefinedParameterValue);
                                                }
                                                break;
                                            }
                                    }
                                }
                            }
                            break;
                        }
                }
            }
        }
    }
}
