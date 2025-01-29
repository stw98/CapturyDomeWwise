using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

public class CameraManager : MonoBehaviour
{
    private static List<CinemachineCamera> cameras = new List<CinemachineCamera>();
    public static CinemachineCamera ActiveVCam { get; private set; }

    internal static bool IsActiveCamera(CinemachineCamera vCam)
    {
        return vCam == ActiveVCam;
    }

    internal static void RegisterCamera(CinemachineCamera vCam)
    {
        cameras.Add(vCam);
    }

    internal static void UnregisterCamera(CinemachineCamera vCam)
    {
        cameras.Remove(vCam);
    }

    internal static void SwitchCamera(CinemachineCamera vCam)
    {
        vCam.Prioritize();
        ActiveVCam = vCam;
    }
}
