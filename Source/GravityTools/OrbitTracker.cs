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
using FlaxEngine.Utilities;
using Units;
using Units.Vectors;

namespace Gravity;

/// <summary>
/// OrbitTracker Script.
/// </summary>
[ExecuteInEditMode]
public class OrbitTracker : Script
{
    public int StepCount { get; set; } = 10;

    public float TraceRadius { get; set; }

    public double TimeStepSeconds
    {
        get => this.TimeStep.TotalSeconds;
        set => this.TimeStep = TimeSpan.FromSeconds(value);
    }

    [HideInEditor]
    public TimeSpan TimeStep { get; set; } = TimeSpan.FromSeconds(1);
    public bool ReDraw { get; set; }

    private List<VirtualGravitySource> virtualBodies;

    /// <inheritdoc/>
    public override void OnStart()
    {
        // Here you can add code that needs to be called when script is created, just before the first game update

        // this.ReDraw = true;
    }

    private void Initialize()
    {

        Debug.Log("Initializing OrbitTracker");

        Orbiter[] orbiters = Level.GetScripts<Orbiter>();
        string orbitersLog = "";
        orbiters.ForEach(orbiter => orbitersLog += orbiter.GetNamePath() + ", ");
        Debug.Log("Orbiters: " + orbiters.Length + "; \n" + orbitersLog);

        this.virtualBodies = new List<VirtualGravitySource>();

        // Initialize virtual bodies        
        foreach (Orbiter orbiter in orbiters)
        {
            List<GravitySource> gravitySources = GetChildGravitySources(orbiter.Actor);
            foreach (GravitySource gravitySource in gravitySources)
            {
                VirtualGravitySource virtualBody = new VirtualGravitySource(gravitySource);
                virtualBodies.Add(virtualBody);
            }
        }

        Debug.Log("OrbitTracker Initialized. VirtualBodies count: " + virtualBodies.Count);
    }

    private static List<GravitySource> GetChildGravitySources(Actor actor)
    {
        List<GravitySource> gravitySources = new List<GravitySource>(actor.GetScripts<GravitySource>());

        foreach (Actor child in actor.Children)
        {
            gravitySources.AddRange(GetChildGravitySources(child));
        }

        return gravitySources;
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


    private int stepsTaken = 0;
    /// <inheritdoc/>
    public override void OnUpdate()
    {
        if (!this.ReDraw)
            return;

        DrawAllSteps();

        if (stepsTaken == this.StepCount)
        {
            stepsTaken = 0;
            this.ReDraw = false;
        }
    }

    private void DrawAllSteps()
    {
        if (stepsTaken == 0)
            this.Initialize();

        if (stepsTaken < this.StepCount)
        {
            Debug.Log("OrbitTracker handling step " + (stepsTaken + 1) + " out of " + this.StepCount);
            foreach (VirtualGravitySource virtualGravitySource in this.virtualBodies)
            {
                HashSet<VirtualGravitySource> otherVirtualGravitySources = new HashSet<VirtualGravitySource>(this.virtualBodies);
                otherVirtualGravitySources.Remove(virtualGravitySource);

                virtualGravitySource.Step(otherVirtualGravitySources, this.TimeStep, this.TraceRadius);
            }

            stepsTaken++;
        }
    }

}

class VirtualGravitySource
{
    public string name;

    public Distance3 position;
    public Velocity3 velocity;
    public Acceleration3 acceleration;
    public Mass mass;

    public Vector3 gravitationalDirection;

    public Color drawColor;

    public VirtualGravitySource(GravitySource gravitySource)
    {
        this.name = gravitySource.GetNamePath() + "Virtual";

        this.mass = gravitySource.Mass;
        this.position = Distance3.FromCentimeters(gravitySource.Actor.Position);
        this.gravitationalDirection = gravitySource.GravitationalDirection;
        this.velocity = gravitySource.InitialVelocity;
        this.drawColor = gravitySource.OrbitPreviewDrawColor;

        this.acceleration = Acceleration3.FromDistanceAndTimeSquared(Distance3.FromMeters(Vector3.Zero), TimeSpan.FromSeconds(1));

        Debug.Log("Created " + this.GetLogString());

    }

