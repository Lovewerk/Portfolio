using System;
using UnityEngine;

/// <summary>
/// A driver is responsible for managing a single value which other objects/scripts will use as a control value for their behaviour. 
/// </summary>
public abstract class Driver : MonoBehaviour
{
    #region variables/properties

    private bool active; //is the Driver active/capable of calculating values
    public bool Active => active;

    [SerializeField] float raw; // the unscaled drive value
    public float Raw
    {
        get => raw;
        protected set
        {
            if (raw != value)
            {
                raw = clampToRange ? Mathf.Clamp(value, rawMin, rawMax) : value;

                // we want the scaled range to go from scaledRange.x to scaledRange.y, not necessarily from max to min
                scaled = clampToRange ? MathUtil.RemapClamped(value, rawMin, rawMax, scaledRange.x, scaledRange.y) : MathUtil.Remap(value, rawMin, rawMax, scaledRange.x, scaledRange.y);

                RaiseValueChanged();

                IsInRange = MathUtil.IsInRange(raw, rawRange, false); // inclusive is set to false because we want to detect specifically when the range is entered or exited (ie: when we arrive at/leave 0 and 1)
            }
        }
    }

    [SerializeField] float scaled;
    public float Scaled => scaled;


    [Tooltip("Controls whether values will be constrained to their respective range")]
    [SerializeField] bool clampToRange = true;

    [SerializeField] Vector2 rawRange = new Vector2(0f, 1f); // the range to which raw will be clamped if clampToRange is true
    public Vector2 RawRange
    {

        get => rawRange;
        protected set
        {
            rawRange = value;
            rawMax = Mathf.Max(rawRange.x, rawRange.y);
            rawMin = Mathf.Min(rawRange.x, rawRange.y);
        }
    }

    [SerializeField] Vector2 scaledRange = new Vector2(0f, 1f); // the range to which scaled will be clamped if clampToRange is true
    public Vector2 ScaledRange
    {
        get => scaledRange;
        protected set
        {
            scaledRange = value;
            scaledMax = Mathf.Max(scaledRange.x, scaledRange.y);
            scaledMin = Mathf.Min(scaledRange.x, scaledRange.y);
        }
    }

    float rawMin;
    public float RawMin => rawMin;

    float rawMax;
    public float RawMax => rawMax;

    float scaledMin;
    public float ScaledMin => scaledMin;

    float scaledMax;
    public float ScaledMax => scaledMax;

    [SerializeField] bool isInRange;
    public bool IsInRange
    {
        get => isInRange;
        private set
        {
            if (value != isInRange) // this will be true when the value has crossed the range's threshold, so we know it is either entering or exiting the range
            {
                isInRange = value;

                if (isInRange)
                {
                    RaiseRangeEntered();
                }
                else
                {
                    RaiseRangeExited();
                }
            }
        }
    }

    [Tooltip("Determines which loop to use for updating the driver value")]
    [SerializeField] protected UnityUpdate updateType = UnityUpdate.Standard;

    [Tooltip("Time in seconds to wait between calculations. If set to zero, calculations will occur at the rate of the chosen update type.")]
    [SerializeField] float timeBetweenCalculations;
    public float TimeBetweenCalculations
    {
        get => timeBetweenCalculations;
        protected set
        {
            timeBetweenCalculations = Mathf.Max(value, 0f);
        }
    }

    [Tooltip("Time in seconds since last calculation was performed.")]
    float timeSinceLastCalculation;
    public float TimeSinceLastCalculation => timeSinceLastCalculation;

    #endregion

    #region Events

    /// <summary>
    /// Triggered when the raw value enters the bounds of RawRange
    /// </summary>
    public event EventHandler BoundsEnteredEvent;

    /// <summary>
    /// Triggered when the raw value reaches or exceeds the bounds of RawRange.
    /// </summary>
    public event EventHandler BoundsExitedEvent;

    /// <summary>
    /// Notifies subscribers when the Driver changes from active to inactive and vice versa
    /// </summary>
    public event EventHandler DriverStateChangeEvent;

