using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using static PlayerInputActions;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class InputReader : MonoBehaviour, IPlayerActions
{
    static InputReader singleton;
    internal static InputReader Singleton
    {
        get => singleton;
        private set
        {
            if (singleton == null) singleton = value;
            else if (singleton != value)
            {
                Debug.Log($"{nameof(InputReader)} instance already existed, destroying duplicate.");
                Destroy(value);
            }
        }
    }
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    private int moveTouchId = -1;
    private int lookTouchId = -1;

    private PlayerInputActions inputActions;

    public delegate void TouchEvent(Vector2 position, int fingerId);
    public event TouchEvent OnTouchBegan;
    public event TouchEvent OnTouchMoved;
    public event TouchEvent OnTouchEnded;

   void Awake()
   {
        Singleton = this;
        EnhancedTouchSupport.Enable();
   }
   
    void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new PlayerInputActions();
            inputActions.Player.SetCallbacks(this);
        }
        inputActions.Player.Enable();

        Touch.onFingerDown += HandleFingerDown;
        Touch.onFingerMove += HandleFingerMove;
        Touch.onFingerUp += HandleFingerUp;
    }

    void OnDisable()
    {
        if (inputActions == null)
            return;
        
        inputActions.Player.RemoveCallbacks(this);
        inputActions.Player.Disable();

        Touch.onFingerDown -= HandleFingerDown;
        Touch.onFingerMove -= HandleFingerMove;
        Touch.onFingerUp -= HandleFingerUp;

        EnhancedTouchSupport.Disable();
    }

    void OnDestroy()
    {
        singleton = null;
    }

    //void Update()
    //{
    //    MoveInput = Vector2.zero;
    //    LookInput = Vector2.zero;
//
    //    foreach (var touch in Touch.activeTouches)
    //    {
    //        if (!touch.isInProgress) continue;
    //        
    //        switch (touch.phase)
    //        {
    //            case TouchPhase.Began:
    //                if (touch.startScreenPosition.x < Screen.width * 0.3)
    //                    moveTouchId = touch.touchId;
    //                else lookTouchId = touch.touchId;
    //                break;
    //            case TouchPhase.Ended:
    //                ResetInput();
    //                break;
    //            case TouchPhase.Moved:
    //                if (touch.touchId == moveTouchId)
    //                    MoveInput = inputActions.Player.Move.ReadValue<Vector2>();
    //                if (touch.touchId == lookTouchId)
    //                    LookInput = touch.delta * 10;
    //                break;
    //            case TouchPhase.Canceled:
    //                ResetInput();
    //                break;
    //            case TouchPhase.None:
    //                ResetInput();
    //                break;
    //            case TouchPhase.Stationary:
    //                if (moveTouchId != -1)
    //                    MoveInput = inputActions.Player.Move.ReadValue<Vector2>();
    //                if (lookTouchId != -1)
    //                    LookInput = touch.delta;
    //                break;
    //        }
    //    }
    //}

    void Update()
    {
        foreach (var touch in Touch.activeTouches)
        {
            if (!touch.isInProgress) continue;

            if (touch.phase == TouchPhase.Stationary)
                if (touch.touchId == moveTouchId)
                    MoveInput = inputActions.Player.Move.ReadValue<Vector2>();
                if (touch.touchId == lookTouchId)
                    LookInput = touch.delta * 10;
        }
    }

    void HandleFingerDown(Finger finger)
    {
        if (finger.screenPosition.x < Screen.width * 0.3)
            moveTouchId = finger.currentTouch.touchId;
        if (finger.screenPosition.x > Screen.width * 0.3)
            lookTouchId = finger.currentTouch.touchId;
    }

    void HandleFingerMove(Finger finger)
    {
        if (finger.currentTouch.touchId == moveTouchId)
            MoveInput = inputActions.Player.Move.ReadValue<Vector2>();
        if (finger.currentTouch.touchId == lookTouchId)
            LookInput = finger.currentTouch.delta * 10;
    }

    void HandleFingerUp(Finger finger)
    {
        ResetInput(finger.currentTouch.touchId);
    }

    void ResetInput()
    {
        moveTouchId = -1;
        MoveInput = Vector2.zero;
        lookTouchId = -1;
        LookInput = Vector2.zero;
    }

    void ResetInput(int touchId)
    {
        if (touchId == moveTouchId)
        {
            moveTouchId = -1;
            MoveInput = Vector2.zero;
        }
        if (touchId == lookTouchId)
        {
            lookTouchId = -1;
            LookInput = Vector2.zero;
        }
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.started)
            Debug.Log("Fire started");
        if (context.canceled)
            Debug.Log("Fire canceled");
        if (context.performed)
            Debug.Log("Fire performed");
    }

    public void OnMove(InputAction.CallbackContext context) { }
}
