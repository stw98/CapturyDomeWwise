using System.Collections;
using System.Collections.Generic;
using OpusSharp.Core;
using UnityEngine;

[RequireComponent(typeof(StreamedAudioSource))]
public class MicAudioSource : MonoBehaviour
{
    [SerializeField] Mic.Device device;

    public Mic.Device Device
    {
        get => device;
        set {
            if (device != null) {
                device.OnStartRecording -= OnStartRecording;
                device.OnFrameCollected -= OnFrameCollected;
                device.OnStopRecording -= OnStopRecording;
                Debug.Log("Device removed from MicAudioSource", gameObject);
            }
            if(value != null) {
                device = value;
                device.OnStartRecording += OnStartRecording;
                device.OnFrameCollected += OnFrameCollected;
                device.OnStopRecording += OnStopRecording;
                if (device.IsRecording)
                    StreamedAudioSource.Play();
                else
                    StreamedAudioSource.Stop();
                Debug.Log("MicAudioSource shifted to " + device.Name, gameObject);
            }
            else
                StreamedAudioSource.Stop();
        }
    }

    void OnDestroy()
    {
        device.OnStartRecording -= OnStartRecording;
        device.OnFrameCollected -= OnFrameCollected;
        device.OnStopRecording -= OnStopRecording;

        device.StopRecording();
    }

    StreamedAudioSource streamedAudioSource;
    public StreamedAudioSource StreamedAudioSource
    {
        get {
            if (streamedAudioSource == null)
                streamedAudioSource = gameObject.GetComponent<StreamedAudioSource>();
            return streamedAudioSource;
        }
    }

    Voice networkVoice;
    public Voice NetworkVoice
    {
        get
        {
            if (networkVoice == null)
                networkVoice = gameObject.GetComponent<Voice>();
            return networkVoice;
        }
    }
    
    void OnStartRecording()
    {
        StreamedAudioSource.Play();
        NetworkVoice.InitEncoder(Device.SamplingFrequency, Device.ChannelCount, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
    }

    void OnFrameCollected(int frameSequence, int frequency, int channels, float[] samples)
    {
        byte[] encoded = new byte[samples.Length];
        int encodedBytes = NetworkVoice.EncodeAudioPacket(samples, encoded);

        NetworkVoice.SendAudioPacket(frameSequence, frequency, channels, encodedBytes, encoded);
    }

    void OnStopRecording()
    {
        StreamedAudioSource.Stop();
    }
}
