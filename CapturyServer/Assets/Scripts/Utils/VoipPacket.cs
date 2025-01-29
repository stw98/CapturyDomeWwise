using System;

[Serializable]
public class VoipPacket
{
    public int SequenceId { get; set; }
    public int Frequency { get; set; }
    public int Channels { get; set; }
    public int EncodedBytes { get; set; }
    public byte[] Data { get; set; }

    public VoipPacket(int sequenceId, int frequency, int channels, int encodedBytes, byte[] opusSamples)
    {
        SequenceId = sequenceId;
        Frequency = frequency;
        Channels = channels;
        EncodedBytes = encodedBytes;
        Data = opusSamples;
    }
}
