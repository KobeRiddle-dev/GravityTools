#if USE_LARGE_WORLDS
using Real = System.Double;
using Mathr = FlaxEngine.Mathd;
#else
using Real = float;
using Mathr = FlaxEngine.Mathf;
#endif

using System;
using FlaxEngine;
using Units.Vectors;
using Units;
using FlaxEngine.Utilities;

namespace Gravity;

/// <summary>
/// Orbiter Script. Add to objects to give them an initial velocity and thus, an orbit.
/// </summary>
public class Orbiter : Script
{
    public Vector3 InitialVelocityMetersPerSecond
    {
        get => this.InitialVelocity.Distance.Meters / (Real)this.InitialVelocity.Time.TotalSeconds;
        set => this.InitialVelocity = Distance3.FromMeters(value) / TimeSpan.FromSeconds(1);
    }

    [HideInEditor]
    public Velocity3 InitialVelocity { get; set; } = Distance3.FromMeters(Vector3.Zero) / TimeSpan.FromSeconds(1);

    [HideInEditor]
    public Distance3 DisplacementFromTarget { get; private set; }
    public Color OrbitPreviewDrawColor { get; set; } = Color.White;


    /// <inheritdoc/>
    public override void OnStart()
    {
        RigidBody[] rigidBodies = this.Actor.GetChildren<RigidBody>();
        rigidBodies.ForEach(rigidbody => rigidbody.LinearVelocity = InitialVelocity.Distance.Centimeters / (Real)InitialVelocity.Time.TotalSeconds);
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
}
