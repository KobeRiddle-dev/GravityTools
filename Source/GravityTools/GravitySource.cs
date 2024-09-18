#if USE_LARGE_WORLDS
using Real = System.Double;
using Mathr = FlaxEngine.Mathd;
#else
using Real = System.Single;
using Mathr = FlaxEngine.Mathf;
#endif

using System;
using System.Collections.Generic;
using FlaxEngine;
using System.Linq;
using static GravityTools.Constants;
using GravityTools.Units;

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
        get => this.SurfaceGravity / 9.8f;
        set => this.SurfaceGravity = 9.8f * value;
    }

    /// <summary>
    /// Controls the direction of the gravitational field
    /// </summary>
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
        get => this.surfaceRadius;
        set
        {
            this.surfaceRadius = value;
            this.UpdateMassBasedOnGravity(this.SurfaceGravity);
        }
    }
    private Distance surfaceRadius = Distance.FromMeters(1);

    /// <summary>
    /// The Mass of the GravitySource in Kilograms.
    /// Changing this will update SurfaceGravity accordingly.
    /// If MassUpdatesToRigidBody is enabled, the mass of the rigidbody associated with the GravitySource will also be updated upon changes to Mass.
    /// </summary>
    public float MassKilograms
    {
        get => (float)this.Mass.Kilograms;
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
            if (this.UseRigidBodyMass && this.RigidBody != null && this.Mass.Kilograms != this.RigidBody.Mass)
            {
                this.mass = Mass.FromKilograms(this.RigidBody.Mass);
            }

            return this.mass;
        }
        set
        {
            this.mass = value;
            if (this.UseRigidBodyMass && this.RigidBody != null)
                this.RigidBody.Mass = (float)this.mass.Kilograms;
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


    /// <summary>
    /// 
    /// </summary>
    public RigidBody RigidBody { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Collider GravityVolume { get; set; }

    /// <inheritdoc/>
    public override void OnStart()
    {
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

        Debug.Log("Object entered " + this.Actor.Name + "'s gravity: " + collider.AttachedRigidBody.Name);

        if (collider.AttachedRigidBody.TryGetScript<SelfRightingBody>(out SelfRightingBody gravityObject))
            gravityObject.GravitySources.Add(this);

        if (collider.AttachedRigidBody != null)
            this.rigidBodiesInGravity.Add(collider.AttachedRigidBody);
    }

    private void OnObjectExitGravity(PhysicsColliderActor collider)
    {
        Debug.Log("Object exited " + this.Actor.Name + "'s gravity: " + collider.AttachedRigidBody.Name);

        if (collider.AttachedRigidBody.TryGetScript<SelfRightingBody>(out SelfRightingBody gravityObject))
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
        Debug.Log("Attracting object: " + rigidBody.GetNamePath() + " with acceleration " + this.GetGravitationalAccelerationFor(rigidBody) + " and a vector " + this.GetGravitationalVectorTowards(rigidBody));

        rigidBody.AddForce(-this.GetGravitationalVectorTowards(rigidBody), mode: ForceMode.Force);

        if (this.RigidBody != null && this.AffectedByMutualGravitation)
            this.RigidBody.AddForce(this.GetGravitationalVectorTowards(rigidBody), mode: ForceMode.Force);
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
        Real gravitationalForce = GRAVITATIONAL_CONSTANT * (this.Mass.Kilograms * rigidBody.Mass) / Distance.FromCentimeters(fromThisToRigidBodyGravity.LengthSquared).Meters;
        return gravitationalForce;
    }

    /// <summary>
    /// Gets the gravitational acceleration for a Rigidbody. Only the position of the RigidBody affects the return value
    /// </summary>
    /// <param name="rigidBody"></param>
    /// <returns>the gravitational acceleration for <paramref name="rigidBody"/>.</returns>
    public Real GetGravitationalAccelerationFor(RigidBody rigidBody)
    {
        Real force = this.GetGravitationalForceBetween(rigidBody);

        Real acceleration = force / rigidBody.Mass;
        
        return acceleration;
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
