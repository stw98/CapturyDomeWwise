using System.Collections.Generic;
using OpusSharp.Core;
using Riptide;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

[RequireComponent(typeof(Player))]
public class Voice : MonoBehaviour, IMultiplayObject
{
    public ushort Id { get; set; }
    public string Name { get; set; }
    public bool IsMicPlayer { get; internal set; } 

    /// <summary>
    /// Player
    /// </summary>
    private Player player;

    /// <summary>
    /// Audio Source
    /// </summary>
    private MicAudioSource micAudioSource;
    private StreamedAudioSource streamedAudioSource;

    /// <summary>
    /// Opus encoder and decoder
    /// </summary>
    private OpusEncoder encoder;
    private OpusDecoder decoder;

    /// <summary>
    /// VOIP chatroom list, similar to Playerlist
    /// </summary>
    public static Dictionary<ushort, Voice> voiceList = new Dictionary<ushort, Voice>();

    /// <summary>
    /// Audio Buffer
    /// </summary>
    private CircularBuffer<VoipPacket> voipBuffer = new CircularBuffer<VoipPacket>(32, 4);

    void OnEnable()
    {
        player = GetComponent<Player>();
        NetworkManager.Singleton.Client.Connection.NotifyReceived += OnReceivedAudioPacket;
        player.OnInitMicInfo += OnInitMicInfo;
    }

    void OnDisable()
    {
        NetworkManager.Singleton.Client.Connection.NotifyReceived -= OnReceivedAudioPacket;
        player.OnInitMicInfo -= OnInitMicInfo;
    }

    public void OnInitMicInfo()
    {
        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            return;
        InitVoice();
        #else
        InitVoice();
        #endif
    }

    void OnDestroy()
    {
        voiceList.Remove(Id);
    }

    void Update()
    {
        if (decoder != null)
        {
            while (voipBuffer.IsReady)
            {
                var packet = voipBuffer.Read();
                float[] decoded = new float[ packet.Data.Length ];
                decoder.Decode(packet.Data, packet.EncodedBytes, decoded, packet.Data.Length, false);
                streamedAudioSource.Feed(packet.Frequency, packet.Channels, decoded);
            }
        }
    }

    void InitVoice()
    {
        if (Mic.AvailableDevices.Count > 0 && IsMicPlayer)
        {
            Mic.Init();
            micAudioSource = gameObject.AddComponent<MicAudioSource>();
            micAudioSource.Device = Mic.AvailableDevices[UIManager.Singleton.SelectedDevice];
            micAudioSource.Device.StartRecording();
            streamedAudioSource = micAudioSource.StreamedAudioSource;
        }
        else
        {
            if (streamedAudioSource == null)
            {
                streamedAudioSource = StreamedAudioSource.New(gameObject);
                streamedAudioSource.Play();
            }
        }
    }

    public void InitEncoder(int SamplingFrequency, int ChannelCount, OpusPredefinedValues Application)
    {
        encoder = new OpusEncoder(SamplingFrequency, ChannelCount, Application);
    }

    public void InitDecoder(int frequency, int channels)
    {
        Debug.Log($"Decoder frequency is {frequency} with {channels} channel(s)");
        decoder = new OpusDecoder(frequency, channels);
    }

    public void SendAudioPacket(int frameSequence, int frequency, int channels, int encodedBytes, byte[] encoded)
    {
        Message message = Message.Create(MessageSendMode.Notify);
        message.AddUShort(Id);
        message.AddInt(frameSequence);
        message.AddInt(frequency);
        message.AddInt(channels);
        message.AddInt(encodedBytes);
        message.AddBytes(encoded);
        NetworkManager.Singleton.Client.Send(message);
    }

    public int EncodeAudioPacket(float[] samples, byte[] encoded)
    {
        int encodedBytes = encoder.Encode(samples, samples.Length, encoded, samples.Length);
        return encodedBytes;
    }

    #region Message
    int lastSequenceId;
    void HandleReceivedAudioPacket(int sequenceId, int frequency, int channels, int encodedBytes, byte[] packet)
    {
        if (decoder == null)
            InitDecoder(frequency, channels);
        
        if (sequenceId == lastSequenceId)
        {
            if (frequency > 44100)
                return;
        }
        if (sequenceId < lastSequenceId)
            return;
        
        VoipPacket data = new VoipPacket(frequency, channels, encodedBytes, packet);
        voipBuffer.Write(data);
        
        lastSequenceId = sequenceId;
    }

    static void OnReceivedAudioPacket(Message message)
    {
        if (message.UnreadBits < 1280)
        {
            message.Release();
            return;
        }
        if (voiceList.TryGetValue(message.GetUShort(), out Voice voice))
            voice.HandleReceivedAudioPacket(message.GetInt(), message.GetInt(), message.GetInt(), message.GetInt(), message.GetBytes());
    }
    #endregion
}
