using System;
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
    private static NetworkManager _singleton;
    public static NetworkManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying object!");
                Destroy(value);
            }
        }
    }
    private const ushort port = 51001;
    [SerializeField] private ushort maxPlayers = 5;

    [Header("Prefabs")]
    [SerializeField] private GameObject capturyPlayerPrefab;
    [SerializeField] private GameObject controllerPlayerPrefab;

    public GameObject CapturyPlayerPrefab => capturyPlayerPrefab;
    public GameObject ControllerPlayerPrefab => controllerPlayerPrefab;

    public Server Server { get; private set; }
    public const float SERVER_TICK_RATE = 64f;

    public Action <string> ErrorLog = delegate { };

    private void Awake()
    {
        Singleton = this;
    }

    private void OnEnable()
    {
        SetNewServerTick(SERVER_TICK_RATE);

        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        Server = new Server();
        UpdateTick += Server.Update;
        Server.ClientConnected += NewPlayerConnected;
        Server.ClientDisconnected += OnPlayerLeft;
    }

    private void Start()
    {
        FinishNetworkTick = OnFinishNetworkTick;

        //Used In Dedicated Server onlyyyy!!!!
        StartHostAsDefault();
    }

    private void OnDisable()
    {
        UpdateTick -= Server.Update;
    }

    private void OnApplicationQuit()
    {
        LeaveGame();
    }

    public void StartHost()
    {
        Server.Start(port, maxPlayers);
    }
    
    private void StartHostAsDefault()
    {
        Server.Start(port, 20);
    }
    internal void LeaveGame()
    {
        if (Server.IsRunning)
        {
            Server.Stop();
        }
    }

    private void OnFinishNetworkTick()
    {
        Physics.Simulate(ServerTickTime);
    }

    private void NewPlayerConnected(object sender, ServerConnectedEventArgs e)
    {
        foreach (Player player in Player.playerList.Values)
        {
            Debug.Log(player.name);
            if (player.Id != e.Client.Id)
            {
                switch (player.playerMode)
                {
                    case PlayerMode.Controller:
                        Debug.Log($"Spawning {e.Client.Id} to {player.Id}");
                        player.SendSpawn(e.Client.Id, (ushort) ServerToClientID.SpawnControllerPlayer);
                        break;

                    case PlayerMode.Captury:
                        Debug.Log($"Spawning {e.Client.Id} to {player.Id}");
                        player.SendSpawn(e.Client.Id, (ushort) ServerToClientID.SpawnCapturyPlayer);
                        break;
                    case PlayerMode.VR:
                        break;
                }
            }
        }
    }

    private void OnPlayerLeft(object sender, ServerDisconnectedEventArgs e)
    {
        Destroy(Player.playerList[e.Client.Id].gameObject);
    }

    public void SetMaxPlayer(string maxPlayersValue)
    {
        ushort value;
        bool isUShort = ushort.TryParse(maxPlayersValue, out value);
        if (isUShort)
        {
            maxPlayers = value;
            return;
        }
        else
        {
            ErrorLog.Invoke("Max player must be a number. Now reset max player to 5");
            maxPlayers = 5;
        }
    }
}