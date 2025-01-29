using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Riptide;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkVoice : MonoBehaviour
{
    /// <summary>
    /// Voip voice list, similar to player list
    /// </summary>
    public static Dictionary<ushort, NetworkVoice> voiceList = new Dictionary<ushort, NetworkVoice>();

    public ushort Id { get; internal set; }
    Connection client;
    
    /// <summary>
    /// VOIP buffer
    /// </summary>
    public int delayBlockSize = 2;
    //VoipBuffer voipBuffer;
    CircularBuffer<VoipPacket> voipBuffer;

    /// <summary>
    /// ErrorLog
    /// </summary>
    public Action<string> SetErrorLog = delegate { };

    void OnDestroy()
    {
        voiceList.Remove(Id);
        client.NotifyReceived -= OnNotifyReceived;
    }

    void Start()
    {
        voipBuffer = new CircularBuffer<VoipPacket>(32, delayBlockSize);
    }

    async void Update()
    {
        while (voipBuffer.IsReady)
        {
            VoipPacket packet = voipBuffer.Read();
            await SendOpusPacket(packet.SequenceId, packet.Frequency, packet.Channels, packet.EncodedBytes, packet.Data);
        }
    }

    internal void SubscribeNotifyMessage()
    {
        foreach (Connection client in NetworkManager.Singleton.Server.Clients)
        {
            if (client.Id == Id)
            {
                this.client = client;
                this.client.NotifyReceived += OnNotifyReceived;
                break;
            }
        }
    }

    #region Handle send message
    async UniTask SendOpusPacket(int sequenceId, int frequency, int channels, int encodedBytes, byte[] opusSamples)
    {
        Message message = Message.Create(MessageSendMode.Notify);
        message.AddUShort(Id);
        message.AddInt(sequenceId);
        message.AddInt(frequency);
        message.AddInt(channels);
        message.AddInt(encodedBytes);
        message.AddBytes(opusSamples);

        await UniTask.Delay(TimeSpan.FromSeconds(0.01), ignoreTimeScale: false);
        NetworkManager.Singleton.Server.SendToAll(message, Id);
    }
    #endregion

    #region Handle receive messages
    int lastSequenceId;
    void HandleOpusPacket(int sequenceId, int frequency, int channels, int encodedBytes, byte[] opusSamples)
    {
        if (sequenceId == lastSequenceId)
        {
            if (frequency > 44100)
                return;
        }
        if (sequenceId < lastSequenceId)
            return;
        voipBuffer.Write(new VoipPacket(sequenceId, frequency, channels, encodedBytes, opusSamples));
        lastSequenceId = sequenceId;
    }

    static void OnNotifyReceived(Message message)
    {
        if (message.UnreadBits < 1280)
            message.Release();
        if (voiceList.TryGetValue(message.GetUShort(), out NetworkVoice voice))
            voice.HandleOpusPacket(message.GetInt(), message.GetInt(), message.GetInt(), message.GetInt(), message.GetBytes());
    }
    #endregion
}
