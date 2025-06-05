#if USE_LARGE_WORLDS
using Real = System.Double;
using Mathr = FlaxEngine.Mathd;
#else
using Real = float;
using Mathr = FlaxEngine.Mathf;
#endif

using System;
using System.Collections.Generic;
using FlaxEngine;
using Units;
using static Gravity.Constants;

namespace Gravity;

/// <summary>
/// GravitySource2 Script.
/// </summary>
public class GravitySource : Orbiter
{
    #region Editor Properties

    ///<summary>The mass of the GravitySource in kg</summary>
    public float MassKilograms
    {
        get => this.Mass.Kilograms;
        set => this.Mass = Mass.FromKilograms(value);
    }

    public bool SynchronizeMassWithRigidBody { get; set; }

    public RigidBody RigidBody { get; set; }

    /// <summary>The radius at the "surface" of the GravitySource, where the acceleration on other objects from the GravitySource's gravity will equal SurfaceGravity</summary>
    public Real SurfaceRadiusCentimeters
    {
        get => this.SurfaceRadius.Centimeters;
        set => this.SurfaceRadius = Distance.FromCentimeters(value);
    }

    public Real SurfaceGravityMetersPerSecondSquared
    {
        get => this.SurfaceGravity.Distance.Meters / (Real)this.SurfaceGravity.TimeSquared.TotalSeconds;
        set => this.SurfaceGravity = Distance.FromMeters(value) / TimeSpan.FromSeconds(1) / TimeSpan.FromSeconds(1);
    }

    public Real SurfaceGForce
    {
        get => this.SurfaceGravityMetersPerSecondSquared / (Real)9.8;
        set => this.SurfaceGravityMetersPerSecondSquared = value * (Real)9.8;
    }

    public Vector3 GravitationalDirection { get; set; } = Vector3.One;

    public bool AffectedByMutualGravitation { get; set; } = true;

    public bool EnableLogging { get; set; }

    #endregion

    #region Hidden Properties

    [HideInEditor]
    public Mass Mass
    {
        get
        {
            if (this.SynchronizeMassWithRigidBody && this.AreMassesOutOfSync())
            {
                this.mass = Mass.FromKilograms(this.RigidBody.Mass);
            }

            return this.mass;
        }
        set
        {
            this.mass = value;

            if (this.SynchronizeMassWithRigidBody && this.RigidBody != null)
            {
                this.RigidBody.Mass = this.mass.Kilograms;
            }
        }
    }
    [Serialize]
    private Mass mass;

    private bool AreMassesOutOfSync()
    {
        return this.RigidBody != null && Mathf.Abs(this.mass.Kilograms - this.RigidBody.Mass) >= 0.000001;
    }

    [HideInEditor]
    public Acceleration SurfaceGravity
    {
        get => Distance.FromMeters(this.Mass.Kilograms / (Mathr.Pow(this.SurfaceRadius.Meters, 2) / GRAVITATIONAL_CONSTANT)) / TimeSpan.FromSeconds(1) / TimeSpan.FromSeconds(1);
        set => this.UpdateMass(value, this.SurfaceRadius);
    }

    [HideInEditor]
    public Distance SurfaceRadius
    {
        get => this.surfaceRadius;
        set
        {
            Acceleration currentSurfaceGravity = this.SurfaceGravity;
            this.surfaceRadius = value;
            this.UpdateMass(currentSurfaceGravity, value);
        }
    }

    [Serialize]
    private Distance surfaceRadius = Distance.FromMeters(10);

    /// <summary>
    /// surfaceRadius.Meters² * surfaceGravity / G
    /// </summary>
    /// <param name="surfaceGravity"></param>
    /// <param name="surfaceRadius"></param>
    private void UpdateMass(Acceleration surfaceGravity, Distance surfaceRadius)
    {
        this.Mass = Mass.FromKilograms((float)
                    (Mathr.Pow(surfaceRadius.Meters, 2)
                    * (surfaceGravity.Distance.Meters / surfaceGravity.TimeSquared.TotalSeconds)
                    / GRAVITATIONAL_CONSTANT)
        );
    }

