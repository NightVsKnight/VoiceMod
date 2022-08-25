using NvkCommon;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.SoundOut;
using CSCore.Streams;
using CSCore.Streams.Effects;
using CSCore.Win32;
using System;
using System.Windows.Forms;
using static NvkCommon.Log;
using System.ComponentModel;
using static NvAfxDotNet.NVAudioEffectsDLL;

namespace VoiceMod
{
    partial class VoiceModUserControl
    {
        private WasapiCapture mAudioCapture;
        private IWaveSource mAudioCaptureSource;
        private WasapiOut mAudioRender;
        private NvAfxDotNet.NvAfxDenoiserFilter mNoiseSuppression;
        private DmoCompressorEffect mCompressorEffect;
        private PitchShifter mPitchEffect;
        private GainSource mGainSource;
        private DmoChorusEffect mChorusEffect;
        private DmoWavesReverbEffect mReverbEffect;
        private HighpassFilter mHighpassFilter;
        private BiQuadFilterSource mBiQuadFilterSource;
        private DmoDistortionEffect mDistortionEffect;

        public class BiQuadFilterSource : SampleAggregatorBase
        {
            private readonly object _lockObject = new object();
            private BiQuad _filter;

            public BiQuad Filter
            {
                get
                {
                    lock (_lockObject)
                    {
                        return _filter;
                    }
                }
                set
                {
                    lock (_lockObject)
                    {
                        _filter = value;
                    }
                }
            }

            public BiQuadFilterSource(ISampleSource source) : base(source) { }

            private BiQuad _temp = null;

            public override int Read(float[] buffer, int offset, int count)
            {
                var read = base.Read(buffer, offset, count);
                _temp = Filter;
                if (_temp != null)
                {
                    for (int i = 0; i < read; ++i)
                    {
                        buffer[i + offset] = _temp.Process(buffer[i + offset]);
                    }
                }
                return read;
            }
        }

        //
        //
        //

        private AudioDeviceManager audioDeviceManager = new AudioDeviceManager();

        private void AudioLoad()
        {
            audioDeviceManager.OnAudioDeviceUpdated += AudioDeviceManager_OnAudioDeviceUpdated;
        }

        private void AudioUnload()
        {
            audioDeviceManager.OnAudioDeviceUpdated -= AudioDeviceManager_OnAudioDeviceUpdated;
            AudioCaptureStop("OnHandleDestroyed");
            BufferWrapper.Clear();
        }

