using System.Collections;
using Captury;
using Riptide;
using UnityEngine;

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(Avatar))]
public class NetworkHumanPose : NetworkBehaviour
{
    private const int BUFFER_SIZE = 512;

    private Player player;

    private Avatar avatar;
    private HumanPoseHandler poseSetter;

    private PosePayload[] refPoseBuffer = new PosePayload[BUFFER_SIZE];
    private PosePayload[] retPoseBuffer = new PosePayload[BUFFER_SIZE];
    PosePayload latestServerRetPose = new PosePayload();

    void OnEnable()
    {
        player = GetComponent<Player>();
        avatar = GetComponent<Animator>().avatar;
        poseSetter = new HumanPoseHandler(avatar, transform);
    }

    void Start()
    {
        SetNewServerTick(NetworkManager.SERVER_TICK_RATE);
    }

    protected override void NetworkUpdate()
    {
        if (player.playerMode == PlayerMode.Captury && player.isLocalPlayer)
            HandlePrediction();

        if (player.playerMode == PlayerMode.Captury && !player.isLocalPlayer)
        {
            int serverBufferIndex = latestServerRetPose.Tick % BUFFER_SIZE;
            poseSetter.SetHumanPose(ref latestServerRetPose.pose);
            retPoseBuffer[serverBufferIndex] = latestServerRetPose;
        }
    }

    private void HandleServerReconciliation()
    {
        int serverBufferIndex = latestServerRetPose.Tick % BUFFER_SIZE;

        if (retPoseBuffer[serverBufferIndex] == null)
            return;
        
        float positionError = Vector3.Distance(latestServerRetPose.pose.bodyPosition, retPoseBuffer[serverBufferIndex].pose.bodyPosition);

        if (positionError > 0.001f)
        {
            //Rewind & Replay
            poseSetter.SetHumanPose(ref latestServerRetPose.pose);

            //Update to latest Server RetargetPose
            retPoseBuffer[serverBufferIndex] = latestServerRetPose;

            //Now resimulate rest of the ticks from current tick on client side
            int tickToProcess = latestServerRetPose.Tick + 1;

            while (tickToProcess < currentTick)
            {
                int bufferIndex = tickToProcess % BUFFER_SIZE;
                retPoseBuffer[bufferIndex] = RewindPose(tickToProcess);
                PosePayload newPose = new PosePayload();
                GetRefPose(ref newPose);
                poseSetter.SetHumanPose(ref newPose.pose);
                ++tickToProcess;
            }
        }
    }

    PosePayload RewindPose(int rewindTick)
    {
        PosePayload retPose = new PosePayload();
        GetRetPose(ref retPose, rewindTick);
        return retPose;
        
    }

    private void HandlePrediction()
    {
        int bufferIndex = currentTick % BUFFER_SIZE;
            
        PosePayload refPose = new PosePayload();
        GetRefPose(ref refPose);
        refPoseBuffer[bufferIndex] = refPose;
        SetRetPose(refPose);
        
        StartCoroutine(SendPose(refPose));
    }

    private void GetRefPose(ref PosePayload refPose)
    {
        refPose.FromClientId = player.Id;
        CapturyNetworkPlugin.Instance.skeletons[player.actorId].poseGetter.GetHumanPose(ref refPose.pose);
        refPose.Tick = currentTick;
    }

    private void GetRetPose(ref PosePayload retPose, int tick)
    {
        retPose.FromClientId = player.Id;
        poseSetter.GetHumanPose(ref retPose.pose);
        retPose.Tick = tick;
    }

    private PosePayload SetRetPose(PosePayload refPose)
    {
        poseSetter.SetHumanPose(ref refPose.pose);

        return new PosePayload
        {
            FromClientId = refPose.FromClientId,
            pose = refPose.pose,
            Tick = refPose.Tick
        };
    }

    private void HandleServerPoseData(ushort fromClientId, Vector3 bodyPosition, Quaternion bodyRotation, float[] muscles, int tick)
    {
        latestServerRetPose.FromClientId = fromClientId;
        latestServerRetPose.pose.bodyPosition = bodyPosition;
        latestServerRetPose.pose.bodyRotation = bodyRotation;
        latestServerRetPose.pose.muscles = muscles;
        latestServerRetPose.Tick = tick;
    }

    IEnumerator SendPose(PosePayload referencePose)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerID.CapturyPose);
        message.AddVector3(referencePose.pose.bodyPosition);
        message.AddQuaternion(referencePose.pose.bodyRotation);
        message.AddFloats(referencePose.pose.muscles);
        message.AddInt(referencePose.Tick);
        yield return new WaitForSeconds(0.01f);
        NetworkManager.Singleton.Client.Send(message);
    }

    [MessageHandler((ushort)ServerToClientID.CapturyPose)]
    private static void OnReceiveRetPoseData(Message message)
    {
        ushort fromClientId = message.GetUShort();
        if (Player.playerList.TryGetValue(fromClientId, out Player player))
        {
            NetworkHumanPose networkHumanPose = player.GetComponent<NetworkHumanPose>();
            if (networkHumanPose == null)
            {
                Debug.Log("It's NULLLLLLLL");
                return;
            }
            networkHumanPose.HandleServerPoseData(fromClientId, message.GetVector3(), message.GetQuaternion(), message.GetFloats(), message.GetInt());
        }
    }
}
