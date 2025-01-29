using System;
using System.Net;
using Cysharp.Threading.Tasks;
using Riptide;
using Riptide.Utils;
using UnityEngine;

public enum ServerToClientID : ushort
{
    SpawnControllerPlayer = 1,
    SpawnCapturyPlayer,
    SpawnWeapon,
    CapturyPose,
    State,
}

public enum ClientToServerID : ushort
{
    ControllerPlayerName = 1,
    CapturyPlayerName,
    CapturyWeaponName,
    CapturyPose,
    Input,
}

public enum PlayerMode
{
    Controller = 1,
    Captury,
    VR,
}

public class NetworkManager : NetworkBehaviour
{
    private static NetworkManager singleton;
    public static NetworkManager Singleton
    {
        get => singleton;
        private set
        {
            if (singleton == null)
            {
                singleton = value;
            }
            else if (singleton != value)
            {
                Debug.Log($"{nameof(NetworkManager)} instance already existed, destroying duplicate.");
                Destroy(value);
            }
        }
    }

    public Client Client { get; private set; }

    private const ushort port = 51001;
    private string ip;
    public const float SERVER_TICK_RATE = 64f;

    public Action<string> SetErrorLog = delegate { };

    void Awake()
    {
        Singleton = this;
    }

    private void OnEnable()
    {
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        Client = new Client();
        UpdateTick += Client.Update;
        Client.Connected += OnConnected;
        Client.ConnectionFailed += OnConnectionFailed;
        Client.ClientDisconnected += OnPlayerLeft;
        Client.Disconnected += OnDisconnected;
    }

    private async void Start()
    {
        SetNewServerTick(SERVER_TICK_RATE);

        await GetIP();

        //To Ensure Physics Simulate Through Script If Derive Network Behaviour Class
        if (Physics.simulationMode != SimulationMode.Script)
            Physics.simulationMode = SimulationMode.Script;
    }

    private void OnDisable()
    {
        UpdateTick -= Client.Update;
    }

    void OnApplicationQuit()
    {
        Client.Disconnect();

        Client.Connected -= OnConnected;
        Client.ConnectionFailed -= OnConnectionFailed;
        Client.ClientDisconnected -= OnPlayerLeft;
        Client.Disconnected -= OnDisconnected;
    }

    public void Connect()
    {
        Client.Connect($"{ip}:{port}");
    }

    void OnConnected(object sender, EventArgs e)
    {
        switch (UIManager.Singleton.PlayMode)
        {
            case PlayerMode.Captury:
                UIManager.Singleton.StartCaptury();
                break;
            case PlayerMode.Controller:
                UIManager.Singleton.StartDome();
                break;
        }
    }

    void OnConnectionFailed(object sender, EventArgs e)
    {
        UIManager.Singleton.BackToMain("Please check your connection and try again.");
    }

    void OnDisconnected(object sender, EventArgs e)
    {
        foreach (Player player in Player.playerList.Values)
            Destroy(player.gameObject);

        UIManager.Singleton.BackToMain("Server is Disconnected.");
    }

    void OnPlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        Destroy(Player.playerList[e.Id].gameObject);
    }

    async UniTask GetIP()
    {
        string ddns = "capturyserver.ddns.net";
        IPAddress[] IPs = await Dns.GetHostAddressesAsync(ddns).AsUniTask();
        if (IPs != null)
        {
            ip = IPs[0].ToString();
            Debug.Log(ip);
        }
        if (IPs == null)
        {
            SetErrorLog.Invoke("Please Make Sure Your Server Is Running, or Server DNS is Correct.");
        }
    }
}