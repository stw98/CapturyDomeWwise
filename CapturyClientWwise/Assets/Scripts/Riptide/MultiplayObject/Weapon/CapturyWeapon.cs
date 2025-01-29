using System;
using System.Collections.Generic;
using Riptide;
using UnityEngine;

public class CapturyWeapon : MonoBehaviour, IMultiplayObject
{
    public static Dictionary<ushort, CapturyWeapon> weaponList = new Dictionary<ushort, CapturyWeapon>();

    public ushort Id { get; set; }
    public string Name { get; set; }
    public bool isCapturySkelWeapon { get; internal set; } = false;
    public bool isCapturyARWeapon { get; internal set; } = false;
    public int WeaponId { get; internal set; }

    public Action OnInitMicInfo = delegate { };

    void OnDestroy()
    {
        weaponList.Remove(Id);
    }

    public static void Spawn (ushort id, string username, Vector3 position)
    {
        CapturyWeapon weapon;
        if (id == NetworkManager.Singleton.Client.Id)
        {
            Debug.Log("Spawn a rubbish local");
            weapon = Instantiate(GameLogic.Singleton.CapturyPlayerPrefab, position, Quaternion.identity).GetComponent<CapturyWeapon>();
        }
        else
        {
            Debug.Log("Spawn a remoteeeeee");
            weapon = Instantiate(GameLogic.Singleton.CapturyPlayerPrefab, position, Quaternion.identity).GetComponent<CapturyWeapon>();
        }
        
        weaponList.Add(id, weapon);
    }

    void InitWeaponInfo(ushort id, string username)
    {
        name = $"Player {id} : {(string.IsNullOrEmpty(username) ? "Guest" : username)}";
        Id = id;
        Name = string.IsNullOrEmpty(username) ? "Guest" : username;
    }

    [MessageHandler((ushort) ServerToClientID.SpawnWeapon)]
    private static void SpawnWeapon(Message message)
    {
        Spawn(message.GetUShort(), message.GetString(), message.GetVector3());
    }
}
