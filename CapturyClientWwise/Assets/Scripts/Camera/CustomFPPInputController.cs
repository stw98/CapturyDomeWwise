using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteAlways]
[SaveDuringPlay]
[AddComponentMenu("Cinemachine/Helpers/Cinemachine Input Axis Controller")]
public class CustomFPPInputController : InputAxisControllerBase<CustomFPPInputController.FPPInputReader> 
{
    internal delegate void SetControlDefaultsForAxis(in IInputAxisOwner.AxisDescriptor axis, ref Controller controller);
    
    internal static SetControlDefaultsForAxis SetControlDefaults;
    
    protected override void OnEnable()
    {
        base.OnEnable();
    }
    
    protected override void OnValidate()
    {
        base.OnValidate();
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
    }
    
    protected override void Reset()
    {
        base.Reset();
    }
    
    protected override void InitializeControllerDefaultsForAxis(in IInputAxisOwner.AxisDescriptor axis, Controller controller)
    {
        SetControlDefaults?.Invoke(in axis, ref controller);
    }
    
    private void Update()
    {
        if (Application.isPlaying)
        {
            UpdateControllers(Time.deltaTime);
        }
    }
    
    [Serializable]
    public sealed class FPPInputReader : IInputAxisReader
    {
        public delegate float ControlValueReader(InputAction action, IInputAxisOwner.AxisDescriptor.Hints hint, UnityEngine.Object context, ControlValueReader defaultReader);
    
        [Tooltip("The input value is multiplied by this amount prior to processing.  Controls the input power.  Set it to a negative value to invert the input")]
        [Range(-5f, 5f)] public float Gain = default;
    
        [Tooltip("Enable this if the input value is inherently dependent on frame time.  For example, mouse deltas will naturally be bigger for longer frames, so in this case the default deltaTime scaling should be canceled.")]
        public bool CancelDeltaTime;
    
        public float GetValue(UnityEngine.Object context, IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            float inputValue = 0f;
            if (InputReader.Singleton != null)
            {
                if (context is CustomFPPInputController c)
                    inputValue = ResolveAndReadInputAction(c, hint) * Gain;
            }
    
            return (Time.deltaTime > 0 && CancelDeltaTime) ? inputValue / Time.deltaTime : inputValue;
        }
    
        private float ResolveAndReadInputAction(CustomFPPInputController context, IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            if (InputReader.Singleton != null) return ReadInput(hint, context, null);
    
            return 0f;
        }
    
        private float ReadInput(IInputAxisOwner.AxisDescriptor.Hints hint, UnityEngine.Object context, ControlValueReader defaultReader)
        {
            try
            {              
                if (InputReader.Singleton.LookInput.GetType() == typeof(Vector2))
                {
                    Vector2 vector = InputReader.Singleton.LookInput;
                    return (hint == IInputAxisOwner.AxisDescriptor.Hints.Y) ? vector.y : vector.x;
                }
            }
            catch (InvalidOperationException)
            {
                Debug.LogError("An action in " + context.name + " is mapped to a " + "wrong control.  CinemachineInputAxisController.Reader can only handle float or Vector2 types.  To handle other types you can install a custom handler for CinemachineInputAxisController.ReadControlValueOverride.");
            }
            return 0f;
        }
    }
}
