using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    private static UIManager _singleton;
    public static UIManager Singleton
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
                Debug.Log($"{nameof(UIManager)} instance already existed, destroying duplicate.");
                Destroy(value);
            }
        }
    }

    [Header("Connect")]
    [SerializeField] GameObject connectUI;
    [SerializeField] Button connectButton;
    [SerializeField] TextMeshProUGUI errorLog;

    [Header("Tracking Area")]
    [SerializeField] GameObject trackingArea;

    void Awake()
    {
        Singleton = this;
        Application.targetFrameRate = -1;
    }

    void Start()
    {
        NetworkManager.Singleton.ErrorLog += DisplayErrorLog;
    }

    void OnDisable()
    {
        NetworkManager.Singleton.ErrorLog -= DisplayErrorLog;
    }

    public void StartClicked()
    {
        connectButton.interactable = false;
        connectUI.SetActive(false);

        NetworkManager.Singleton.StartHost();

        trackingArea.SetActive(true);
    }

    public void BackToMain()
    {
        NetworkManager.Singleton.LeaveGame();
        connectButton.interactable = true;
        connectUI.SetActive(true);
    }

    void DisplayErrorLog(string log)
    {
        errorLog.text = log;
        errorLog.transform.parent.gameObject.SetActive(true);
    }

    public void HideErrorLog()
    {
        errorLog.transform.parent.gameObject.SetActive(false);
    }
}
