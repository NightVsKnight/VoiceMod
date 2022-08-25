using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Windows.Forms;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.SoundOut;
using CSCore.Streams;
using CSCore.Streams.Effects;
using CSCore.Win32;
using NvkCommon;
using static NvkCommon.Log;
using static VoiceMod.AudioDeviceManager;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using System.Drawing;
using System.Linq;

namespace VoiceMod
{
    public partial class VoiceModUserControl : UserControl, IPersistComponentSettings
    {
        private static readonly string TAG = Log.TAG(typeof(VoiceModUserControl));

        public VoiceModUserControl()
        {
            settings = new VoiceModSettings(this, SettingsKey);
            InitializeComponent();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
                if (SaveSettings)
                {
                    SaveComponentSettings();
                }
            }
            base.Dispose(disposing);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (DesignMode) return;

            InitializeKeyBind();

            groupBoxNoiseSuppression.Tag = groupBoxNoiseSuppression.Height;
            GroupExpandCollapseToggle(groupBoxNoiseSuppression, buttonNoiseSuppression);

            groupBoxCompressor.Tag = groupBoxCompressor.Height;
            GroupExpandCollapseToggle(groupBoxCompressor, buttonCompressor);

            groupBoxChorus.Tag = groupBoxChorus.Height;
            GroupExpandCollapseToggle(groupBoxChorus, buttonChorus);

            groupBoxReverb.Tag = groupBoxReverb.Height;
            GroupExpandCollapseToggle(groupBoxReverb, buttonReverb);

            groupBoxDistortion.Tag = groupBoxDistortion.Height;
            GroupExpandCollapseToggle(groupBoxDistortion, buttonDistortion);

            MapEffectsControlsBuild();

            ToolTipsInitialize();

            AudioLoad();

            LoadEffects();

            LoadPredefineds();

            LoadComponentSettings();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            if (DesignMode) return;
            HotKeyListenerStop();
            AudioUnload();
        }

        #region IPersistComponentSettings

