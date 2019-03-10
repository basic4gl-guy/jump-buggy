using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(RacetrackCarState))]
public class CarDebugger : MonoBehaviour
{
    public float MaxSpeed = 150.0f / MathUtil.MilesPerKM * 1000.0f / 3600.0f;
    public float MaxAccel = 10.0f;
    public float MaxBrake = 20.0f;
    public float ResetSpeed = 7.0f;

    private bool gotInfo = false;
    private float prevSpeed = 0.0f;

    private Rigidbody rigidBody;
    private RacetrackCarState carState;

    private void Awake()
    {
        // get the car controller
        rigidBody = GetComponent<Rigidbody>();
        carState = GetComponent<RacetrackCarState>();
    }

    private void Start()
    {
        this.gotInfo = false;
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            rigidBody.velocity = rigidBody.transform.TransformVector(Vector3.forward * ResetSpeed);
            rigidBody.angularVelocity = Vector3.zero;
        }

    }

    private void FixedUpdate()
    {
        var state = carState.State;
        if (!state.IsAboveRoad)
        {
            gotInfo = false;
            return;
        }

        Vector3 segForward = state.TrackFromSeg.MultiplyVector(Vector3.forward);
        float gradient = segForward.normalized.y;
        float speed = rigidBody.transform.InverseTransformVector(rigidBody.velocity).z;

        if (gotInfo)
        {
            float accel = (speed - prevSpeed) / Time.fixedDeltaTime;
            Debug.Log(string.Format("Gradient: {0:0.00}, Speed: {1:0.00}, Accel: {2:0.00}, Adj-Speed: {3:0.00}, Adj-Accel: {4:0.00}, Adj-Brake: {5:0.00}", gradient, speed, accel, speed / MaxSpeed, accel / MaxAccel, -accel / MaxBrake));
        }

        prevSpeed = speed;
        gotInfo = true;
    }
}
