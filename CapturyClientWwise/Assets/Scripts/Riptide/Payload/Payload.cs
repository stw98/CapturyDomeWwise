using System.Threading;

public abstract class Payload
{
    private ushort fromClientId;
    private int tick;

    public ushort FromClientId
    {
        get => Volatile.Read(ref fromClientId);
        set => Volatile.Write(ref fromClientId, value);
    }

    public int Tick
    {
        get => Volatile.Read(ref tick);
        set => Volatile.Write(ref tick, value);
    }
}
