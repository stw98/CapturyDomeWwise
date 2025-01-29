using System.Collections;
using System.Collections.Generic;
using Riptide;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class NetworkHumanPose : NetworkBehaviour
{
    //private float timer;
    //private float currentTick;
    //private float minTimeBetweenTicks;
    private const int BUFFER_SIZE = 512;

    Avatar avatar;
    HumanPoseHandler poseSetter;

    private Queue<PosePayload> refPoseBuffer = new Queue<PosePayload>(BUFFER_SIZE);
    private PosePayload[] retPoseBuffer = new PosePayload[BUFFER_SIZE];

    void Start()
    {
        avatar = GetComponent<Animator>().avatar;
        poseSetter = new HumanPoseHandler(avatar, gameObject.transform);
        SetNewServerTick(NetworkManager.SERVER_TICK_RATE);
    }

    // Update is called once per frame
    //void Update()
    //{
    //    timer += Time.deltaTime;
    //    while (timer >= minTimeBetweenTicks)
    //    {
    //        timer -= minTimeBetweenTicks;          
    //        HandleTick();
    //        currentTick++;
    //    }
    //}

    protected override void NetworkUpdate()
    {
        HandleTick();
    }

    void HandleTick()
    {
        int bufferIndex = -1;
        while (refPoseBuffer.Count > 0)
        {
            PosePayload refPose = refPoseBuffer.Dequeue();
            bufferIndex = refPose.Tick % BUFFER_SIZE;
            PosePayload retPose = SetRetargetPose(refPose);
            retPoseBuffer[bufferIndex] = retPose;
        }

        if (bufferIndex > -1)
            StartCoroutine(SendPose(retPoseBuffer[bufferIndex]));
    }

    PosePayload SetRetargetPose(PosePayload refPose)
    {
        poseSetter.SetHumanPose(ref refPose.pose);
        return new PosePayload
        {
            FromClientId = refPose.FromClientId,
            Tick = refPose.Tick,
            pose = refPose.pose
        };
    }

    IEnumerator SendPose(PosePayload retPose)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientID.CapturyPose);
        message.AddUShort(retPose.FromClientId);
        message.AddVector3(retPose.pose.bodyPosition);
        message.AddQuaternion(retPose.pose.bodyRotation);
        message.AddFloats(retPose.pose.muscles);
        message.AddInt(retPose.Tick);
        yield return new WaitForSeconds(0.01f);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    private void HandlePoseData(ushort fromClientId, Vector3 bodyPosition, Quaternion bodyRotation, float[] muscles, int tick)
    {
        PosePayload refPose = new PosePayload();
        refPose.FromClientId = fromClientId;
        refPose.pose.bodyPosition = bodyPosition;
        refPose.pose.bodyRotation = bodyRotation;
        refPose.pose.muscles = muscles;
        refPose.Tick = tick;
        refPoseBuffer.Enqueue(refPose);
    }

    [MessageHandler((ushort)ClientToServerID.CapturyPose)]
    private static void PoseData(ushort fromClientId, Message message)
    {
        if (Player.playerList.TryGetValue(fromClientId, out Player player))
        {
            player.NetworkHumanPose.HandlePoseData(fromClientId, message.GetVector3(), message.GetQuaternion(), message.GetFloats(), message.GetInt());
        }
    }
}
