using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;
using Vehicle.Systems;

namespace Vehicle.UI.Telemetry
{
    /// <summary>
    /// Telemetry module for displaying force data (wheel force, drag, damping, net force).
    /// </summary>
    public sealed class ForcesTelemetryModule : TelemetryModuleBase
    {
        private readonly EngineSystem _engineSystem;

        public override string ModuleName => "[FORCES]";

        public ForcesTelemetryModule(bool enabled = true) : base(enabled)
        {
            _engineSystem = new EngineSystem();
        }

        protected override string BuildDisplayText(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            float speed = state.speed;
            float forwardSpeed = Vector3.Dot(RigidbodyCompat.GetVelocity(ctx.rb), ctx.tr.forward);

            float wheelForce = 0f;
            float dragForce = 0f;
            float dampingForce = 0f;
            float normalLoadTotal = 0f;
            float normalLoadFront = 0f;
            float normalLoadRear = 0f;
            float rollingResistance = 0f;

            // Calculate drag force
            if (ctx.chassisSpec != null)
            {
                const float airDensity = 1.225f;
                if (speed > 0.1f && ctx.chassisSpec.frontArea > 0f && ctx.chassisSpec.dragCoefficient > 0f)
                {
                    dragForce = 0.5f * airDensity * ctx.chassisSpec.dragCoefficient * 
                                ctx.chassisSpec.frontArea * speed * speed;
                }

                // Damping force
                float linearDamping = RigidbodyCompat.GetLinearDamping(ctx.rb);
                if (speed > 0.001f)
                {
                    dampingForce = linearDamping * speed * ctx.rb.mass;
                }
            }

            // Collect normal loads from suspension
            if (state.wheels != null && state.wheels.Length > 0)
            {
                for (int i = 0; i < state.wheels.Length; i++)
                {
                    var n = Mathf.Max(0f, state.wheels[i].normalForce);
                    normalLoadTotal += n;
                    if (i < 2) normalLoadFront += n;
                    else normalLoadRear += n;

                    float crr = ctx.wheelSpec != null ? ctx.wheelSpec.rollingResistance : 0f;
                    rollingResistance += crr * n;
                }
            }

            // Calculate wheel force from engine
            if (ctx.engineSpec != null && ctx.gearboxSpec != null && state.engineRPM > 0f)
            {
                float currentGearRatio = GetCurrentGearRatio(state.currentGear, ctx.gearboxSpec);
                float wheelRadius = state.wheelRadius > 0.01f 
                    ? state.wheelRadius 
                    : (ctx.wheelSpec != null ? ctx.wheelSpec.wheelRadius : 0.3f);

            }

            float netForce = wheelForce - dragForce - dampingForce;

            // Calculate power equivalents
            float wheelPowerHP = 0f;
            float dragPowerHP = 0f;
            float dampingPowerHP = 0f;
            float netPowerHP = 0f;

            if (speed > 0.001f)
            {
                if (wheelForce > 0f)
                {
                    float wheelPowerW = Mathf.Abs(wheelForce) * speed;
                    wheelPowerHP = wheelPowerW / 745.7f;
                }
                if (dragForce > 0f)
                {
                    float dragPowerW = dragForce * speed;
                    dragPowerHP = dragPowerW / 745.7f;
                }
                if (dampingForce > 0f)
                {
                    float dampingPowerW = dampingForce * speed;
                    dampingPowerHP = dampingPowerW / 745.7f;
                }
                if (netForce > 0f)
                {
                    float netPowerW = netForce * speed;
                    netPowerHP = netPowerW / 745.7f;
                }
            }

            string text = ModuleName + "\n";
            if (ctx.engineSpec != null)
            {
                text += FormatValue("Wheel Force", wheelForce, "N", 0) + $" ({wheelPowerHP:F1} HP)\n";
            }
            text += FormatValue("Drag Force", dragForce, "N", 0) + $" ({dragPowerHP:F1} HP)\n";
            text += FormatValue("Damping Force", dampingForce, "N", 0) + $" ({dampingPowerHP:F1} HP)\n";
            text += FormatValue("Net Force", netForce, "N", 0) + $" ({netPowerHP:F1} HP)\n";
            if (normalLoadTotal > 0f)
            {
                text += FormatValue("Normal Total", normalLoadTotal, "N", 0);
                text += FormatValue("Normal Front", normalLoadFront, "N", 0);
                text += FormatValue("Normal Rear", normalLoadRear, "N", 0);
            }
            if (rollingResistance > 0f)
            {
                text += FormatValue("Rolling Resistance", rollingResistance, "N", 0);
            }
            text += FormatValue("Mass", ctx.rb.mass, "kg", 1);
            return text;
        }

        private float GetCurrentGearRatio(int currentGear, GearboxSpec gearboxSpec)
        {
            if (currentGear == -1)
            {
                return gearboxSpec.reverseGearRatio * gearboxSpec.finalDriveRatio;
            }
            else if (currentGear > 0 && currentGear <= gearboxSpec.gearRatios.Length)
            {
                int arrayIndex = currentGear - 1;
                return gearboxSpec.gearRatios[arrayIndex] * gearboxSpec.finalDriveRatio;
            }
            return 0f;
        }
    }
}

