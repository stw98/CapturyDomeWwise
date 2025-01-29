using System.Collections.Generic;
using Riptide;
using UnityEngine;

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(Rigidbody))]
public class NetworkPlayerController : NetworkBehaviour
{
    private const int BUFFER_SIZE = 512;
    
    private const float rotationSpeed = 500f;
    private const float movementSpeed = 150f;
    
    private Queue<InputPayload> inputBuffer = new Queue<InputPayload>(BUFFER_SIZE);
    private StatePayload[] stateBuffer = new StatePayload[BUFFER_SIZE];

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void Start()
    {
        SetNewServerTick(NetworkManager.SERVER_TICK_RATE);
    }

    protected override void NetworkUpdate()
    {
        HandleTick();
    }

    void HandleTick()
    {
        int bufferIndex = -1;
        while (inputBuffer.Count > 0)
        {
            InputPayload inputPayload = inputBuffer.Dequeue();
            bufferIndex = inputPayload.Tick % BUFFER_SIZE;
            StatePayload state = HandleMovement(inputPayload);
            stateBuffer[bufferIndex] = state;
        }

        if (bufferIndex > -1)
            SendState(stateBuffer[bufferIndex]);
    }

    void SendState(StatePayload state)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientID.State);
        message.AddUShort(state.FromClientId);
        message.AddVector3(state.position);
        message.AddQuaternion(state.rotation);
        message.AddVector3(state.velocity);
        message.AddVector3(state.angularVelocity);
        message.AddInt(state.Tick);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    StatePayload HandleMovement(InputPayload inputPayload)
    {
        Vector3 direction = inputPayload.direction;
        Vector3 adjustedDirection = inputPayload.adjustedDirection;

        if (direction.magnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * ServerTickTime);

            Vector3 velocity = adjustedDirection * movementSpeed * ServerTickTime;
            rb.velocity = new Vector3 (velocity.x, velocity.y, velocity.z);
        }
        else
            rb.velocity = new Vector3 (0f, rb.velocity.y, 0f);

        return new StatePayload()
        {
            FromClientId = inputPayload.FromClientId,
            Tick = inputPayload.Tick,
            position = rb.position,
            rotation = rb.rotation,
            velocity = rb.velocity,
            angularVelocity = rb.angularVelocity,
        };
    }

    void HandleInputData(ushort fromClientId, Vector3 direction, Vector3 adjustDirection, int tick)
    {
        InputPayload inputPayload = new InputPayload();
        inputPayload.FromClientId = fromClientId;
        inputPayload.direction = direction;
        inputPayload.adjustedDirection = adjustDirection;
        inputPayload.Tick = tick;

        inputBuffer.Enqueue(inputPayload);
    }

    #region message
    [MessageHandler((ushort)ClientToServerID.Input)]
    private static void InputData(ushort fromClientId, Message message)
    {
        if (Player.playerList.TryGetValue(fromClientId, out Player player))
        {
            player.PlayerController.HandleInputData(fromClientId, message.GetVector3(), message.GetVector3(), message.GetInt());
        }
    }
    #endregion
}
