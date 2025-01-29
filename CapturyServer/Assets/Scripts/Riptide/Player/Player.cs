using System.Collections.Generic;
using Riptide;
using UnityEngine;

public class Player : MonoBehaviour, IPlayer
{
    public static Dictionary<ushort, Player> playerList = new Dictionary<ushort, Player>();

    public ushort Id { get; set; }
    public string Username { get; set; }
    public int actorId { get; private set; }

    public PlayerMode playerMode;

    NetworkHumanPose networkHumanPose;
    public NetworkHumanPose NetworkHumanPose
    {
        get
        {
            if (networkHumanPose == null)
            {
                networkHumanPose = GetComponent<NetworkHumanPose>();
            }
            return networkHumanPose;
        }
    }
    
    NetworkPlayerController playerController;
    public NetworkPlayerController PlayerController
    {
        get
        {
            if (playerController == null)
            {
                playerController = GetComponent<NetworkPlayerController>();
            }
            return playerController;
        }
    }

    void OnDestroy()
    {
        playerList.Remove(Id);
    }

    //Spawn Method For Other Client
    private static void SpawnControllerPlayer(ushort fromClientId, string username)
    {
        Debug.Log(fromClientId);
        Player player = Instantiate(NetworkManager.Singleton.ControllerPlayerPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity).GetComponent<Player>();
        player.name = $"Player {fromClientId} : {(string.IsNullOrEmpty(username) ? "Guest" : username)}";
        player.Id = fromClientId;
        player.Username = string.IsNullOrEmpty(username) ? "Guest" : username;
        player.playerMode = PlayerMode.Controller;

        NetworkVoice voice = player.GetComponent<NetworkVoice>();
        if (voice != null)
        {
            voice.Id = fromClientId;
            voice.SubscribeNotifyMessage();
        }

        player.SendControllerSpawnToAll((ushort) ServerToClientID.SpawnControllerPlayer);
        playerList.Add(player.Id, player);
        NetworkVoice.voiceList.Add(voice.Id, voice);
    }

    private static void SpawnCapturyPlayer(ushort fromClientId, string username, int actorId, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Debug.Log($"Captury: {fromClientId}");
        foreach (Player otherPlayer in playerList.Values)
        {
            if (otherPlayer.Id != fromClientId)
            {
                Debug.Log($"Spawning {fromClientId} to {otherPlayer.Id}");
                otherPlayer.SendCapturySpawn(otherPlayer.Id, fromClientId, username);
            }
        }

        Player player = Instantiate(NetworkManager.Singleton.CapturyPlayerPrefab, position, rotation).GetComponent<Player>();
        player.transform.localScale = scale;
        player.name = $"Player {fromClientId} : {(string.IsNullOrEmpty(username) ? "Guest" : username)}";
        player.Id = fromClientId;
        player.actorId = actorId;
        player.Username = string.IsNullOrEmpty(username) ? "Guest" : username;
        player.playerMode = PlayerMode.Captury;

        player.networkHumanPose = player.GetComponent<NetworkHumanPose>();

        NetworkVoice voice = player.GetComponent<NetworkVoice>();
        if (voice != null)
        {
            voice.Id = fromClientId;
            voice.SubscribeNotifyMessage();
        }
        
        playerList.Add(player.Id, player);
        NetworkVoice.voiceList.Add(voice.Id, voice);
    }

    #region message
    private void SendControllerSpawnToAll(ushort serverToClientID)
    {
        Message message = Message.Create(MessageSendMode.Reliable, serverToClientID);
        AddSpawnData(message);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    internal void SendSpawn(ushort fromClientID, ushort serverToClientID)
    {
        Message message = Message.Create(MessageSendMode.Reliable, serverToClientID);
        AddSpawnData(message);
        NetworkManager.Singleton.Server.Send(message, fromClientID);
    }

    private void SendCapturySpawn(ushort toClientID, ushort fromClientID, string username)
    {
        Message message = Message.Create(MessageSendMode.Reliable, (ushort) ServerToClientID.SpawnCapturyPlayer);
        message.AddUShort(fromClientID);
        message.AddString(username);
        message.AddVector3(transform.position);
        NetworkManager.Singleton.Server.Send(message, toClientID);
    }

    private Message AddSpawnData(Message message)
    {
        message.AddUShort(Id);
        message.AddString(Username);
        message.AddVector3(transform.position);

        return message;
    }

    [MessageHandler((ushort)ClientToServerID.ControllerPlayerName)]
    private static void ControllerPlayerName(ushort fromClientId, Message message)
    {
        SpawnControllerPlayer(fromClientId, message.GetString());
    }

    [MessageHandler((ushort)ClientToServerID.CapturyPlayerName)]
    private static void CapturyPlayerName(ushort fromClientId, Message message)
    {
        SpawnCapturyPlayer(fromClientId, message.GetString(), message.GetInt(), message.GetVector3(), message.GetQuaternion(), message.GetVector3());
    }
    #endregion
}
