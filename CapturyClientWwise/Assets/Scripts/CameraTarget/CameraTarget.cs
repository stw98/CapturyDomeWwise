using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    public Transform Target { get; private set; }

    public void Reset()
    {
        Target = null;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void SetTarget(Transform target)
    {
        Target = target;
    }

    public void FollowTarget(Vector3 targetPosition, float time)
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, time);
    }
}