        private readonly VoiceModSettings settings;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Bindable(false)]
        [Browsable(false)]
        public bool SaveSettings { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Bindable(false)]
        [Browsable(false)]
        public string SettingsKey
        {
            get
            {
                return this.Name;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public void LoadComponentSettings()
        {
            if (DesignMode) return;
            Log.PrintLine(TAG, LogLevel.Information, $"LoadComponentSettings()");
            AudioDevicesUpdate("LoadComponentSettings", false);

            // Should be last to only start capturing after all other settings are loaded
            IsAudioCaptureEnabled = settings.IsAudioCaptureEnabled;
        }

        public void SaveComponentSettings()
        {
            if (DesignMode) return;
            Log.PrintLine(TAG, LogLevel.Information, $"SaveComponentSettings()");
            settings.IsAudioCaptureEnabled = IsAudioCaptureEnabled;
            settings.Save();
        }

        public void ResetComponentSettings()
        {
            if (DesignMode) return;
            Log.PrintLine(TAG, LogLevel.Information, $"ResetComponentSettings()");
            settings.Reset();
            LoadComponentSettings();
        }

        #endregion IPersistComponentSettings

        //
        //
        //

        class EffectsWrapper
        {
            public static string Legalize(string value)
            {
                return value?.Replace(" ", "");
            }

            public static string ToString(CheckBox checkBox)
            {
                return $"{{ Name={Utils.Quote(checkBox.Name)}, Checked={checkBox.Checked} }}";
            }

            public static string ToString(TrackBar trackBar)
            {
                return $"{{ Name={Utils.Quote(trackBar.Name)}, Value={trackBar.Value} }}";
            }

            public static string ToString(Dictionary<string, TrackBar> dictionary)
            {
                var sb = new StringBuilder();
                sb.Append("{");
                foreach (var keyValue in dictionary)
                {
                    sb.Append(Utils.Quote(keyValue.Key)).Append(":").Append(ToString(keyValue.Value)).Append(",");
                }
                sb.Append("}");
                return sb.ToString();
            }

            public bool IsValid { get { return CheckBox != null && TrackBars.Count > 0; } }
            public string Name { get { return Legalize(CheckBox?.Text); } }
            public CheckBox CheckBox;
            public readonly Dictionary<string, TrackBar> TrackBars = new Dictionary<string, TrackBar>();

            public override string ToString()
            {
                return $"{{ IsValid={IsValid}, Name={Utils.Quote(Name)}, CheckBox={ToString(CheckBox)}, TrackBars={ToString(TrackBars)} }}";
            }
        }

        /**
         * {
         *  'NoiseSuppression':EffectsWrapper(checkBoxNoiseSuppression, {
         *      'NoiseSuppression':trackBarNoiseSuppression,
         *  }),
         *  'Compressor':EffectsWrapper(checkBoxCompressor, {
         *      'Ratio':trackBarCompressorRatio,
         *      ...
         *  }),
         *  'Pitch':EffectsWrapper(checkBoxPitch, {
         *      'Pitch':trackBarPitch,
         *  }),
         *  'Gain':EffectsWrapper(checkBoxGain, {
         *      'Gain':trackBarGain,
         *  }),
         *  'Chorus':EffectsWrapper(checkBoxChorus, {
         *      'Delay':trackBarChorusDelay,
         *      ...
         *  }),
         *  'Reverb':EffectsWrapper(checkBoxReverb, {
         *      'Ratio':trackBarReverbRatio,
         *      ...
         *  }),
         *  'HighPass':EffectsWrapper(checkBoxHighPass, {
         *      'HighPass':trackBarHighPass,
         *  }),
         *  'Distortion':EffectsWrapper(checkBoxDistortion, {
         *      'Gain':trackBarDistortionGain,
         *      ...
         *  }),
         * }
         */
        private Dictionary<string, EffectsWrapper> mapEffectNameToEffectsWrapper = new Dictionary<string, EffectsWrapper>();

        private void MapEffectControlAdd(EffectsWrapper effectsWrapper, Panel panel)
        {
            string keyControl = null;
            TrackBar valueTrackBar = null;
            foreach (Control control in panel.Controls)
            {
                if (control is Label)
                {
                    if (!control.Name.EndsWith("Unit"))
                    {
                        keyControl = EffectsWrapper.Legalize(control.Text);
                    }
                }
                else if (control is CheckBox)
                {
                    effectsWrapper.CheckBox = control as CheckBox;
                    keyControl = EffectsWrapper.Legalize(control.Text);
                }
                else if (control is TrackBar)
                {
                    valueTrackBar = control as TrackBar;
                }
            }
            if (keyControl != null && valueTrackBar != null)
            {
                //Log.PrintLine(TAG, LogLevel.Information, $"MapEffectControlAdd: effectsWrapper.TrackBars[{Utils.Quote(keyControl)}] = {valueTrackBar}");
                effectsWrapper.TrackBars[keyControl] = valueTrackBar;
            }
        }

        private void MapEffectsControlsBuild()
        {
            mapEffectNameToEffectsWrapper.Clear();
            foreach (Control panelEffectsControl in panelEffects.Controls)
            {
                var effectsWrapper = new EffectsWrapper();
                if (panelEffectsControl is GroupBox)
                {
                    var groupBoxEffect = panelEffectsControl as GroupBox;
                    foreach (Control effectControl in groupBoxEffect.Controls)
                    {
                        if (effectControl is CheckBox)
                        {
                            effectsWrapper.CheckBox = effectControl as CheckBox;
                        }
                        else if (effectControl is Panel)
                        {
                            MapEffectControlAdd(effectsWrapper, effectControl as Panel);
                        }
                    }
                }
                else if (panelEffectsControl is Panel)
                {
                    MapEffectControlAdd(effectsWrapper, panelEffectsControl as Panel);
                }
                if (effectsWrapper.IsValid)
                {
                    //Log.PrintLine(TAG, LogLevel.Information, $"MapEffectsControlsBuild: mapEffectNameToEffectsWrapper[{Utils.Quote(effectsWrapper.Name)}] = {effectsWrapper}");
                    mapEffectNameToEffectsWrapper[effectsWrapper.Name] = effectsWrapper;
                }
            }
        }

        private void ToolTipsInitialize()
        {
            toolTip1.SetToolTip(labelCapture, "Usually \"Headset Microphone (Corsair VOID Wireless Gaming Dongle)\"\nDouble-Click to Reset");
            toolTip1.SetToolTip(labelRender, "Usually \"CABLE-C Input (VB-Audio Cable C)\"");
        }

        //
        //
        //

        class TrackBarWrapper
        {
            public static void Test()
            {
                var trackBar = new TrackBar();
                TrackBarWrapper.Wrap(trackBar, 0, 1.0f, 1.0f);
                TrackBarWrapper.Wrap(trackBar, 0, 1.0f, 0.0f);
                TrackBarWrapper.Wrap(trackBar, 0, 1.0f, 0.3f);
                TrackBarWrapper.Wrap(trackBar, 0, 1.0f, 0.5f);
                TrackBarWrapper.Wrap(trackBar, 0, 1.0f, 0.7f);
                TrackBarWrapper.Wrap(trackBar, 0, 75, 50);
                TrackBarWrapper.Wrap(trackBar, 25, 100, 50);
                TrackBarWrapper.Wrap(trackBar, -100, -25, -50);
                TrackBarWrapper.Wrap(trackBar, -75, 0, -25);
                TrackBarWrapper.Wrap(trackBar, -50, 25, -25);
                TrackBarWrapper.Wrap(trackBar, -25, 50, 25);
            }

            public static void Wrap(TrackBar trackBar, float min, float max, float value)
            {
                trackBar.Tag = new TrackBarWrapper(trackBar, min, max);
                SetValue(trackBar, value);
            }

            public static void SetValue(TrackBar trackBar, float value)
            {
                (trackBar.Tag as TrackBarWrapper)?.ValueSet(value);
            }

            public static float GetValue(TrackBar trackBar)
            {
                return (trackBar.Tag as TrackBarWrapper)?.ValueGet() ?? float.NaN;
            }

            public TrackBar TrackBar { get; protected set; }
            public float Min { get; protected set; }
            public float Max { get; protected set; }

            private TrackBarWrapper(TrackBar trackBar, float min, float max, int resolution = 100)
            {
                trackBar.Minimum = 0;
                trackBar.Maximum = resolution;
                trackBar.TickFrequency = 10;
                TrackBar = trackBar;
                Min = min;
                Max = max;
            }

            public void ValueSet(float value)
            {
                var valueMin = Min;
                var valueMax = Max;
                var valueNormalized = value - valueMin;
                var valueRange = Math.Abs(valueMax - valueMin);
                var valuePercent = valueNormalized / valueRange;
                var trackBar = TrackBar;
                var trackBarMin = trackBar.Minimum;
                var trackBarMax = trackBar.Maximum;
                var trackBarRange = trackBarMax - trackBarMin;
                var trackBarValue = (int)Math.Round(trackBarRange * valuePercent);
                trackBar.Value = trackBarValue;
                if (false)
                {
                    var testValue = ValueGet();
                    Log.PrintLine(TAG, LogLevel.Information, $"ValueSet: value={value}, testValue={testValue}");
                }
            }

            public float ValueGet()
            {
                var valueMin = Min;
                var valueMax = Max;
                var valueRange = Math.Abs(valueMax - valueMin);
                var trackBar = TrackBar;
                var trackBarMin = trackBar.Minimum;
                var trackBarMax = trackBar.Maximum;
                var trackBarValue = trackBar.Value;
                var trackBarRange = trackBarMax - trackBarMin;
                var trackBarPercent = trackBarValue / (float)trackBarRange;
                var value = valueMin + (valueRange * trackBarPercent);
                return value;
            }
        }

        private void LoadEffects()
        {
            if (false)
            {
                TrackBarWrapper.Test();
            }

            LoadNoiseSuppression();
            LoadCompressor();
            LoadPitch();
            LoadGain();
            LoadChorus();
            LoadReverb();
            LoadHighPass();
            LoadDistortion();
        }

        private void ResetEffects(bool hard = false)
        {
            ResetNoiseSuppression(hard);
            ResetCompressor(hard);
            ResetPitch(hard);
            ResetGain(hard);
            ResetChorus(hard);
            ResetReverb(hard);
            ResetHighPass(hard);
            ResetDistortion(hard);
        }

        //
        // Noise Suppression
        //

        private void LoadNoiseSuppression()
        {
            TrackBarWrapper.Wrap(trackBarNoiseSuppressionIntensity, 0.0f, 1.0f, 1.0f);
        }

        private void ResetNoiseSuppression(bool hard = false)
        {
            TrackBarWrapper.SetValue(trackBarNoiseSuppressionIntensity, 1.0f);
        }

        private void UpdateNoiseSuppression()
        {
            var isEnabled = checkBoxNoiseSuppression.Checked;
            Log.PrintLine(TAG, LogLevel.Information, $"NoiseSuppressionUpdate: isEnabled={isEnabled}");
            if (mNoiseSuppression != null)
            {
                mNoiseSuppression.Enable = isEnabled;
                checkBoxNoiseSuppression.Checked = mNoiseSuppression.IsEnabled;
            }

            UpdateNoiseSuppressionIntensity();
        }

        private void UpdateNoiseSuppressionIntensity()
        {
            float intensity_ratio = TrackBarWrapper.GetValue(trackBarNoiseSuppressionIntensity);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateNoiseSuppressionIntensity: intensity_ratio={intensity_ratio}");
            textBoxNoiseSuppressionIntensity.Text = String.Format("{0:0.00}", intensity_ratio);
            if (mNoiseSuppression != null)
            {
                if (mNoiseSuppression.IntensityRatioSet(intensity_ratio))
                {
                    Log.PrintLine(TAG, LogLevel.Information, $"UpdateNoiseSuppressionIntensity: IntensityRatioSet({intensity_ratio:0.00}) success");
                }
                else
                {
                    Log.PrintLine(TAG, LogLevel.Information, $"UpdateNoiseSuppressionIntensity: IntensityRatioSet({intensity_ratio:0.00}) failed");
                }
            }
        }

        private void NoiseSuppression_OnEnabledChanged(object sender, EventArgs e)
        {
            checkBoxNoiseSuppression.Checked = mNoiseSuppression != null ? mNoiseSuppression.IsEnabled : false;
        }

        //
        // Compressor
        //

        private void LoadCompressor()
        {
            TrackBarWrapper.Wrap(trackBarCompressorRatio, DmoCompressorEffect.RatioMin, DmoCompressorEffect.RatioMax, DmoCompressorEffect.RatioDefault);
            TrackBarWrapper.Wrap(trackBarCompressorThreshold, DmoCompressorEffect.ThresholdMin, DmoCompressorEffect.ThresholdMax, DmoCompressorEffect.ThresholdDefault);
            TrackBarWrapper.Wrap(trackBarCompressorPredelay, DmoCompressorEffect.PredelayMin, DmoCompressorEffect.PredelayMax, DmoCompressorEffect.PredelayDefault);

            TrackBarWrapper.Wrap(trackBarCompressorAttack, DmoCompressorEffect.AttackMin, DmoCompressorEffect.AttackMax, DmoCompressorEffect.AttackMin);
            trackBarCompressorAttack.Enabled = false;

            TrackBarWrapper.Wrap(trackBarCompressorRelease, DmoCompressorEffect.ReleaseMin, DmoCompressorEffect.ReleaseMax, DmoCompressorEffect.ReleaseMax);
            trackBarCompressorRelease.Enabled = false;

            TrackBarWrapper.Wrap(trackBarCompressorGain, DmoCompressorEffect.GainMin, DmoCompressorEffect.GainMax, DmoCompressorEffect.GainDefault);
        }

        private void ResetCompressor(bool hard = false)
        {
            if (hard)
            {
                checkBoxCompressor.Checked = false;
            }
            ResetCompressorRatio();
            ResetCompressorThreshold();
            ResetCompressorPredelay();
            ResetCompressorAttack();
            ResetCompressorRelease();
            ResetCompressorGain();
        }

        private void ResetCompressorRatio()
        {
            TrackBarWrapper.SetValue(trackBarCompressorRatio, DmoCompressorEffect.RatioDefault);
        }

        private void ResetCompressorThreshold()
        {
            TrackBarWrapper.SetValue(trackBarCompressorThreshold, DmoCompressorEffect.ThresholdDefault);
        }

        private void ResetCompressorPredelay()
        {
            TrackBarWrapper.SetValue(trackBarCompressorPredelay, DmoCompressorEffect.PredelayDefault);
        }

        private void ResetCompressorAttack()
        {
            TrackBarWrapper.SetValue(trackBarCompressorAttack, DmoCompressorEffect.AttackMin);
        }

        private void ResetCompressorRelease()
        {
            TrackBarWrapper.SetValue(trackBarCompressorRelease, DmoCompressorEffect.ReleaseMax);
        }

        private void ResetCompressorGain()
        {
            TrackBarWrapper.SetValue(trackBarCompressorGain, DmoCompressorEffect.GainDefault);
        }

        private void UpdateCompressor()
        {
            if (mCompressorEffect == null) return;
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateCompressor()");
            var isEnabled = IsCompressorEnabled;
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateCompressor: isEnabled={isEnabled}");
            if (mCompressorEffect != null)
            {
                mCompressorEffect.IsEnabled = isEnabled;
            }
            UpdateCompressorRatio();
            UpdateCompressorThreshold();
            UpdateCompressorPredelay();
            UpdateCompressorAttack();
            UpdateCompressorRelease();
            UpdateCompressorGain();
        }

        private void UpdateCompressorRatio()
        {
            var ratio = TrackBarWrapper.GetValue(trackBarCompressorRatio);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateCompressor: ratio={ratio}");
            textBoxCompressorRatio.Text = String.Format("{0:0.00}", ratio);
            if (mCompressorEffect != null)
            {
                mCompressorEffect.Ratio = ratio;
            }
        }

        private void UpdateCompressorThreshold()
        {
            var threshold = TrackBarWrapper.GetValue(trackBarCompressorThreshold);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateCompressor: threshold={threshold}");
            textBoxCompressorThreshold.Text = String.Format("{0:0.00}", threshold);
            if (mCompressorEffect != null)
            {
                mCompressorEffect.Threshold = threshold;
            }
        }

        private void UpdateCompressorPredelay()
        {
            var predelay = TrackBarWrapper.GetValue(trackBarCompressorPredelay);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateCompressor: predelay={predelay}");
            textBoxCompressorPredelay.Text = String.Format("{0:0.00}", predelay);
            if (mCompressorEffect != null)
            {
                mCompressorEffect.Predelay = predelay;
            }
        }

        private void UpdateCompressorAttack()
        {
            var attack = TrackBarWrapper.GetValue(trackBarCompressorAttack);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateCompressor: attack={attack}");
            textBoxCompressorAttack.Text = String.Format("{0:0.00}", attack);
            if (mCompressorEffect != null)
            {
                mCompressorEffect.Attack = attack;
            }
        }

        private void UpdateCompressorRelease()
        {
            var release = TrackBarWrapper.GetValue(trackBarCompressorRelease);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateCompressor: release={release}");
            textBoxCompressorRelease.Text = String.Format("{0:0.00}", release);
            if (mCompressorEffect != null)
            {
                mCompressorEffect.Release = release;
            }
        }

        private void UpdateCompressorGain()
        {
            var gain = TrackBarWrapper.GetValue(trackBarCompressorGain);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateCompressor: gain={gain}");
            textBoxCompressorGain.Text = String.Format("{0:0.00}", gain);
            if (mCompressorEffect != null)
            {
                mCompressorEffect.Gain = gain;
            }
        }

        //
        // Pitch
        //

        private void LoadPitch()
        {
            // https://github.com/filoe/cscore/blob/master/CSCore/Streams/Effects/PitchShifter.cs
            // Must be between 0.5 and 2.0, 1.0 default
            TrackBarWrapper.Wrap(trackBarPitch, 0.5f, 2, 1);
        }

        private void ResetPitch(bool hard = false)
        {
            if (hard)
            {
                checkBoxPitch.Checked = false;
            }
            TrackBarWrapper.SetValue(trackBarPitch, 1);
        }

        private void UpdatePitch()
        {
            Log.PrintLine(TAG, LogLevel.Information, $"UpdatePitch()");
            var pitch = TrackBarWrapper.GetValue(trackBarPitch);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdatePitch: pitch={pitch}");
            textBoxPitch.Text = String.Format("{0:0.00}", pitch);
            if (mPitchEffect != null)
            {
                mPitchEffect.PitchShiftFactor = IsPitchEnabled ? pitch : 1;
            }
        }

        //
        // Gain
        //

        private void LoadGain()
        {
            // https://github.com/filoe/cscore/blob/master/CSCore/Streams/GainSource.cs
            // Should be between 0.0 and 2.0, 1.0 default
            TrackBarWrapper.Wrap(trackBarGain, 0, 2, 1);
        }

        private void ResetGain(bool hard = false)
        {
            if (hard)
            {
                checkBoxGain.Checked = false;
            }
            TrackBarWrapper.SetValue(trackBarGain, 1);
        }

        private void UpdateGain()
        {
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateGain()");
            var gain = TrackBarWrapper.GetValue(trackBarGain);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateGain: gain={gain}");
            textBoxGain.Text = String.Format("{0:0.00}", gain);
            if (mGainSource != null)
            {
                mGainSource.Volume = IsGainEnabled ? gain : 1;
            }
        }

        //
        // Chorus
        //

        private void LoadChorus()
        {
        }

        private void ResetChorus(bool hard = false)
        {
            if (hard)
            {
                checkBoxChorus.Checked = false;
            }
            comboBoxChorusPhase.SelectedItem = "90";
            // TODO:(pv) Reset the rest of the Chorus parameters...
        }

        private void UpdateChorus()
        {
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateChorus()");
            // TODO: update the UI controls...
            if (mChorusEffect != null)
            {
                // https://github.com/filoe/cscore/blob/master/CSCore/Streams/Effects/DmoChorusEffect.cs
                mChorusEffect.IsEnabled = IsChorusEnabled;
                mChorusEffect.Delay = trackBarChorusDelay.Value; // default 16, range 0 to 20
                mChorusEffect.Depth = trackBarChorusDepth.Value; // default 10, range 0 to 100
                mChorusEffect.Feedback = trackBarChorusFeedback.Value; // default 25, range -99 to 99
                mChorusEffect.Frequency = trackBarChorusFrequency.Value / 10.0f; // default 1.1, range 0 to 10
                switch (comboBoxChorusPhase.SelectedItem)
                {
                    case "0":
                        mChorusEffect.Phase = ChorusPhase.PhaseZero;
                        break;
                    case "90":
                        mChorusEffect.Phase = ChorusPhase.Phase90; // default
                        break;
                    case "180":
                        mChorusEffect.Phase = ChorusPhase.Phase180;
                        break;
                    case "-180":
                        mChorusEffect.Phase = ChorusPhase.PhaseNegative180;
                        break;
                    case "-90":
                        mChorusEffect.Phase = ChorusPhase.PhaseNegative90;
                        break;
                }
                mChorusEffect.WetDryMix = trackBarChorusWetDryMix.Value; // default 50, range 0 to 100
            }
        }

        //
        // Reverb
        //

        private void LoadReverb()
        {
        }

        private void ResetReverb(bool hard = false)
        {
            if (hard)
            {
                checkBoxReverb.Checked = false;
            }
            // TODO:(pv) Reset the rest of the Reverb parameters...
        }

        private void UpdateReverb()
        {
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateReverb()");
            // TODO: update the UI controls...
            if (mReverbEffect != null)
            {
                // https://github.com/filoe/cscore/blob/master/CSCore/Streams/Effects/DmoWavesReverbEffect.cs
                mReverbEffect.IsEnabled = IsReverbEnabled;
                /*
                mReverbEffect.HighFrequencyRTRatio = 0.001f;
                mReverbEffect.InGain = 0;
                mReverbEffect.ReverbMix = 0;
                mReverbEffect.ReverbTime = 1000;
                */
            }
        }

        //
        // High Pass
        //

        private void LoadHighPass()
        {
            TrackBarWrapper.Wrap(trackBarHighPass, 20, 20000, 750);
        }

        private void ResetHighPass(bool hard = false)
        {
            if (hard)
            {
                checkBoxHighPass.Checked = false;
            }
            TrackBarWrapper.SetValue(trackBarHighPass, 750);
        }

        private void UpdateHighpass()
        {
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateHighpass()");
            var hz = TrackBarWrapper.GetValue(trackBarHighPass);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateHighpass hz={hz}");
            if (mAudioCaptureSource != null)
            {
                if (IsHighPassEnabled)
                {
                    if (mHighpassFilter == null)
                    {
                        mHighpassFilter = new HighpassFilter(mAudioCaptureSource.WaveFormat.SampleRate, hz);
                    }
                    else
                    {
                        mHighpassFilter.Frequency = hz;
                    }
                }
                else
                {
                    mHighpassFilter = null;
                }
                if (mBiQuadFilterSource != null)
                {
                    mBiQuadFilterSource.Filter = mHighpassFilter;
                }
            }
            textBoxHighPass.Text = String.Format("{0}", hz);
        }

        //
        // Distortion
        //

        private void LoadDistortion()
        {
            TrackBarWrapper.Wrap(trackBarDistortionGain, DmoDistortionEffect.GainMin, DmoDistortionEffect.GainMax, DmoDistortionEffect.GainDefault);
            TrackBarWrapper.Wrap(trackBarDistortionEdge, DmoDistortionEffect.EdgeMin, DmoDistortionEffect.EdgeMax, DmoDistortionEffect.EdgeDefault);
            TrackBarWrapper.Wrap(trackBarDistortionCenter, DmoDistortionEffect.PostEQCenterFrequencyMin, DmoDistortionEffect.PostEQCenterFrequencyMax, DmoDistortionEffect.PostEQCenterFrequencyDefault);
            TrackBarWrapper.Wrap(trackBarDistortionBandwidth, DmoDistortionEffect.PostEQBandwidthMin, DmoDistortionEffect.PostEQBandwidthMax, DmoDistortionEffect.PostEQBandwidthDefault);
            TrackBarWrapper.Wrap(trackBarDistortionLowPass, DmoDistortionEffect.PreLowPassCutoffMin, DmoDistortionEffect.PreLowPassCutoffMax, DmoDistortionEffect.PreLowPassCutoffDefault);
        }

        private void ResetDistortion(bool hard = false)
        {
            if (hard)
            {
                checkBoxDistortion.Checked = false;
            }
            ResetDistortionGain();
            ResetDistortionEdge();
            ResetDistortionCenter();
            ResetDistortionBandwidth();
            ResetDistortionLowPass();
        }

        private void ResetDistortionGain()
        {
            TrackBarWrapper.SetValue(trackBarDistortionGain, DmoDistortionEffect.GainDefault);
        }

        private void ResetDistortionEdge()
        {
            TrackBarWrapper.SetValue(trackBarDistortionEdge, DmoDistortionEffect.EdgeDefault);
        }

        private void ResetDistortionCenter()
        {
            TrackBarWrapper.SetValue(trackBarDistortionCenter, DmoDistortionEffect.PostEQCenterFrequencyDefault);
        }

        private void ResetDistortionBandwidth()
        {
            TrackBarWrapper.SetValue(trackBarDistortionBandwidth, DmoDistortionEffect.PostEQBandwidthDefault);
        }

        private void ResetDistortionLowPass()
        {
            TrackBarWrapper.SetValue(trackBarDistortionLowPass, DmoDistortionEffect.PreLowPassCutoffDefault);
        }

        private void UpdateDistortion()
        {
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateDistortion()");
            var isEnabled = IsDistortionEnabled;
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateDistortion: isEnabled={isEnabled}");
            if (mDistortionEffect != null)
            {
                mDistortionEffect.IsEnabled = isEnabled;
            }
            UpdateDistortionGain();
            UpdateDistortionEdge();
            UpdateDistortionCenter();
            UpdateDistortionBandwidth();
            UpdateDistortionLowPass();
        }

        private void UpdateDistortionGain()
        {
            var gain = TrackBarWrapper.GetValue(trackBarDistortionGain);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateDistortion: gain={gain}");
            textBoxDistortionGain.Text = String.Format("{0:0.00}", gain);
            if (mDistortionEffect != null)
            {
                mDistortionEffect.Gain = gain;
            }
        }

        private void UpdateDistortionEdge()
        {
            var edge = TrackBarWrapper.GetValue(trackBarDistortionEdge);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateDistortion: edge={edge}");
            textBoxDistortionEdge.Text = String.Format("{0:0.00}", edge);
            if (mDistortionEffect != null)
            {
                mDistortionEffect.Edge = edge;
            }
        }

        private void UpdateDistortionCenter()
        {
            var center = TrackBarWrapper.GetValue(trackBarDistortionCenter);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateDistortion: center={center}");
            textBoxDistortionCenter.Text = String.Format("{0:0.00}", center);
            if (mDistortionEffect != null)
            {
                mDistortionEffect.PostEQCenterFrequency = center;
            }
        }

        private void UpdateDistortionBandwidth()
        {
            var bandwidth = TrackBarWrapper.GetValue(trackBarDistortionBandwidth);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateDistortion: bandwidth={bandwidth}");
            textBoxDistortionBandwidth.Text = String.Format("{0:0.00}", bandwidth);
            if (mDistortionEffect != null)
            {
                mDistortionEffect.PostEQBandwidth = bandwidth;
            }
        }

        private void UpdateDistortionLowPass()
        {
            var lowpass = TrackBarWrapper.GetValue(trackBarDistortionLowPass);
            Log.PrintLine(TAG, LogLevel.Information, $"UpdateDistortion: lowpass={lowpass}");
            textBoxDistortionLowPass.Text = String.Format("{0:0.00}", lowpass);
            if (mDistortionEffect != null)
            {
                mDistortionEffect.PreLowpassCutoff = lowpass;
            }
        }
    }
}