    private string GetLogString()
    {
        return this.name
            + ": position m - " + this.position.Meters
            + "; velocity m/s - " + this.velocity.Distance.Meters
            + "; acceleration m/s^2 - " + this.acceleration.Distance.Meters / (Real)this.acceleration.TimeSquared.TotalSeconds
            + "; mass kg - " + this.mass.Kilograms
            + "; drawColor: - " + this.drawColor;
    }


    public void Step(HashSet<VirtualGravitySource> otherVirtualBodies, TimeSpan timeStep, float traceRadius)
    {
        Debug.Log("Started Step " + this.name + " other VirtualBodies " + otherVirtualBodies.Count + " timeStep s " + timeStep.TotalSeconds);

        Acceleration3 deltaAcceleration = this.CalculateAccelerationFromGravity(otherVirtualBodies);
        this.acceleration += deltaAcceleration;

        Velocity3 deltaVelocity = this.acceleration * timeStep;
        this.velocity += deltaVelocity;

        Distance3 deltaPosition = this.velocity * timeStep;
        Distance3 nextPosition = this.position + deltaPosition;

        Vector3 fromPositionToNextPosition = (nextPosition - this.position).Centimeters;

        // Debug.Log("Drawing tube at " + this.position.Centimeters
        //     + " in direction " + Quaternion.FromDirection(fromPositionToNextPosition)
        //     + " of length " + fromPositionToNextPosition.Length);

        this.position = nextPosition;

        Debug.Log(this.name + " Drawing sphere at " + this.position.Centimeters + " of radius " + traceRadius);
        DebugDraw.DrawSphere(new BoundingSphere(this.position.Centimeters, traceRadius), this.drawColor, duration: (Real)120);

        Debug.Log("End Step " + " delta Position m " + deltaPosition.Meters
            + " deltaVelocity m/s " + deltaVelocity.Distance.Meters
            + " deltaAcceleration m/s^2 " + deltaAcceleration.Distance.Meters
            + this.GetLogString());
    }

    private Acceleration3 CalculateAccelerationFromGravity(HashSet<VirtualGravitySource> otherVirtualBodies)
    {
        Acceleration3 accelerationFromGravityXYZ = Distance3.FromCentimeters(Vector3.Zero) / TimeSpan.FromSeconds(1) / TimeSpan.FromSeconds(1);

        foreach (VirtualGravitySource other in otherVirtualBodies)
        {
            Acceleration3 accelerationFromOther = CalculateAccelerationDueToGravityXYZWith(other);
            accelerationFromGravityXYZ += accelerationFromOther;
        }
        return accelerationFromGravityXYZ;
    }

    private Acceleration3 CalculateAccelerationDueToGravityXYZWith(VirtualGravitySource other)
    {
        Force gravityWithOther = this.GetGravitationalForceWith(other);

        Acceleration accelerationFromGravity = gravityWithOther / this.mass;

        Vector3 directionToOther = (other.position - this.position).Centimeters.Normalized;

        Real accelerationMetersPerSecondSquared = accelerationFromGravity.Distance.Meters / (Real)accelerationFromGravity.TimeSquared.TotalSeconds;
        Acceleration3 accelerationFromGravityXYZ = Distance3.FromMeters(directionToOther * accelerationMetersPerSecondSquared)
            / TimeSpan.FromSeconds(1) / TimeSpan.FromSeconds(1);
        return accelerationFromGravityXYZ;
    }

    public Force GetGravitationalForceWith(VirtualGravitySource other)
    {
        Distance3 fromThisToOther = other.position - this.position;
        Distance3 fromThisToRigidBodyGravity = Distance3.FromMeters(fromThisToOther.Meters * other.gravitationalDirection);

        Distance distanceScaledAndSquared = Distance.FromCentimeters(fromThisToRigidBodyGravity.Centimeters.LengthSquared);

        // F_g = G * (m1 * m2) / r²
        Force gravitationalForce = Force.FromNewtons(
            Constants.GRAVITATIONAL_CONSTANT
            * (this.mass.Kilograms * other.mass.Kilograms)
            / distanceScaledAndSquared.Meters
            );

        return gravitationalForce;
    }

}
