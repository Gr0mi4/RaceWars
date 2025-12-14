using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules
{
    /// <summary>
    /// Module that applies physically accurate aerodynamic drag resistance.
    /// Formula: F_drag = -0.5 * ρ * Cd * A * v² * direction
    /// Where:
    /// - ρ (airDensity) - air density from AerodragModuleSpec
    /// - Cd (dragCoefficient) - drag coefficient from CarSpec
    /// - A (frontArea) - frontal area from CarSpec
    /// - v - velocity magnitude
    /// - direction - normalized velocity vector
    /// </summary>
    public sealed class AerodragModule : IVehicleModule
    {
        private readonly float _airDensity;
        private readonly float _minSpeed;

        public AerodragModule(float airDensity, float minSpeed)
        {
            _airDensity = Mathf.Max(0.01f, airDensity);
            _minSpeed = Mathf.Max(0f, minSpeed);
        }

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            // Get velocity
            Vector3 velocity = RigidbodyCompat.GetVelocity(ctx.rb);
            float speed = velocity.magnitude;

            // Only apply drag if speed is above minimum threshold
            if (speed < _minSpeed) return;

            // Get aerodynamic parameters from CarSpec
            if (ctx.spec == null) return;

            float frontArea = ctx.spec.frontArea;
            float dragCoefficient = ctx.spec.dragCoefficient;

            if (frontArea <= 0f || dragCoefficient <= 0f) return;

            // Calculate aerodynamic drag force: F = -0.5 * ρ * Cd * A * v² * direction
            // This is the standard drag equation for fluid dynamics
            float dragForceMagnitude = 0.5f * _airDensity * dragCoefficient * frontArea * speed * speed;
            
            // Direction is opposite to velocity (drag opposes motion)
            Vector3 dragDirection = -velocity.normalized;
            Vector3 dragForce = dragDirection * dragForceMagnitude;

            // Apply drag force
            ctx.rb.AddForce(dragForce, ForceMode.Force);
        }
    }
}
