using UnityEngine;

public class PosePayload : Payload
{
    public HumanPose pose;
}

public class InputPayload : Payload
{
    public Vector3 direction;
    public Vector3 adjustedDirection;
}

public class StatePayload : Payload
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public Vector3 angularVelocity;
}