    /// <summary>
    /// Triggered each time the raw value is changed, as long as it is within RawRange, or clampToRange is false.
    /// </summary>
    public event EventHandler ValueChangedEvent;

    #endregion

    #region Methods

    protected virtual void Awake()
    {
        isInRange = MathUtil.IsInRange(raw, rawRange, false); //set isInRange to match the current state, to prevent missing the first threshold-crossing
    }

    /// <summary>
    /// base.OnEnable() raises the DriverStateChangeEvent, so do all derived-specific behaviour before calling base.
    /// </summary>
    protected virtual void OnEnable()
    {
        if (gameObject.activeInHierarchy)
        {
            active = true;
            RaiseDriverStateChangeEvent();
        }
    }

    /// <summary>
    /// base.OnDisable() raises the DriverStateChangeEvent, so do all derived-specific behaviour before calling base.
    /// </summary>
    protected virtual void OnDisable()
    {
        if (active)
        {
            active = false;
            RaiseDriverStateChangeEvent();
        }
    }

    protected void Update()
    {
        if (updateType == UnityUpdate.Standard)
        {
            AttemptToCalculateDriver();
        }
    }

    protected void FixedUpdate()
    {
        if (updateType == UnityUpdate.Fixed)
        {
            AttemptToCalculateDriver();
        }
    }

    protected void LateUpdate()
    {
        if (updateType == UnityUpdate.Late)
        {
            AttemptToCalculateDriver();
        }
    }

    /// <summary>
    /// Allows a derived Driver to control exactly when it updates
    /// </summary>
    protected virtual void ManualUpdate() 
    {
        CalculateDriverValue();
    }

    /// <summary>
    /// Perform whatever Driver specific type of processing is necessary to calculate the driver value.
    /// </summary>
    protected abstract void CalculateDriverValue();


    private bool AttemptToCalculateDriver()
    {
        bool success = false;
        if (timeSinceLastCalculation >= timeBetweenCalculations)
        {
            CalculateDriverValue();
            timeSinceLastCalculation = 0f;
            success = true;
        }
        else
        {
            // Note that this counter is only incremented while the driver is active
            timeSinceLastCalculation += GetDeltaTime();
        }

        return success;
    }

    protected float GetDeltaTime() => updateType switch
    {
        UnityUpdate.Fixed => Time.fixedDeltaTime,
        UnityUpdate.Late => Time.deltaTime,
        UnityUpdate.Standard => Time.deltaTime,
        _ => throw new ArgumentOutOfRangeException(nameof(UnityUpdate), ($"Unexpected update type: {updateType}"))
    };

    void RaiseDriverStateChangeEvent()
    {
        DriverStateChangeEvent?.Invoke(this, GetNewDriverArgs());
    }

    void RaiseRangeEntered()
    {
        BoundsEnteredEvent?.Invoke(this, GetNewDriverArgs());
        HandleRangeEntered();
    }
    void RaiseRangeExited()
    {
        BoundsExitedEvent?.Invoke(this, GetNewDriverArgs());
        HandleRangeExited();
    }

    void RaiseValueChanged()
    {
        ValueChangedEvent?.Invoke(this, GetNewDriverArgs());
    }

    DriverArgs GetNewDriverArgs()
    {
        return new DriverArgs(raw, rawRange, scaled, scaledRange, this.enabled);
    }

    /// <summary>
    /// Allows derived Drivers to respond to entering the range without requiring them to detect it themselves.
    /// </summary>
    protected virtual void HandleRangeEntered() { }

    /// <summary>
    /// Allows derived Drivers to respond to exiting the range without requiring them to detect it themselves.
    /// </summary>
    protected virtual void HandleRangeExited() { }

    protected virtual void OnValidate() 
    {
        // these properties will ensure that RawMin, RawMax, ScaledMin, ScaledMax are all set properly
        RawRange = rawRange;
        ScaledRange = scaledRange;

        // this property will ensure TimeBetweenCalculations is >= 0
        TimeBetweenCalculations = TimeBetweenCalculations;
    }

    #endregion
}
