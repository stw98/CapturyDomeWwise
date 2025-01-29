using System;
using UnityEngine;

public abstract class NetworkBehaviour : MonoBehaviour
{
    protected float timer;
    protected int currentTick;
    protected float ServerTickTime { get; private set; }

    protected event Action UpdateTick = delegate { };
    public Action FinishNetworkUpdate = delegate { };

    protected void SetNewServerTick(float serverTickRate)
    {
        ServerTickTime = 1 / serverTickRate;
    }

    protected void SetServerTickAsDefault()
    {
        ServerTickTime = Time.fixedDeltaTime;
    }

    void Update()
    {
        if (ServerTickTime == default) throw new Exception("ServerTickTime is not set. Please set (ServerTickTime) before Update State.");

        timer += Time.deltaTime;

        while (timer >= ServerTickTime)
        {
            timer -= ServerTickTime;
            UpdateTick.Invoke();
            NetworkUpdate();
            if (FinishNetworkUpdate != null) FinishNetworkUpdate();
            ++currentTick;
        }

        OnFinishNetworkTick();
    }

    protected virtual void NetworkUpdate() { }

    protected virtual void OnFinishNetworkTick() { }
}
