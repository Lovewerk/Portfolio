using UnityEngine;

public class TimeDriver : Driver
{
    CountDirection direction; // determines whether timer will increment or decrement

    [Tooltip("If true, this script will be disabled when raw meets or exceeds rawRange")]
    [SerializeField] bool deactivateOnRangeExit;
    [SerializeField] bool resetOnEnable;

    [SerializeField] CountDirection initialCountDirection = CountDirection.Forward;
    [SerializeField] LoopBehaviour loopMode = LoopBehaviour.Pingpong;
    public LoopBehaviour LoopMode
    {
        get => loopMode;
        set
        {
            loopMode = value;

            if (!IsInRange)
            {
                ManageLooping(); // ensures that the value will start looping again even if it was at its bounds when LoopMode was set.
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();

        direction = initialCountDirection;

    }

    protected override void OnEnable()
    {
        if(resetOnEnable)
        {
            ResetTime();
        }
        base.OnEnable();
    }

    protected override void CalculateDriverValue()
    {
        float deltaTime;

        // if not calculating this on every frame, we want to use the timeSinceLastCalculation to ensure that the sampled time value is correct.
        if(TimeBetweenCalculations > 0)
        {
            deltaTime = TimeSinceLastCalculation;
        }
        else
        {
            deltaTime = GetDeltaTime();
        }
        Raw += deltaTime * (int)direction;
    }

    protected override void HandleRangeExited()
    {
        ManageLooping();

        if (deactivateOnRangeExit)
        {
            this.enabled = false;
        }
    }

    void ManageLooping()
    {
        switch (loopMode)
        {
            case LoopBehaviour.Pingpong:
                if (Raw >= RawMax)
                {
                    direction = CountDirection.Backward;
                }
                else //if we are not at the max bound, we must be at the min, therefore count up
                {
                    direction = CountDirection.Forward;
                }
                break;

            case LoopBehaviour.Wraparound:
                if (Raw >= RawMax)
                {
                    Raw = RawMin;
                }
                else
                {
                    Raw = RawMax;
                }
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Resets Raw to RawMin if initialCountDirection is forward, otherwise set it to RawMax and set direction to initialCountDirection
    /// </summary>
    public void ResetTime()
    {
        direction = initialCountDirection;

        if(direction == CountDirection.Forward)
        {
            Raw = RawMin;
        }
        else
        {
            Raw = RawMax;
        }
    }

#if UNITY_EDITOR

    protected override void OnValidate()
    {
        base.OnValidate();

        LoopMode = loopMode; // property ensures that looping will continue if loopBehaviour is changed while raw is out of range
    }

#endif

}
