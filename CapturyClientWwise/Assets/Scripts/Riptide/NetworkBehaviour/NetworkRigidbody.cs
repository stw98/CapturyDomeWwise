using Riptide;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NetworkRigidbody : NetworkBehaviour
{
    public StatePayload LatestServerState { get; private set; } = new StatePayload();
    Player player;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        player = GetComponent<Player>();
    }

    void OnEnable()
    {
        if (player != null)
            player.SetControllerPlayer += OnSetControllerPlayer;
    }

    void OnDisable()
    {
        if (player != null)
            player.SetControllerPlayer -= OnSetControllerPlayer;
    }

    void Start()
    {
        SetNewServerTick(NetworkManager.SERVER_TICK_RATE);
    }

    void OnSetControllerPlayer(bool isLocalPlayer)
    {
        if (!isLocalPlayer)
            rb.freezeRotation = true;
    }

    protected override void NetworkUpdate()
    {
        if (player.playerMode == PlayerMode.Controller && !player.isLocalPlayer)
        {
            rb.MovePosition(LatestServerState.position);
            if (rb.rotation != Quaternion.identity)
                rb.MoveRotation(LatestServerState.rotation);
            rb.velocity = LatestServerState.velocity;
            rb.angularVelocity = LatestServerState.angularVelocity;
        }
    }

    void HandleServerState(ushort fromClientId, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, int tick)
    {
        LatestServerState.FromClientId = fromClientId;
        LatestServerState.position = position;
        LatestServerState.rotation = rotation;
        LatestServerState.velocity = velocity;
        LatestServerState.angularVelocity = angularVelocity;
        LatestServerState.Tick = tick;
    }

    [MessageHandler((ushort)ServerToClientID.State)]
    private static void OnReceiveState(Message message)
    {
        ushort fromClientId = message.GetUShort();
        if (Player.playerList.TryGetValue(fromClientId, out Player player))
            player.NetworkRigidbody.HandleServerState(fromClientId, message.GetVector3(), message.GetQuaternion(), message.GetVector3(), message.GetVector3(), message.GetInt());
    }
}