        private MMDevice TryGetAudioDevice(string deviceId, DataFlow dataFlow)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                MMDevice device;
                try
                {
                    device = AudioDeviceManager.DeviceEnumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia);
                }
                catch (CoreAudioAPIException)
                {
                    device = null;
                }
                deviceId = device?.DeviceID;
            }
            //Log.PrintLine(TAG, LogLevel.Information, "TryGetAudioDevice get deviceId={0}", deviceId);
            try
            {
                return deviceId != null ? AudioDeviceManager.DeviceEnumerator[deviceId] : null;
            }
            catch (CoreAudioAPIException)
            {
                return null;
            }
        }

        private string SelectedCaptureDeviceId
        {
            get
            {
                var deviceId = settings.SelectedCaptureDeviceId;
                Log.PrintLine(TAG, LogLevel.Information, $"SelectedCaptureDeviceId get deviceId={deviceId}");
                return deviceId;
            }
            set
            {
                AudioCaptureStop("SelectedCaptureDeviceId set");

                var deviceId = value;
                Log.PrintLine(TAG, LogLevel.Information, $"SelectedCaptureDeviceId set deviceId={deviceId}");
                settings.SelectedCaptureDeviceId = deviceId;
                settings.Save();

                AudioDevicesUpdate("SelectedCaptureDeviceId set");
            }
        }

        private MMDevice SelectedCaptureDevice
        {
            get
            {
                var deviceId = SelectedCaptureDeviceId;
                Log.PrintLine(TAG, LogLevel.Information, $"SelectedCaptureDevice get deviceId={deviceId}");
                MMDevice device = TryGetAudioDevice(deviceId, DataFlow.Capture);
                return device;
            }
        }

        private string SelectedRenderDeviceId
        {
            get
            {
                var deviceId = settings.SelectedRenderDeviceId;
                Log.PrintLine(TAG, LogLevel.Information, $"SelectedRenderDeviceId get deviceId={deviceId}");
                return deviceId;
            }
            set
            {
                AudioCaptureStop("SelectedRenderDeviceId set");

                var deviceId = value;
                Log.PrintLine(TAG, LogLevel.Information, $"SelectedRenderDeviceId set deviceId={deviceId}");
                settings.SelectedRenderDeviceId = deviceId;
                settings.Save();

                AudioDevicesUpdate("SelectedRenderDeviceId set");
            }
        }

        private MMDevice SelectedRenderDevice
        {
            get
            {
                var deviceId = SelectedRenderDeviceId;
                Log.PrintLine(TAG, LogLevel.Information, "SelectedRenderDevice get deviceId={0}", deviceId);
                MMDevice device = TryGetAudioDevice(deviceId, DataFlow.Render);
                return device;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Bindable(false)]
        [Browsable(false)]
        public bool IsAudioCaptureEnabled
        {
            get
            {
                return settings.IsAudioCaptureEnabled;
            }
            private set
            {
                checkBoxEnabled.Checked = value;

                settings.IsAudioCaptureEnabled = value;
                settings.Save();

                if (IsAudioCaptureEnabled)
                {
                    AudioCaptureStart("IsAudioCaptureEnabled set");
                }
                else
                {
                    AudioCaptureStop("IsAudioCaptureEnabled set");
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Bindable(false)]
        [Browsable(false)]
        public bool IsActivelyCapturing { get { return mAudioCapture != null; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Bindable(false)]
        [Browsable(false)]
        public bool IsSelectedCaptureDeviceShowing { get { return comboBoxCapture.SelectedItem != null; } }

        public bool IsAudioEffectsEnabled
        {
            get
            {
                return checkBoxEffectsEnabled.Checked;
            }
            set
            {
                var wasActivelyCapturing = IsActivelyCapturing;
                if (wasActivelyCapturing)
                {
                    AudioCaptureStop("IsAudioEffectsEnabled set");
                }

                checkBoxEffectsEnabled.Checked = value;

                if (wasActivelyCapturing)
                {
                    AudioCaptureStart("IsAudioEffectsEnabled set");
                }
            }
        }

        public bool IsCompressorEnabled
        {
            get
            {
                return checkBoxCompressor.Checked;
            }
        }

        public bool IsPitchEnabled
        {
            get
            {
                return checkBoxPitch.Checked;
            }
        }

        public bool IsGainEnabled
        {
            get
            {
                return checkBoxGain.Checked;
            }
        }

        public bool IsChorusEnabled
        {
            get
            {
                return checkBoxChorus.Checked;
            }
        }

        public bool IsReverbEnabled
        {
            get
            {
                return checkBoxReverb.Checked;
            }
        }

        public bool IsHighPassEnabled
        {
            get
            {
                return checkBoxHighPass.Checked;
            }
        }

        public bool IsDistortionEnabled
        {
            get
            {
                return checkBoxDistortion.Checked;
            }
        }

        private void AudioDeviceManager_OnAudioDeviceUpdated(object sender, AudioDeviceManager.DeviceUpdatedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate { AudioDeviceManager_OnAudioDeviceUpdated(sender, e); });
                return;
            }
            Log.PrintLine(TAG, LogLevel.Information, $"AudioDeviceManager_OnAudioDeviceUpdated {e}");
            AudioDevicesUpdate("AudioDeviceManager_OnAudioDevicesUpdated");
        }

        /// <summary>
        /// Gate to prevent comboBoxCapture_SelectedValueChanged and comboBoxRender_SelectedValueChanged
        /// </summary>
        private bool audioDevicesUpdating;

        private void AudioDevicesUpdate(string caller, bool autoStart = true)
        {
            audioDevicesUpdating = true;

            Log.PrintLine(TAG, LogLevel.Information, $"AudioDevicesUpdate({Utils.Quote(caller)})");

            DeviceWrapper.AudioDevicesUpdate(
                AudioDeviceManager.DeviceEnumerator.EnumAudioEndpoints(DataFlow.Capture, DeviceState.Active),
                comboBoxCapture,
                SelectedCaptureDevice);

            DeviceWrapper.AudioDevicesUpdate(
                AudioDeviceManager.DeviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active),
                comboBoxRender,
                SelectedRenderDevice);

            audioDevicesUpdating = false;

            if (autoStart && IsAudioCaptureEnabled && IsSelectedCaptureDeviceShowing)
            {
                if (!IsActivelyCapturing)
                {
                    Log.PrintLine(TAG, LogLevel.Information, "AudioDevicesUpdate: IsSelectedCaptureDeviceShowing && !IsActivelyCapturing; AudioCaptureStart();");
                    AudioCaptureStart("AudioDevicesUpdate");
                }
            }
            else
            {
                if (IsActivelyCapturing)
                {
                    Log.PrintLine(TAG, LogLevel.Information, "AudioDevicesUpdate: !IsSelectedCaptureDeviceShowing && IsActivelyCapturing; AudioCaptureStop();");
                    AudioCaptureStop("AudioDevicesUpdate");
                }
            }
        }

        public bool AudioCaptureStart(string caller)
        {
            Log.PrintLine(TAG, LogLevel.Information, $"AudioCaptureStart({Utils.Quote(caller)})");
            try
            {
                if (!IsAudioCaptureEnabled || IsActivelyCapturing)
                {
                    return false;
                }

                var deviceCapture = SelectedCaptureDevice;
                var deviceRender = SelectedRenderDevice;
                if (deviceCapture == null || deviceRender == null)
                {
                    var msg = @"Voice Morphing 'Capture' *AND* 'Render' devices must *BOTH* not be null";
                    MessageBox.Show(msg);
                    return false;
                }

                var eventSync = false;
                var shareMode = AudioClientShareMode.Shared;
                var latency = 100;

                mAudioCapture = new WasapiCapture(eventSync, shareMode, latency)
                {
                    Device = deviceCapture
                };
                mAudioCapture.Initialize();

                mAudioCaptureSource = new SoundInSource(mAudioCapture) { FillWithZeros = true };

                if (IsAudioEffectsEnabled)
                {
                    mNoiseSuppression = new NvAfxDotNet.NvAfxDenoiserFilter(mAudioCaptureSource.ToSampleSource());
                    mNoiseSuppression.OnEnabledChanged += NoiseSuppression_OnEnabledChanged;
                    UpdateNoiseSuppression();
                    mAudioCaptureSource = mNoiseSuppression.ToWaveSource();

                    mCompressorEffect = new DmoCompressorEffect(mAudioCaptureSource);
                    UpdateCompressor();
                    mAudioCaptureSource = mCompressorEffect;

                    mPitchEffect = new PitchShifter(mAudioCaptureSource.ToSampleSource());
                    UpdatePitch();
                    mAudioCaptureSource = mPitchEffect.ToWaveSource();

                    mGainSource = new GainSource(mAudioCaptureSource.ToSampleSource());
                    UpdateGain();
                    mAudioCaptureSource = mGainSource.ToWaveSource();

                    // TODO:(pv) Equalizer.Create10BandEqualizer
                    // TODO:(pv) DmoEchoEffect
                    // TODO:(pv) DmoFlangerEfect
                    // TODO:(pv) DmoGargleEffect

                    mChorusEffect = new DmoChorusEffect(mAudioCaptureSource);
                    UpdateChorus();
                    mAudioCaptureSource = mChorusEffect;

                    mReverbEffect = new DmoWavesReverbEffect(mAudioCaptureSource);
                    UpdateReverb();
                    mAudioCaptureSource = mReverbEffect;

                    mBiQuadFilterSource = mAudioCaptureSource.ToSampleSource().AppendSource(x => new BiQuadFilterSource(x));
                    UpdateHighpass();
                    mAudioCaptureSource = mBiQuadFilterSource.ToWaveSource();

                    mDistortionEffect = new DmoDistortionEffect(mAudioCaptureSource);
                    UpdateDistortion();
                    mAudioCaptureSource = mDistortionEffect;
                }

                mAudioRender = new WasapiOut(eventSync, shareMode, latency)
                {
                    Device = deviceRender
                };
                // 0x8889000A "Device In Use"
                mAudioRender.Initialize(mAudioCaptureSource);
                mAudioRender.Stopped += OnAudioRender_Stopped;
                mAudioRender.Play();

                mAudioCapture.Stopped += OnAudioCapture_Stopped;
                mAudioCapture.Start();

                //OnAudioCaptureStarted?.Invoke(this, null);
            }
            catch (Exception ex)
            {
                AudioCaptureStop($"AudioCaptureStart({Utils.Quote(caller)})");
                if (true)
                {
                    var msg = $"Error in AudioCaptureStart: \r\n{ex.Message}";
                    MessageBox.Show(msg);
                    Log.PrintLine(TAG, LogLevel.Information, msg);
                }
                return false;
            }

            return true;
        }

        private bool AudioCaptureStop(string caller)
        {
            Log.PrintLine(TAG, LogLevel.Information, $"AudioCaptureStop({Utils.Quote(caller)})");
            if (!IsActivelyCapturing) return false;
            if (mAudioCapture != null)
            {
                mAudioCapture.Stop();
                mAudioCapture.Dispose();
                mAudioCapture = null;
            }
            if (mAudioCaptureSource != null)
            {
                mAudioCaptureSource.Dispose();
                mAudioCaptureSource = null;
            }
            if (mAudioRender != null)
            {
                mAudioRender.Stop();
                mAudioRender.Dispose();
                mAudioRender = null;
            }
            if (mNoiseSuppression != null)
            {
                mNoiseSuppression.OnEnabledChanged -= NoiseSuppression_OnEnabledChanged;
                mNoiseSuppression.Dispose();
                mNoiseSuppression = null;
            }
            if (mCompressorEffect != null)
            {
                mCompressorEffect.Dispose();
                mCompressorEffect = null;
            }
            if (mPitchEffect != null)
            {
                mPitchEffect.Dispose();
                mPitchEffect = null;
            }
            if (mGainSource != null)
            {
                mGainSource.Dispose();
                mGainSource = null;
            }
            if (mChorusEffect != null)
            {
                mChorusEffect.Dispose();
                mChorusEffect = null;
            }
            if (mReverbEffect != null)
            {
                mReverbEffect.Dispose();
                mReverbEffect = null;
            }
            if (mBiQuadFilterSource != null)
            {
                mBiQuadFilterSource.Dispose();
                mBiQuadFilterSource = null;
            }
            if (mHighpassFilter != null)
            {
                mHighpassFilter = null;
            }
            if (mDistortionEffect != null)
            {
                mDistortionEffect.Dispose();
                mDistortionEffect = null;
            }
            //OnAudioCaptureStopped?.Invoke(this, null);
            return true;
        }

        private void OnAudioRender_Stopped(object sender, PlaybackStoppedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate { OnAudioRender_Stopped(sender, e); });
                return;
            }
            if (e.HasError)
            {
                var exception = e.Exception;
                if (exception.HResult == (int)HResult.E_POINTER && exception.TargetSite.Name == "get_WaveFormat")
                {
                    // NullReferenceException: Object reference not set to an instance of an object.
                    //  at CSCore.SampleAggregatorBase.get_WaveFormat()
                    //  at CSCore.SampleAggregatorBase.Read(Single[] buffer, Int32 offset, Int32 count)
                    //  ...
                    if (IsAudioCaptureEnabled && IsActivelyCapturing)
                    {
                        Log.PrintLine(TAG, LogLevel.Information, $"OnAudioRender_Stopped(...): Restarting capture as workaround for get_WaveFormat() E_POINTER error");
                        AudioCaptureStop("OnAudioRender_Stopped");
                        AudioCaptureStart("OnAudioRender_Stopped");
                    }
                }
                else
                {
                    Log.PrintLine(TAG, LogLevel.Error, $"OnAudioRender_Stopped(...): e={e}");
                }
            }
        }

        private void OnAudioCapture_Stopped(object sender, RecordingStoppedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate { OnAudioCapture_Stopped(sender, e); });
                return;
            }

            if (e.HasError)
            {
                var exception = e.Exception;
                if (exception.HResult == (int)HResult.E_POINTER && exception.TargetSite.Name == "get_WaveFormat")
                {
                    // NullReferenceException: Object reference not set to an instance of an object.
                    //  at CSCore.SampleAggregatorBase.get_WaveFormat()
                    //  at CSCore.SampleAggregatorBase.Read(Single[] buffer, Int32 offset, Int32 count)
                    //  ...
                    if (IsAudioCaptureEnabled && IsActivelyCapturing)
                    {
                        Log.PrintLine(TAG, LogLevel.Information, $"OnAudioCapture_Stopped(...): Restarting capture as workaround for get_WaveFormat() E_POINTER error");
                        AudioCaptureStop("OnAudioCapture_Stopped");
                        AudioCaptureStart("OnAudioCapture_Stopped");
                    }
                }
                else
                {
                    Log.PrintLine(TAG, LogLevel.Error, $"OnAudioCapture_Stopped(...): e={e}");
                }
            }
        }
    }
}
