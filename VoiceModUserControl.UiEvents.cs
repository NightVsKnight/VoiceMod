using System;
using System.Windows.Forms;
using static NvkCommon.Log;
using NvkCommon;

namespace VoiceMod
{
    partial class VoiceModUserControl
    {
        //
        // Capture/Render
        //

        private void checkBoxEnabled_CheckedChanged(object sender, EventArgs e)
        {
            IsAudioCaptureEnabled = checkBoxEnabled.Checked;
        }

        private void labelCapture_DoubleClick(object sender, EventArgs e)
        {
            Log.PrintLine(TAG, LogLevel.Information, "labelCapture_DoubleClick");
            SelectedCaptureDeviceId = null;
            AudioDevicesUpdate("labelCapture_DoubleClick");
        }

        private void comboBoxCapture_SelectedValueChanged(object sender, EventArgs e)
        {
            if (audioDevicesUpdating) return;
            //Log.PrintLine(TAG, LogLevel.Information, "ComboBoxCapture_SelectedValueChanged(...)");
            var selectedDeviceWrapper = comboBoxCapture.SelectedItem as DeviceWrapper;
            var deviceId = selectedDeviceWrapper?.Device?.DeviceID;
            Log.PrintLine(TAG, LogLevel.Information, $"comboBoxCapture_SelectedValueChanged: SelectedCaptureDeviceId={deviceId}");
            SelectedCaptureDeviceId = deviceId;
        }

        private void labelRender_DoubleClick(object sender, EventArgs e)
        {
            Log.PrintLine(TAG, LogLevel.Information, "labelRender_DoubleClick");
            SelectedRenderDeviceId = null;
            AudioDevicesUpdate("labelRender_DoubleClick");
        }

        private void comboBoxRender_SelectedValueChanged(object sender, EventArgs e)
        {
            if (audioDevicesUpdating) return;
            //Log.PrintLine(TAG, LogLevel.Information, "ComboBoxRender_SelectedValueChanged(...)");
            var selectedDeviceWrapper = comboBoxRender.SelectedItem as DeviceWrapper;
            var deviceId = selectedDeviceWrapper?.Device?.DeviceID;
            Log.PrintLine(TAG, LogLevel.Information, $"comboBoxRender_SelectedValueChanged: SelectedCaptureDeviceId={deviceId}");
            SelectedRenderDeviceId = deviceId;
        }

        //
        // Effects
        //

        private void checkBoxEffectsEnabled_CheckedChanged(object sender, EventArgs e)
        {
            IsAudioEffectsEnabled = checkBoxEffectsEnabled.Checked;
        }

        private void GroupExpandCollapseToggle(GroupBox groupBox, Button button)
        {
            button.Text = button.Text == "^" ? "v" : "^";
            var expand = button.Text == "^";
            if (expand)
            {
                groupBox.Height = (int)groupBox.Tag;
                panelEffects.ScrollControlIntoView(groupBox);
            }
            else
            {
                groupBox.Height = button.Height + 2;
            }
        }

        //
        // Noise Suppression
        //

        private void buttonNoiseSuppression_Click(object sender, EventArgs e)
        {
            GroupExpandCollapseToggle(groupBoxNoiseSuppression, buttonNoiseSuppression);
        }

        private void checkBoxNoiseSuppression_CheckedChanged(object sender, EventArgs e)
        {
            UpdateNoiseSuppression();
        }

