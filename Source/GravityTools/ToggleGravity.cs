using System;
using System.Collections.Generic;
using FlaxEngine;
using FlaxEngine.Interop;

namespace Game;

/// <summary>
/// ToggleGravity Script.
/// </summary>
public class ToggleGravity : Script
{
    static readonly Vector3 GRAVITY_OFF = Vector3.Zero;

    static readonly Vector3 GRAVITY_ON = new Vector3(0, -980, 0);

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        if (Input.GetKeyUp(KeyboardKeys.G))
        {
            if (this.Actor.PhysicsScene.Gravity.Equals(GRAVITY_ON))
                this.Actor.PhysicsScene.Gravity = GRAVITY_OFF;
            else
                this.Actor.PhysicsScene.Gravity = GRAVITY_ON;
        }
    }
}
