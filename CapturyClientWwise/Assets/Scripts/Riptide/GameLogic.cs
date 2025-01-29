using UnityEngine;

public class GameLogic : MonoBehaviour
{
    private static GameLogic singleton;
    public static GameLogic Singleton
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
                Debug.Log($"{nameof(GameLogic)} instance already existed, destroying duplicate.");
                Destroy(value);
            }
        }
    }

    [Header("Prefabs")]
    [SerializeField] GameObject capturyPlayerPrefab;
    [SerializeField] GameObject playerPrefab;

    public GameObject CapturyPlayerPrefab => capturyPlayerPrefab;
    public GameObject PlayerPrefab => playerPrefab;

    void Awake()
    {
        Singleton = this;
    }
}
