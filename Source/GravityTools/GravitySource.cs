using System;
using System.Collections.Generic;
using FlaxEngine;
using System.Linq;
using static GravityTools.Constants;
using GravityTools.Units;


#if USE_LARGE_WORLDS
using Real = System.Double;
using Mathr = FlaxEngine.Mathd;
#else
using Real = System.float;
using Mathr = FlaxEngine.Mathf;
#endif

namespace GravityTools;

/// <summary>
/// GravitySource Script.
/// </summary>
public class GravitySource : Script
{
    /// <summary>
    /// The Surface gravitational force measured in Gs
    /// </summary>
    public Real GForce
    {
        get => SurfaceGravity / 9.8;
        set => SurfaceGravity = 9.8 * value;
    }
    public Vector3 GravitationalDirection { get; set; } = Vector3.One;

    /// <summary>
    /// The length of the gravitational acceleration vector at the surface, in m/s^2. Earth's is 9.8 m/s^2. 
    /// Changing this will update the mass accordingly.
    /// </summary>
    public Real SurfaceGravity
    {
        get
        {
            return this.Mass.Kilograms / (Mathr.Pow(this.SurfaceRadius.Meters, 2) / GRAVITATIONAL_CONSTANT);
        }
        set
        {
            UpdateMassBasedOnGravity(value);
        }
    }

    /// <summary>
    /// The radius at the "surface" of the GravitySource, where the acceleration on other objects from the GravitySource's gravity will equal SurfaceGravity.
    /// Measured in cm.
    /// </summary>
    public Real SurfaceRadiusCentimeters
    {
        get => this.SurfaceRadius.Centimeters;
        set => this.SurfaceRadius = Distance.FromCentimeters(value);
    }

    /// <summary>
    /// The radius at the "surface" of the GravitySource, where the acceleration on other objects from the GravitySource's gravity will equal SurfaceGravity.
    /// </summary>
    [HideInEditor]
    public Distance SurfaceRadius
    {
        get => surfaceRadius;
        set
        {
            surfaceRadius = value;
            this.UpdateMassBasedOnGravity(this.SurfaceGravity);
        }
    }
    private Distance surfaceRadius = Distance.FromMeters(1);

    /// <summary>
    /// The Mass of the GravitySource in Kilograms.
    /// Changing this will update SurfaceGravity accordingly.
    /// If MassUpdatesToRigidBody is enabled, the mass of the rigidbody associated with the GravitySource will also be updated upon changes to Mass.
    /// </summary>
    public Real MassKilograms
    {
        get => this.Mass.Kilograms;
        set => this.Mass = Mass.FromKilograms(value);
    }

    /// <summary>
    /// The Mass of the GravitySource.
    /// Changing this will update SurfaceGravity accordingly.
    /// If MassUpdatesToRigidBody is enabled, the mass of the rigidbody associated with the GravitySource will also be updated upon changes to Mass.
    /// </summary>
    [HideInEditor]
    public Mass Mass
    {
        get
        {
            if (this.UseRigidBodyMass && this.rigidBody != null && this.Mass.Kilograms != this.rigidBody.Mass)
            {
                this.mass = Mass.FromKilograms(this.rigidBody.Mass);
            }

            return this.mass;
        }
        set
        {
            this.mass = value;
            if (this.UseRigidBodyMass && this.rigidBody != null)
                this.rigidBody.Mass = (float)this.mass.Kilograms;
        }
    }
    private Mass mass;

    /// <summary>
    /// Determines whether a rigidbody the GravitySource script is attached to will have its mass synchronized with the GravitySource's mass. Turning this off is useful if you wish to create less realistic simulations, i.e. a planet with high gravitational force that is small in mass as far as other GravitySources are concerned.
    /// </summary>
    public bool UseRigidBodyMass { get; set; } = false;

    /// <summary>
    /// If true, and the GravitySource script is attached to a rigidbody, the rigidbody will be mutually attracted to other rigidbodies in it's gravitational volume.
    /// </summary>
    public bool AffectedByMutualGravitation { get; set; } = false;

    private List<RigidBody> rigidBodiesInGravity;

    public RigidBody rigidBody;
    public Collider GravityVolume;

    /// <inheritdoc/>
    public override void OnStart()
    {
        // Here you can add code that needs to be called when script is created, just before the first game update
        // this.rigidBody = this.Actor.FindActor<RigidBody>();
        // this.gravitationalBoundCollider = this.Actor.GetChild<Collider>();

        // Debug.Log("G: " + GRAVITATIONAL_CONSTANT);
        this.rigidBodiesInGravity = new List<RigidBody>();

        if (this.GravityVolume != null)
            this.GravityVolume.IsTrigger = true;
    }

    /// <inheritdoc/>
    public override void OnEnable()
    {
        // Here you can add code that needs to be called when script is enabled (eg. register for events)
        this.GravityVolume.TriggerEnter += this.OnObjectEnterGravity;
        this.GravityVolume.TriggerExit += this.OnObjectExitGravity;
    }

    /// <inheritdoc/>
    public override void OnDisable()
    {
        // Here you can add code that needs to be called when script is disabled (eg. unregister from events)
        this.GravityVolume.TriggerEnter -= this.OnObjectEnterGravity;
        this.GravityVolume.TriggerExit -= this.OnObjectExitGravity;
    }

