using System;
using System.Collections.Generic;
using Riptide;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class UIManager : MonoBehaviour
{
    private static UIManager singleton;
    public static UIManager Singleton
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
                Debug.Log($"{nameof(UIManager)} instance already existed, destroying duplicate.");
                Destroy(value);
            }
        }
    }

    [Header("Connect")]
    [SerializeField] GameObject connectUI;
    [SerializeField] TMP_InputField usernameField;
    [SerializeField] Button connectButton;
    [SerializeField] TextMeshProUGUI errorLog;

    [Header("PlayerMode")]
    [SerializeField] PlayerMode playMode;
    public PlayerMode PlayMode => playMode;
    [SerializeField] TextMeshProUGUI playModeText;

    [Header("MicDevices")]
    [SerializeField] TMP_Dropdown devicesUI;
    public int SelectedDevice => devicesUI.value;

    [Header("ControllerUI")]
    [SerializeField] GameObject mobileController;
    public GameObject mobileControllerInstance { get; private set; }

    [Header("Captury")]
    [SerializeField] GameObject capturyPrefab;
    public GameObject capturyInstance { get; private set; }

    [Header("Environment")]
    [SerializeField] GameObject environment;
    public GameObject environmentInstance { get; set; }

    List<GameObject> instanceToDestroy = new List<GameObject>();

    public string Username => usernameField.text;

    public Action destroyCaptury = delegate { };

    void Awake()
    {
        Singleton = this;

        #if UNITY_ANDROID || UNITY_IOS
        //QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
        #else
        Application.targetFrameRate = -1;
        #endif
    }

    void Start()
    {
        NetworkManager.Singleton.SetErrorLog += DisplayErrorLog;

        #if UNITY_ANDROID || UNITY_IOS
        StartCoroutine(RequestPermission());
        #else
        AddDevices();
        #endif
    }

    void OnDisable()
    {
        NetworkManager.Singleton.SetErrorLog -= DisplayErrorLog;
    }

    public void ConnectClicked()
    {
        usernameField.interactable = false;
        connectButton.interactable = false;
        connectUI.SetActive(false);

        NetworkManager.Singleton.Connect();
    }

    public void StartCaptury()
    {
        instanceToDestroy.Add(capturyInstance = Instantiate(capturyPrefab, Vector3.zero, Quaternion.identity));
        instanceToDestroy.Add(environmentInstance = Instantiate(environment, Vector3.zero, Quaternion.identity));
    }

    public void BackToMain()
    {
        usernameField.interactable = true;
        connectButton.interactable = true;
        connectUI.SetActive(true);

        for(int i = 0; i < instanceToDestroy.Count; i++) Destroy(instanceToDestroy[i]);
        instanceToDestroy.Clear();
    }

    public void BackToMain(string log)
    {
        usernameField.interactable = true;
        connectButton.interactable = true;
        connectUI.SetActive(true);

        for(int i = 0; i < instanceToDestroy.Count; i++) Destroy(instanceToDestroy[i]);
        instanceToDestroy.Clear();

        errorLog.text = log;
        errorLog.transform.parent.gameObject.SetActive(true);
    }

    public void StartDome()
    {
        SendName();
        instanceToDestroy.Add(environmentInstance = Instantiate(environment, Vector3.zero, Quaternion.identity));

        #if UNITY_ANDROID || UNITY_IOS
        instanceToDestroy.Add(mobileControllerInstance = Instantiate(mobileController, Vector3.zero, Quaternion.identity));
        #endif
    }

    public void SendName()
    {
        Message message = Message.Create(MessageSendMode.Reliable, (ushort) ClientToServerID.ControllerPlayerName);
        message.AddString(Username);
        NetworkManager.Singleton.Client.Send(message);
    }

    public void SetPlayMode()
    {
        if (playMode == PlayerMode.Captury)
        {
            playMode = PlayerMode.Controller;
            playModeText.text = "Dome";
            return;
        }
        if (playMode == PlayerMode.Controller)
        {
            playMode = PlayerMode.Captury;
            playModeText.text = "Captury";
        }
    }

    void AddDevices()
    {
        foreach (var device in Mic.AvailableDevices)
        {
            devicesUI.options.Add(new TMP_Dropdown.OptionData(device.Name));
        }
        devicesUI.RefreshShownValue();
    }

    public void HideErrorLog()
    {
        errorLog.transform.parent.gameObject.SetActive(false);
    }

    void DisplayErrorLog(string log)
    {
        errorLog.text = log;
        errorLog.transform.parent.gameObject.SetActive(true);
    }

    /// <summary>
    /// Android Permission
    /// </summary>
    #if UNITY_ANDROID
    IEnumerator RequestPermission()
    {
        yield return Permission.HasUserAuthorizedPermission(Permission.Microphone);

        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            AddDevices();
        }
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            PermissionCallbacks callbacks = new PermissionCallbacks();
            callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
            callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
            callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;
            Permission.RequestUserPermission(Permission.Microphone, callbacks);
        }
    }

    /// <summary>
    /// Android Permission Callbacks
    /// </summary>
    internal void PermissionCallbacks_PermissionDeniedAndDontAskAgain(string permissionName)
    {
        Debug.Log($"Highly recommend you to allow this permission.");
    }

    internal void PermissionCallbacks_PermissionGranted(string permissionName)
    {
        Debug.Log($"Permission allowed.");
        AddDevices();
    }

    internal void PermissionCallbacks_PermissionDenied(string permissionName)
    {
        Debug.Log($"Highly recommend you to allow this permission.");
    }

    /// <summary>
    /// IOS Permission
    /// </summary>
    #elif UNITY_IOS
    IEnumerator RequestPermission()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

        if (Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            AddDevices();
        }
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Debug.Log($"Highly recommend you to allow this permission.");
        }
    }
    #endif
}