        private void checkBoxNoiseSuppression_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetNoiseSuppression();
            }
        }

        private void labelNoiseSuppressionIntensity_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetNoiseSuppression();
            }
        }

        private void trackBarNoiseSuppressionIntensity_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetNoiseSuppression();
            }
        }

        private void trackBarNoiseSuppressionIntensity_ValueChanged(object sender, EventArgs e)
        {
            UpdateNoiseSuppressionIntensity();
        }

        //
        // Compressor
        //

        private void buttonCompressor_Click(object sender, EventArgs e)
        {
            GroupExpandCollapseToggle(groupBoxCompressor, buttonCompressor);
        }

        private void checkBoxCompressor_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCompressor();
        }

        private void checkBoxCompressor_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetCompressor();
            }
        }

        private void labelCompressorRatio_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetCompressorRatio();
            }
        }

        private void trackBarCompressorRatio_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetCompressorRatio();
            }
        }

        private void trackBarCompressorRatio_ValueChanged(object sender, EventArgs e)
        {
            UpdateCompressorRatio();
        }

        private void labelCompressorThreshold_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetCompressorThreshold();
            }
        }

        private void trackBarCompressorThreshold_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetCompressorThreshold();
            }
        }

        private void trackBarCompressorThreshold_ValueChanged(object sender, EventArgs e)
        {
            UpdateCompressorThreshold();
        }

        private void labelCompressorPredelay_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetCompressorPredelay();
            }
        }

        private void trackBarCompressorPredelay_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetCompressorPredelay();
            }
        }

        private void trackBarCompressorPredelay_ValueChanged(object sender, EventArgs e)
        {
            UpdateCompressorPredelay();
        }

        private void labelCompressorAttack_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetCompressorAttack();
            }
        }

        private void trackBarCompressorAttack_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetCompressorAttack();
            }
        }

        private void trackBarCompressorAttack_ValueChanged(object sender, EventArgs e)
        {
            UpdateCompressorAttack();
        }

        private void labelCompressorRelease_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetCompressorRelease();
            }
        }

        private void trackBarCompressorRelease_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetCompressorRelease();
            }
        }

        private void trackBarCompressorRelease_ValueChanged(object sender, EventArgs e)
        {
            UpdateCompressorRelease();
        }

        private void labelCompressorGain_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetCompressorGain();
            }
        }

        private void trackBarCompressorGain_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetCompressorGain();
            }
        }

        private void trackBarCompressorGain_ValueChanged(object sender, EventArgs e)
        {
            UpdateCompressorGain();
        }

        //
        // Pitch
        //

        private void checkBoxPitch_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePitch();
        }

        private void checkBoxPitch_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetPitch();
            }
        }

        private void trackBarPitch_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetPitch();
            }
        }

        private void trackBarPitch_ValueChanged(object sender, EventArgs e)
        {
            UpdatePitch();
        }

        //
        // Gain
        //

        private void checkBoxGain_CheckedChanged(object sender, EventArgs e)
        {
            UpdateGain();
        }

        private void checkBoxGain_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetGain();
            }
        }

        private void trackBarGain_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetGain();
            }
        }

        private void trackBarGain_ValueChanged(object sender, EventArgs e)
        {
            UpdateGain();
        }

        //
        // Chorus
        //

        private void buttonChorus_Click(object sender, EventArgs e)
        {
            GroupExpandCollapseToggle(groupBoxChorus, buttonChorus);
        }

        private void checkBoxChorus_CheckedChanged(object sender, EventArgs e)
        {
            UpdateChorus();
        }

        private void checkBoxChorus_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetChorus();
            }
        }

        private void labelChorusDelay_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarChorusDelay_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarChorusDelay_ValueChanged(object sender, EventArgs e)
        {
            UpdateChorus();
        }

        private void labelChorusDepth_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarChorusDepth_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarChorusDepth_ValueChanged(object sender, EventArgs e)
        {
            UpdateChorus();
        }

        private void labelChorusFeedback_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarChorusFeedback_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarChorusFeedback_ValueChanged(object sender, EventArgs e)
        {
            UpdateChorus();
        }

        private void labelChorusFrequency_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarChorusFrequency_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarChorusFrequency_ValueChanged(object sender, EventArgs e)
        {
            UpdateChorus();
        }

        private void labelChorusWaveform_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void comboBoxChorusWaveform_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void labelChorusPhase_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void comboBoxChorusPhase_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void labelChorusWetDryMix_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarChorusWetDryMix_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarChorusWetDryMix_ValueChanged(object sender, EventArgs e)
        {
            UpdateChorus();
        }

        //
        // Reverb
        //

        private void buttonReverb_Click(object sender, EventArgs e)
        {
            GroupExpandCollapseToggle(groupBoxReverb, buttonReverb);
        }

        private void checkBoxReverb_CheckedChanged(object sender, EventArgs e)
        {
            UpdateReverb();
        }

        private void checkBoxReverb_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetReverb();
            }
        }

        private void labelReverbHighFrequencyRtRatio_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarReverbHighFrequencyRtRatio_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void labelReverbInputGain_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarReverbInputGain_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void labelReverbMix_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarReverbMix_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void labelReverbTime_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void trackBarReverbTime_MouseUp(object sender, MouseEventArgs e)
        {

        }

        //
        // High Pass
        //

        private void checkBoxHighPass_CheckedChanged(object sender, EventArgs e)
        {
            UpdateHighpass();
        }

        private void checkBoxHighPass_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetHighPass();
            }
        }

        private void trackBarHighPass_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetHighPass();
            }
        }

        private void trackBarHighPass_ValueChanged(object sender, EventArgs e)
        {
            UpdateHighpass();
        }

        //
        // Distortion
        //

        private void buttonDistortion_Click(object sender, EventArgs e)
        {
            GroupExpandCollapseToggle(groupBoxDistortion, buttonDistortion);
        }

        private void checkBoxDistortion_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDistortion();
        }

        private void checkBoxDistortion_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetDistortion();
            }
        }

        private void labelDistortionGain_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetDistortionGain();
            }
        }

        private void trackBarDistortionGain_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetDistortionGain();
            }
        }

        private void trackBarDistortionGain_ValueChanged(object sender, EventArgs e)
        {
            UpdateDistortionGain();
        }

        private void labelDistortionEdge_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetDistortionEdge();
            }
        }

        private void trackBarDistortionEdge_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetDistortionEdge();
            }
        }

        private void trackBarDistortionEdge_ValueChanged(object sender, EventArgs e)
        {
            UpdateDistortionEdge();
        }

        private void labelDistortionCenter_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetDistortionCenter();
            }
        }

        private void trackBarDistortionCenter_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetDistortionCenter();
            }
        }

        private void trackBarDistortionCenter_ValueChanged(object sender, EventArgs e)
        {
            UpdateDistortionCenter();
        }

        private void labelDistortionBandwidth_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetDistortionBandwidth();
            }
        }

        private void trackBarDistortionBandwidth_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetDistortionBandwidth();
            }
        }

        private void trackBarDistortionBandwidth_ValueChanged(object sender, EventArgs e)
        {
            UpdateDistortionBandwidth();
        }

        private void labelDistortionLowPass_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetDistortionLowPass();
            }
        }

        private void trackBarDistortionLowPass_MouseUp(object sender, MouseEventArgs e)
        {
            if (Utils.IsMouseRightClicked(e))
            {
                ResetDistortionLowPass();
            }
        }

        private void trackBarDistortionLowPass_ValueChanged(object sender, EventArgs e)
        {
            UpdateDistortionLowPass();
        }
    }
}
