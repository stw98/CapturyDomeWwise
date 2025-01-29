using System.Collections.Generic;
using Riptide;
using UnityEngine;
using UnityEngine.Events;

public class Player : MonoBehaviour, IMultiplayObject
{
    public static Dictionary<ushort, Player> playerList = new Dictionary<ushort, Player>();

    public ushort Id { get; set; }
    public string Name { get; set; }
    public int actorId { get; internal set; }

    public bool isLocalPlayer { get; internal set; }
    public PlayerMode playerMode { get; internal set; }

    private NetworkHumanPose networkHumanPose;
    public NetworkHumanPose NetworkHumanPose
    {
        get
        {
            if (networkHumanPose == null)
                networkHumanPose = GetComponent<NetworkHumanPose>();
            return networkHumanPose;
        }
    }

    NetworkRigidbody networkRigidbody;
    public NetworkRigidbody NetworkRigidbody
    {
        get
        {
            if (networkRigidbody == null)
                networkRigidbody = GetComponent<NetworkRigidbody>();
            return networkRigidbody;
        }
    }

    public event UnityAction OnInitMicInfo = delegate { };
    public event UnityAction<bool> SetControllerPlayer = delegate { };

    void OnDestroy()
    {
        playerList.Remove(Id);
    }

    public static void SpawnControllerPlayer (ushort id, string username, Vector3 position)
    {
        Player player;
        if (id == NetworkManager.Singleton.Client.Id)
        {
            Debug.Log("Spawn a rubbish local");
            GameObject localPlayer = Instantiate(GameLogic.Singleton.PlayerPrefab, position, Quaternion.identity);
            localPlayer.AddComponent<NetworkPlayerController>();
            player = localPlayer.GetComponent<Player>();
            player.playerMode = PlayerMode.Controller;
            player.SetControllerPlayer?.Invoke(player.isLocalPlayer = true);

            player.InitVoiceInfo(id, true);
        }
        else
        {
            Debug.Log("Spawn a remoteeeeee");
            GameObject remotePlayer = Instantiate(GameLogic.Singleton.PlayerPrefab, position, Quaternion.identity);
            remotePlayer.AddComponent<NetworkRigidbody>();
            player = remotePlayer.GetComponent<Player>();
            player.playerMode = PlayerMode.Controller;
            player.SetControllerPlayer?.Invoke(player.isLocalPlayer = false);

            player.InitVoiceInfo(id, false);
        }

        player.InitPlayerInfo(id, username);
        playerList.Add(id, player);
    }

    public static void SpawnCapturyPlayer (ushort id, string username, Vector3 position)
    {
        Player player;

        Debug.Log("Spawn a remoteeeeee");
        player = Instantiate(GameLogic.Singleton.CapturyPlayerPrefab, position, Quaternion.identity).GetComponent<Player>();

        player.InitVoiceInfo(id, false);
        
        player.InitPlayerInfo(id, username);
        playerList.Add(id, player);
    }

    void InitPlayerInfo(ushort id, string username)
    {
        name = $"Player {id} : {(string.IsNullOrEmpty(username) ? "Guest" : username)}";
        Id = id;
        Name = string.IsNullOrEmpty(username) ? "Guest" : username;
    }

    void InitVoiceInfo(ushort id, bool isMicPlayer)
    {
        Voice voice = GetComponent<Voice>();
        if (voice != null)
        {
            voice.Id = id;
            voice.IsMicPlayer = isMicPlayer;
            Voice.voiceList.Add(voice.Id, voice);
            
            OnInitMicInfo.Invoke();
        }
    }

    [MessageHandler((ushort) ServerToClientID.SpawnControllerPlayer)]
    private static void SpawnControllerPlayer(Message message)
    {
        SpawnControllerPlayer(message.GetUShort(), message.GetString(), message.GetVector3());
    }

    [MessageHandler((ushort) ServerToClientID.SpawnCapturyPlayer)]
    private static void SpawnCapturyPlayer(Message message)
    {
        SpawnCapturyPlayer(message.GetUShort(), message.GetString(), message.GetVector3());
    }
}
