using UnityEngine;
using Unity.Cinemachine;
using Riptide;

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkRigidbody))]
public class NetworkPlayerController : NetworkBehaviour
{
    /// <summary>
    /// Virtual Cameras
    /// </summary>
    [SerializeField] private string TPPTargetTag = "TPPTarget";
    [SerializeField] private string TPPCamTag = "TPPCamera";
    [SerializeField] private string FPPCamTag = "FPPCamera";
    private CinemachineCamera TPPCamera;
    private CinemachineCamera FPPCamera;
    private CameraTarget TPPCamTarget;
    private CameraTarget FPPCamTarget;

    /// <summary>
    /// Controller Movement
    /// </summary>
    private const float rotationSpeed = 500f;
    private const float movementSpeed = 150f;

    private const int BUFFER_SIZE = 512;
    private StatePayload[] clientStateBuffer = new StatePayload[BUFFER_SIZE];
    private InputPayload[] clientInputBuffer = new InputPayload[BUFFER_SIZE];

    private Rigidbody rb;
    private Player player;
    private NetworkRigidbody networkRigidbody;
    private Animator animator;

    private Vector3 direction;

    private Transform mainCam;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        player = GetComponent<Player>();
        networkRigidbody = GetComponent<NetworkRigidbody>();
        animator = GetComponent<Animator>();
        mainCam = Camera.main.transform;
    }

    void OnEnable()
    {        
        player.SetControllerPlayer += OnSetControllerPlayer;
    }

    void OnDisable()
    {
        player.SetControllerPlayer -= OnSetControllerPlayer;
    }

    void OnDestroy()
    {
        CameraManager.UnregisterCamera(TPPCamera);
        CameraManager.UnregisterCamera(FPPCamera);
    }

    void Start()
    {
        SetNewServerTick(NetworkManager.SERVER_TICK_RATE);

        FinishNetworkUpdate = () => { if (networkRigidbody.LatestServerState != null) HandleServerReconciliation(); };
    }

    protected override void NetworkUpdate()
    {
        if (player.playerMode == PlayerMode.Controller && player.isLocalPlayer)
        {
            HandlePrediction();

            //ForThird Person
            //TPPCamTarget.FollowTarget(new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z), ServerTickTime * 4f);
        }
    }

    void OnSetControllerPlayer(bool isLocalPlayer)
    {
        if (player.playerMode == PlayerMode.Controller && isLocalPlayer)
        {
            if (InputReader.Singleton == null)
                gameObject.AddComponent<InputReader>();

            //For Third Person
            //TPPCamera = GameObject.FindGameObjectWithTag(TPPCamTag).GetComponent<CinemachineCamera>();
            //CameraManager.RegisterCamera(TPPCamera);
            //TPPCamTarget = GameObject.FindGameObjectWithTag(TPPTargetTag).GetComponent<CameraTarget>();
            //SetCamTargetPosition(TPPCamTarget, 1.5f);
            //InitTPPCamera(TPPCamera, TPPCamTarget);

            FPPCamera = GameObject.FindGameObjectWithTag(FPPCamTag).GetComponent<CinemachineCamera>();
            CameraManager.RegisterCamera(FPPCamera);
            FPPCamTarget = gameObject.GetComponentInChildren<CameraTarget>();
            SetCamTargetPosition(FPPCamTarget, 1.7f);
            FPPCamera.Follow = FPPCamTarget.transform;

            CameraManager.SwitchCamera(FPPCamera);
        }
    }

    void SetCamTargetPosition(CameraTarget camTarget, float YOffest)
    {
        camTarget.SetTarget(transform);
        camTarget.transform.position = new Vector3(transform.position.x, transform.position.y + YOffest, transform.position.z);
    }

    void InitTPPCamera(CinemachineCamera TPPCamera, CameraTarget TPPTarget)
    {
        TPPCamera.Follow = TPPTarget.transform;
        TPPCamera.LookAt = TPPTarget.transform;
    }

    void HandleServerReconciliation()
    {
        int serverBufferIndex = networkRigidbody.LatestServerState.Tick % BUFFER_SIZE;

        if (clientStateBuffer[serverBufferIndex] == null) return;
        
        float positionError = 0;
        float rotationError = 0;
        PredictionError predictionError = PredictionError.None;
        CalculateError(ref positionError, ref rotationError, ref predictionError, serverBufferIndex);

        switch (predictionError)
        {
            case PredictionError.None:
                break;
            case PredictionError.TinyError:
                SetPredictionCorrection();
                break;
            case PredictionError.MediumError:
                SetPredictionCorrection(2f, 4f);
                break;
            case PredictionError.MajorError:
                SetPredictionCorrection(4f, 16f);
                break;
            case PredictionError.Cheating:
                SetPredictionCorrection(16f, 32f);
                break;
        }

        {
            //Update to latest Server State
            clientStateBuffer[serverBufferIndex] = networkRigidbody.LatestServerState;

            //Now resimulate rest of the ticks from current tick on client side
            int tickToProcess = networkRigidbody.LatestServerState.Tick + 1;

            //Resimulate all ticks between rewound tick to current tick, and send redundant input to server
            while (tickToProcess < currentTick)
            {
                int bufferIndex = tickToProcess % BUFFER_SIZE;
                clientStateBuffer[bufferIndex] = RewindState(tickToProcess);
                clientStateBuffer[bufferIndex] = HandleMovement(ref clientInputBuffer[bufferIndex]);
                
                SendInput(clientInputBuffer[bufferIndex]);

                ++tickToProcess;
            }
        }
    }

    void CalculateError(ref float positionError, ref float rotationError, ref PredictionError predictionError, int? serverBufferIndex)
    {
        float cheatingThreshold = 2f;

        if (serverBufferIndex == null) return;

        if (serverBufferIndex != null)
        {
            positionError = Vector3.Distance(networkRigidbody.LatestServerState.position, clientStateBuffer[(int)serverBufferIndex].position);
            rotationError = Quaternion.Angle(networkRigidbody.LatestServerState.rotation, clientStateBuffer[(int)serverBufferIndex].rotation);
        }

        if (0.01f > positionError && positionError >= 0.00000001f|| 0.1f > rotationError && rotationError >= 0.0001f)
            predictionError = PredictionError.TinyError;
        else if (0.05 > positionError && positionError >= 0.01f|| 1f > rotationError && rotationError >= 0.1f)
            predictionError = PredictionError.MediumError;
        else if (cheatingThreshold > positionError && positionError >= 0.05f|| cheatingThreshold > rotationError && rotationError >= 1f)
            predictionError = PredictionError.MajorError;
        else if (positionError >= cheatingThreshold || rotationError >= cheatingThreshold)
            predictionError = PredictionError.Cheating;
        else predictionError = PredictionError.None;
    }

    void SetPredictionCorrection(float posCorrectionSpeed = 1f, float rotCorrectionSpeed = 1f)
    {
        //Rewind & Replay
        rb.position = Vector3.Lerp(rb.position, networkRigidbody.LatestServerState.position, ServerTickTime * posCorrectionSpeed);
        if (rb.rotation != Quaternion.identity)
            rb.rotation = Quaternion.Lerp(rb.rotation, networkRigidbody.LatestServerState.rotation, ServerTickTime * rotCorrectionSpeed);
        rb.velocity = networkRigidbody.LatestServerState.velocity;
        rb.angularVelocity = networkRigidbody.LatestServerState.angularVelocity;
    }

    StatePayload RewindState(int rewindTick)
    {
        return new StatePayload
        {
            position = rb.position,
            rotation = rb.rotation,
            velocity = rb.velocity,
            angularVelocity = rb.angularVelocity,
            Tick = rewindTick,
        };
    }

    void HandlePrediction()
    {
        int bufferIndex = currentTick % BUFFER_SIZE;

        InputPayload inputPayload = new InputPayload();
        clientStateBuffer[bufferIndex] = HandleMovement(ref inputPayload);
        clientInputBuffer[bufferIndex] = inputPayload;

        Physics.Simulate(ServerTickTime);

        SendInput(inputPayload);
    }

    void SendInput(InputPayload inputPayload)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerID.Input);
        message.AddVector3(inputPayload.direction);
        message.AddVector3(inputPayload.movementDirection);
        message.AddInt(inputPayload.Tick);
        NetworkManager.Singleton.Client.Send(message);
    }

    StatePayload HandleMovement(ref InputPayload inputPayload)
    {
        //For First Person
        Quaternion angleAxis = Quaternion.AngleAxis(mainCam.eulerAngles.y, Vector3.up);
        direction = new Vector3(Camera.main.transform.forward.x, 0f, Camera.main.transform.forward.z).normalized;
        Vector3 movementDirection = angleAxis * new Vector3(InputReader.Singleton.MoveInput.x, 0f, InputReader.Singleton.MoveInput.y).normalized;
        //For Third Person
        //Quaternion angleAxis = Quaternion.AngleAxis(mainCam.eulerAngles.y, Vector3.up);
        //Vector3 movementDirection = angleAxis * new Vector3(input.MoveInput.x, 0f, input.MoveInput.y).normalized;
        //direction = movementDirection;

        inputPayload.FromClientId = player.Id;
        inputPayload.direction = direction;
        inputPayload.movementDirection = movementDirection;
        inputPayload.Tick = currentTick;

        if (direction.sqrMagnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * ServerTickTime);

            Vector3 velocity = movementDirection * movementSpeed * ServerTickTime;
            rb.velocity = new Vector3(velocity.x, velocity.y, velocity.z);
        }
        else
            rb.velocity = new Vector3 (0f, rb.velocity.y, 0f);

        return new StatePayload()
        {
            Tick = inputPayload.Tick,
            position = rb.position,
            rotation = rb.rotation,
            velocity = rb.velocity,
            angularVelocity = rb.angularVelocity,
        };
    }

    enum PredictionError
    {
        None = 1,
        TinyError,
        MediumError,
        MajorError,
        Cheating,
    }
}
