public class VoipPacket
{
    public int Frequency { get; set; }
    public int Channels { get; set; }
    public int EncodedBytes { get; set; }
    public byte[] Data { get; set; }

    public VoipPacket(int frequency, int channels, int encodedBytes, byte[] opusSamples)
    {
        Frequency = frequency;
        Channels = channels;
        EncodedBytes = encodedBytes;
        Data = opusSamples;
    }
}