    /// <summary>
    /// 
    /// </summary>
    protected HashSet<RigidBody> rigidBodiesInGravity;

    #endregion

    #region Events and Methods

    /// <inheritdoc/>
    public override void OnStart()
    {
        // Here you can add code that needs to be called when script is created, just before the first game update
        base.OnStart();
        this.SetUpRigidBodiesInGravity();
    }

    private void SetUpRigidBodiesInGravity()
    {
        RigidBody[] rigidBodiesInLevel = Level.GetActors<RigidBody>(activeOnly: true);

        this.rigidBodiesInGravity = new HashSet<RigidBody>(rigidBodiesInLevel);
        this.rigidBodiesInGravity.Remove(this.RigidBody);
    }

    /// <inheritdoc/>
    public override void OnEnable()
    {
        // Here you can add code that needs to be called when script is enabled (eg. register for events)

        Level.ActorSpawned += this.OnActorSpawnedInLevel;
        Level.ActorDeleted += this.OnActorDeletedInLevel;
    }

    /// <inheritdoc/>
    public override void OnDisable()
    {
        // Here you can add code that needs to be called when script is disabled (eg. unregister from events)

        Level.ActorSpawned -= this.OnActorSpawnedInLevel;
        Level.ActorDeleted -= this.OnActorDeletedInLevel;

    }
    private void OnActorSpawnedInLevel(Actor actor)
    {
        if (actor is RigidBody rigidBody)
            this.rigidBodiesInGravity.Add(rigidBody);
    }

    private void OnActorDeletedInLevel(Actor actor)
    {
        if (actor is RigidBody rigidBody)
            this.rigidBodiesInGravity.Remove(rigidBody);
    }

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        // Here you can add code that needs to be called every frame
    }

    public override void OnFixedUpdate()
    {
        this.Log(this.Actor.Name + " attracting " + this.rigidBodiesInGravity.Count + " rigid bodies");

        this.AttractAllRigidBodiesInGravity();
    }

    public void Attract(RigidBody rigidBody)
    {
        Vector3 fromThisToRigidBody = rigidBody.Position - this.Actor.Position;

        Force gravitationalForce = this.GetGravitationalForceBetween(rigidBody);

        Vector3 forceVectorToBodyNewtons = fromThisToRigidBody * gravitationalForce.Newtons;

        rigidBody.AddForce(-forceVectorToBodyNewtons);

        if (this.RigidBody != null && this.AffectedByMutualGravitation)
            this.RigidBody.AddForce(forceVectorToBodyNewtons);

        this.Log(this.Actor.Name + " attracting " + rigidBody.Name + " \n with force vector " + -forceVectorToBodyNewtons);
    }

    public void AttractAllRigidBodiesInGravity()
    {
        foreach (RigidBody rigidBody in rigidBodiesInGravity)
        {
            if (rigidBody.EnableGravity && rigidBody.EnableSimulation)
                this.Attract(rigidBody);
        }
    }

    /// <param name="rigidBody"></param>
    /// <returns>the gravitational force between this gravity source and a rigidbody.</returns>
    public Force GetGravitationalForceBetween(RigidBody rigidBody)
    {
        Vector3 fromThisToRigidBody = rigidBody.Position - this.Actor.Position;
        Vector3 fromThisToRigidBodyGravity = fromThisToRigidBody * this.GravitationalDirection;

        Distance distanceScaledAndSquared = Distance.FromCentimeters(fromThisToRigidBodyGravity.LengthSquared);

        // F_g = G * (m1 * m2) / r²
        Force gravitationalForce = Force.FromNewtons(
            GRAVITATIONAL_CONSTANT
            * (this.Mass.Kilograms * rigidBody.Mass)
            / distanceScaledAndSquared.Meters
            );

        return gravitationalForce;
    }

    private void Log(object message)
    {
        if (this.EnableLogging)
            Debug.Log(message);
    }

    #endregion
}
