#if USE_LARGE_WORLDS
using Real = System.Double;
using Mathr = FlaxEngine.Mathd;
#else
using Real = System.Single;
using Mathr = FlaxEngine.Mathf;
#endif

using FlaxEngine;
using System.ComponentModel;
using Units;
using System;
using Gravity;

/// <summary>
/// RigidBodyController Script.
/// </summary>
public class RigidBodyController : SelfRightingBody
{

    /// <summary>
    /// Camera rotation smoothing factor
    /// </summary>
    [DefaultValue(20.0f)]
    public float CameraSmoothing { get; set; } = 20.0f;

    /// <summary>
    /// The maximum on-foot movement speed in cm/s
    /// </summary>
    public Vector3 MaxFootSpeed { get; set; } = Vector3.One * 1000;

    /// <summary>
    /// The maximum on-foot movement acceleration in cm/s
    /// </summary>
    public Vector3 MaxFootAcceleration { get; set; } = Vector3.One * 1000;

    /// <summary>
    /// Layers upon which the controller will be considered grounded
    /// </summary>
    public LayersMask GroundLayers { get; set; }

    /// <summary>
    /// Whether or not the player's feet are touching the ground
    /// </summary>
    [ShowInEditor]
    public bool IsGrounded
    {
        get
        {
            return Physics.RayCast(this.Actor.Position, this.Actor.Transform.Down, 5, GroundLayers);
            // return Physics.SphereCast(center: this.Actor.Position, radius: 10, direction: this.RigidBody.Transform.Down, layerMask: this.GroundLayers, maxDistance: 50);
        }
    }

    public int Foo
    {
        get
        {
            if (this.IsGrounded)
                return 0;
            else
                return 1;
        }
    }

    private float pitch = 0;

    private float yaw = 0;

    private float roll = 0;

    // Prefab components
    // TODO: update with RequireChildActor attribute

    // private RigidBody rigidBody;
    private Collider collider;

    private Camera viewCamera;

    private StaticModel head;


    /// <inheritdoc/>
    public override void OnStart()
    {
        // Here you can add code that needs to be called when script is created, just before the first game update

        this.RigidBody = this.Actor.As<RigidBody>();
        this.collider = this.Actor.GetChild<Collider>();
        this.viewCamera = this.Actor.GetChild<Camera>();

        StaticModel[] staticModels = this.Actor.GetChildren<StaticModel>();
        foreach (StaticModel staticModel in staticModels)
        {
            if (staticModel.Name == "Head")
            {
                this.head = staticModel;
                break;
            }
        }

    }

    /// <inheritdoc/>
    public override void OnEnable()
    {
        SetUpCursor();
        // register for events
    }

    /// <inheritdoc/>
    public override void OnDisable()
    {
        // unregister for events
    }

    private static void SetUpCursor()
    {
        Screen.CursorVisible = false;
        Screen.CursorLock = CursorLockMode.Locked;
    }


    /// <inheritdoc/>
    public override void OnUpdate()
    {
    }

    /// <inheritdoc/>
    public override void OnFixedUpdate()
    {
        Debug.Log("Grounded?: " + this.IsGrounded);

        this.UpdateRotation();
        this.Move();
    }

    private void UpdateRotation()
    {
        GetRotationInput();
        float rotationFactor = Mathf.Saturate(CameraSmoothing * Time.DeltaTime);
        if (this.IsInGravity)
            RotateHead(rotationFactor);
        RotateBody(rotationFactor);
    }

    private void GetRotationInput()
    {
        Float2 viewInputDelta = new Float2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        this.pitch = Mathf.Clamp(pitch + viewInputDelta.Y, -88, 88);
        this.yaw += viewInputDelta.X;
    }


    private void RotateHead(float rotationFactor)
    {
        // FIXME: the lerping is probably breaking things. Lerping Euler Angles breaks stuff.
        this.head.LocalOrientation = Quaternion.Lerp(this.head.LocalOrientation, Quaternion.Euler(this.pitch, 0, 0), rotationFactor);
    }


    private void RotateBody(float rotationFactor)
    {
        // TODO: Make this more readable
        if (this.IsInGravity)
        {
            this.SelfRight();
            this.Actor.Orientation = Quaternion.Lerp(this.Actor.Orientation, Quaternion.Euler(0, this.yaw, this.roll), rotationFactor);
        }
        else
        {
            this.Actor.Orientation = Quaternion.Lerp(this.Actor.Orientation, Quaternion.Euler(this.pitch, this.yaw, this.roll), rotationFactor);
        }

        // Update constraints to try and fix the leaning
        if (this.IsGrounded)
            this.RigidBody.Constraints = RigidbodyConstraints.LockRotationX | RigidbodyConstraints.LockRotationZ;
        else
            this.RigidBody.Constraints &= RigidbodyConstraints.None;
    }

    private void Move()
    {
        Vector3 movementDirection = GetMovementInputDirection();
        this.RigidBody.AddRelativeForce(movementDirection * this.MaxFootAcceleration, mode: ForceMode.Acceleration);

        if (this.RigidBody.LinearVelocity.Absolute.Length > this.MaxFootSpeed.Length)
            this.RigidBody.LinearVelocity = Vector3.Clamp(this.RigidBody.LinearVelocity, min: -this.MaxFootSpeed, max: this.MaxFootSpeed);
    }

    /// <returns>the movement input direction, in local space</returns>
    private Vector3 GetMovementInputDirection()
    {
        Vector3 movementDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        return movementDirection;
    }
}