    public override void OnDebugDraw()
    {
        if (this.GravityVolume is BoxCollider)
            DebugDraw.DrawWireBox(this.GravityVolume.Box, Color.PaleGreen);
        else if (this.GravityVolume is SphereCollider)
            DebugDraw.DrawWireSphere(this.GravityVolume.Sphere, Color.PaleGreen);
    }

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        // Here you can add code that needs to be called every frame
    }

    public override void OnFixedUpdate()
    {
        this.AttractAllRigidBodiesInGravity();
    }

    private void OnObjectEnterGravity(PhysicsColliderActor collider)
    {
        if (collider.AttachedRigidBody == null)
            return;
        if (this.Actor.GetChildren<PhysicsColliderActor>().Contains(collider))
            return;

        // Debug.Log("Object entered " + this.Actor.Name + "'s gravity: " + collider.AttachedRigidBody.Name);

        if (collider.AttachedRigidBody.TryGetScript<SelfRightingBody>(out SelfRightingBody gravityObject))
            gravityObject.GravitySources.Add(this);

        if (collider.AttachedRigidBody != null)
            this.rigidBodiesInGravity.Add(collider.AttachedRigidBody);
    }

    private void OnObjectExitGravity(PhysicsColliderActor collider)
    {
        // Debug.Log("Object exited " + this.Actor.Name + "'s gravity: " + collider.AttachedRigidBody.Name);

        SelfRightingBody gravityObject;
        if (collider.AttachedRigidBody.TryGetScript<SelfRightingBody>(out gravityObject))
            gravityObject.GravitySources.Remove(this);

        if (collider.AttachedRigidBody != null)
            this.rigidBodiesInGravity.Remove(collider.AttachedRigidBody);
    }

    /// <summary>
    /// Calculates and updates the mass of the GravitySource based on its surface gravity and surface radius.
    /// this.Mass = r^2 * surfaceGravity / G
    /// </summary>
    public void UpdateMassBasedOnGravity(Real surfaceGravity)
    {
        Real surfaceRadiusSquared = Mathr.Pow(this.SurfaceRadius.Meters, 2);
        this.Mass = Mass.FromKilograms((float)(surfaceGravity * surfaceRadiusSquared / GRAVITATIONAL_CONSTANT));
    }

    // /// <summary>
    // /// Calculates and updates the surface gravity of the GravitySource based on its mass and surface radius.
    // /// surfaceGravity = (G * this.Mass) / r^2
    // /// </summary>
    // public void UpdateGravityBasedOnMass()
    // {
    //     this.surfaceGravity = (float)(GRAVITATIONAL_CONSTANT * this.Mass.Kilograms / (SurfaceRadius * SurfaceRadius));
    // }

    private void AttractAllRigidBodiesInGravity()
    {
        foreach (RigidBody rigidBody in rigidBodiesInGravity)
        {
            if (rigidBody.EnableGravity)
                this.Attract(rigidBody);
        }
    }

    /// <summary>
    /// Attracts a RigidBody, using the gravitational formula and the mass of this and the RigidBody.
    /// </summary>
    /// <param name="rigidBody"></param>
    private void Attract(RigidBody rigidBody)
    {
        // if (rigidBody.GetNamePath() == "Scene/RigidBodyPlayer")
        //     Debug.Log("Attracting object: " + rigidBody.GetNamePath() + " with acceleration " + this.GetGravitationalForceBetween(rigidBody) / this.Mass + " and a vector " + this.GetGravitationalVectorTowards(rigidBody));

        rigidBody.AddForce(-this.GetGravitationalVectorTowards(rigidBody), mode: ForceMode.Force);

        if (this.rigidBody != null && this.AffectedByMutualGravitation)
            this.rigidBody.AddForce(this.GetGravitationalVectorTowards(rigidBody), mode: ForceMode.Force);
    }

    /// <summary>
    /// Gets the gravitational force between this gravity source and a rigidbody.
    /// </summary>
    /// <param name="rigidBody"></param>
    /// <returns></returns>
    public Real GetGravitationalForceBetween(RigidBody rigidBody)
    {
        Vector3 fromThisToRigidBody = rigidBody.Position - this.Actor.Position;
        Vector3 fromThisToRigidBodyGravity = fromThisToRigidBody * this.GravitationalDirection;

        // F_g = G * (m1 * m2) / r^2
        Real gravitationalForce = GRAVITATIONAL_CONSTANT * (this.Mass.Kilograms * rigidBody.Mass) / fromThisToRigidBodyGravity.LengthSquared;
        return gravitationalForce;
    }

    /// <summary>
    /// Gets the gravitational acceleration for a Rigidbody. Only the position of the RigidBody affects the return value
    /// </summary>
    /// <param name="rigidBody"></param>
    /// <returns>the gravitational acceleration for <paramref name="rigidBody"/>.</returns>
    public Real GetGravitationalAccelerationFor(RigidBody rigidBody)
    {
        Vector3 fromThisToRigidBody = rigidBody.Position - this.Actor.Position;
        Vector3 fromThisToRigidBodyGravity = fromThisToRigidBody * this.GravitationalDirection;

        // F_g = G * (m1 * m2) / r^2
        Real gravitationalForce = GRAVITATIONAL_CONSTANT * this.Mass.Kilograms / Distance.FromCentimeters(fromThisToRigidBodyGravity.LengthSquared).Meters;
        return gravitationalForce;
    }

    /// <summary>
    /// Gets the gravitational force between this gravity source and a rigidbody.
    /// </summary>
    /// <param name="rigidBody"></param>
    /// <returns>the gravitational vector towards <paramref name="rigidBody"/>.</returns>
    public Vector3 GetGravitationalVectorTowards(RigidBody rigidBody)
    {
        Vector3 fromThisToRigidBody = rigidBody.Position - this.Actor.Position;
        return this.GetGravitationalForceBetween(rigidBody) * fromThisToRigidBody.Normalized * this.GravitationalDirection;
    }
}
