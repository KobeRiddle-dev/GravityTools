#if USE_LARGE_WORLDS
using Real = System.Double;
using Mathr = FlaxEngine.Mathd;
#else
using Real = System.Single;
using Mathr = FlaxEngine.Mathf;
#endif

using System.Collections.Generic;
using FlaxEngine;
using Units;
using Units.Vectors;

namespace Gravity;

/// <summary>
/// A body which rights itself when in gravity.
/// </summary>
public class SelfRightingBody : Script
{
    /// <summary>
    /// The GravitySources whose volumes the player is currently within.
    /// </summary>
    public List<GravitySource> GravitySources { get; private set; } = new List<GravitySource>();

    /// <summary>
    /// The strength with which the this rights itself. // TODO: change to angular velocity
    /// </summary>
    public float RightingStrength { get; set; } = 0.5f;

    /// <summary>
    /// Whether this rights itself when in gravity
    /// </summary>
    public bool SelfRightWhenInGravity { get; set; }

    public Real MinimumAccelerationOfGravityMetersPerSecondSquared
    {
        get { return this.MinimumAccelerationOfGravity.Distance.Meters / (Real)this.MinimumAccelerationOfGravity.TimeSquared.TotalSeconds;}
    }

    [HideInEditor]
    public Acceleration MinimumAccelerationOfGravity { get; set; }

    /// <summary>
    /// Whether this is in the gravity of a GravitySource
    /// </summary>
    [ReadOnly]
    public bool IsInGravity
    {
        get
        {
            return this.GravitySources.Count > 0 || this.RigidBody.PhysicsScene.Gravity.Length > 0;
        }
    }

    /// <summary>
    /// A connected RigidBody
    /// </summary>
    protected RigidBody RigidBody
    {
        get
        {
            if (!this.rigidBody)
                this.rigidBody = this.Actor.As<RigidBody>();
            return this.rigidBody;
        }
        set => this.rigidBody = value;
    }
    private RigidBody rigidBody;

    /// <inheritdoc/>
    public override void OnStart()
    {
        this.RigidBody = this.Actor.As<RigidBody>();

        // Here you can add code that needs to be called when script is created, just before the first game update
    }

    /// <inheritdoc/>
    public override void OnEnable()
    {
        // Here you can add code that needs to be called when script is enabled (eg. register for events)
    }

    /// <inheritdoc/>
    public override void OnDisable()
    {
        // Here you can add code that needs to be called when script is disabled (eg. unregister from events)
    }

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        // Here you can add code that needs to be called every frame
    }

    /// <inheritdoc/>
    public override void OnFixedUpdate()
    {
        if (this.SelfRightWhenInGravity && this.IsInGravity)
            this.SelfRight();
    }

    /// <inheritdoc/>
    public override void OnDebugDraw()
    {
        DebugDraw.DrawRay(this.Actor.Position, this.GetDirectionOfStrongestGravity(), Color.PaleGreen, length: 20);
    }

    /// <summary>
    /// Points the bottom of this GravityObject towards the strongest gravitational force
    /// </summary>
    public void SelfRight()
    {
        Vector3 gravityDown = this.GetDirectionOfStrongestGravity();

        Quaternion rightedOrientation = Quaternion.GetRotationFromTo(this.Actor.Transform.Down, gravityDown, Vector3.Zero) * this.Actor.Orientation;

        this.Actor.Orientation = Quaternion.Lerp(this.Actor.Orientation, rightedOrientation, RightingStrength);
    }

    /// <returns>A normalized Vector3 representing the direction of the strongest gravitational pull</returns>
    public Vector3 GetDirectionOfStrongestGravity()
    {
        Vector3 directionOfStrongestGravity = Vector3.Down;
        Force3 strongestGravity = Force3.FromNewtons(this.Actor.As<RigidBody>().PhysicsScene.Gravity * this.RigidBody.Mass);

        foreach (GravitySource gravitySource in this.GravitySources)
        {
            Vector3 directionFromThisToSource = (gravitySource.Actor.Position - this.Actor.Position).Normalized;
            Force3 gravityTowardsSource = Force3.FromNewtons(directionFromThisToSource * gravitySource.GetGravitationalForceBetween(this.Actor.As<RigidBody>()).Newtons);

            if (gravityTowardsSource.Length.Newtons > strongestGravity.Length.Newtons)
            {
                strongestGravity = gravityTowardsSource;
                directionOfStrongestGravity = directionFromThisToSource;
            }
        }

        return directionOfStrongestGravity;
    }
}
