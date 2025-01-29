using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    private static GameLogic _singleton;
    public static GameLogic Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(GameLogic)} instance already existed, destroying duplicate.");
                Destroy(value);
            }
        }
    }

    [Header("Prefab")]
    [SerializeField] GameObject CapturyPlayerPrefab;

    public GameObject PlayerPrefab => CapturyPlayerPrefab;

    void Awake()
    {
        Singleton = this;
    }
}
