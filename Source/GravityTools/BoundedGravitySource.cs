
using System.Collections.Generic;
using System.Linq;
using FlaxEngine;

namespace Gravity;

public class BoundedGravitySource : GravitySource
{
    public Collider BoundingVolume { get; set; }

    /// <inheritdoc/>
    public override void OnStart()
    {
        this.rigidBodiesInGravity = new HashSet<RigidBody>();

        if (this.BoundingVolume != null)
            this.BoundingVolume.IsTrigger = true;
    }

    public override void OnEnable()
    {
        this.BoundingVolume.TriggerEnter += this.OnObjectEnterGravity;
        this.BoundingVolume.TriggerExit += this.OnObjectExitGravity;
    }

    /// <inheritdoc/>
    public override void OnDisable()
    {
        // Here you can add code that needs to be called when script is disabled (eg. unregister from events)
        this.BoundingVolume.TriggerEnter -= this.OnObjectEnterGravity;
        this.BoundingVolume.TriggerExit -= this.OnObjectExitGravity;
    }

    private void OnObjectEnterGravity(PhysicsColliderActor collider)
    {
        if (collider.AttachedRigidBody == null)
            return;

        if (this.Actor.GetChildren<PhysicsColliderActor>().Contains(collider))
            return;

        if (collider.AttachedRigidBody.TryGetScript<SelfRightingBody>(out SelfRightingBody selfRightingBody))
            selfRightingBody.GravitySources.Add(this);

        this.rigidBodiesInGravity.Add(collider.AttachedRigidBody);
        
        Debug.Log("Collider:" + collider.Name + "; AttachedRigidBody: " + collider.AttachedRigidBody.Name);
        Debug.Log("rigidBodies: " + this.rigidBodiesInGravity.ToArray());
    }

    private void OnObjectExitGravity(PhysicsColliderActor collider)
    {
        if (collider.AttachedRigidBody == null)
            return;

        if (collider.AttachedRigidBody.TryGetScript<SelfRightingBody>(out SelfRightingBody selfRightingBody))
            selfRightingBody.GravitySources.Remove(this);

        this.rigidBodiesInGravity.Remove(collider.AttachedRigidBody);
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
    }
}