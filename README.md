# RaceWars - Vehicle Physics System

## Project Architecture

### General Structure

The project uses a modular architecture for vehicle physics simulation. Main principle: **separation of data (Specs) and logic (Systems)**.

```
Assets/Scripts/Vehicle/
├── Core/           # Core engine logic
├── Systems/        # Force systems (apply physical forces)
├── Debug/          # Debug utilities
├── Specs/          # ScriptableObjects with parameters
├── Input/          # Input system
└── UI/             # UI components
```

---

## Core (System Core)

### Main Components:

- **`VehicleController.cs`** - Main MonoBehaviour, manages pipeline and state updates
- **`VehicleContext.cs`** - Immutable struct, passes all necessary references to systems
- **`VehicleState.cs`** - Mutable struct for vehicle state (speed, RPM, gear, etc.)
- **`VehiclePipeline.cs`** - Executes systems in sequence
- **`IVehicleModule.cs`** - Interface for all systems
- **`VehicleInput.cs`** - Input data structure (throttle, brake, steer, etc.)

### Core Rules:

1. **VehicleController** creates a fixed pipeline in `CreatePipeline()` method - this is part of engine logic, not configurable via ScriptableObject
2. **StateCollector** is integrated into `VehicleController.UpdateState()` - this is core logic, not a separate system
3. **VehicleContext** is created every frame in `FixedUpdate()` with current data
4. All systems receive the same `VehicleContext` and can modify `VehicleState`

---

## Systems (Force Systems)

### Structure:

All systems are located in `Assets/Scripts/Vehicle/Systems/` and implement the `IVehicleModule` interface.

### Current Systems:

1. **`WheelSystem.cs`** - Wheel management and lateral grip
2. **`DriveSystem.cs`** - Coordinates engine and gearbox
3. **`EngineSystem.cs`** - Engine calculations (RPM, power, torque)
4. **`GearboxSystem.cs`** - Gearbox control
5. **`SteeringSystem.cs`** - Steering control (bicycle model, friction circle)
6. **`AerodragSystem.cs`** - Aerodynamic drag
7. **`SuspensionSystem.cs`** - Suspension (placeholder for future implementation)
8. **`DrivetrainSystem.cs`** - Drivetrain (FWD/RWD/AWD, placeholder)

### Rules for Creating New Systems:

1. **All systems must:**
   - Implement `IVehicleModule`
   - Be in namespace `Vehicle.Systems`
   - Have `System` suffix in name (e.g., `BrakeSystem.cs`)
   - Accept parameters via constructor (usually Spec)
   - Use `VehicleContext` to access data

2. **System must be added to `VehicleController.CreatePipeline()`:**
   ```csharp
   // Example of adding a new system
   if (carSpec?.brakeSpec != null)
   {
       modules.Add(new BrakeSystem(carSpec.brakeSpec));
   }
   ```

3. **System order in pipeline matters:**
   - First: systems that update state (WheelSystem)
   - Then: systems that apply forces (DriveSystem, SteeringSystem)
   - Last: resistance systems (AerodragSystem)
   - Debug systems at the very end

4. **Systems must NOT:**
   - Depend on each other directly (use only VehicleContext and VehicleState)
   - Modify VehicleContext (it's immutable)
   - Create other systems inside themselves (except internal helper classes)

---

## Specs (ScriptableObjects)

### Structure:

All Specs are located in `Assets/Scripts/Vehicle/Specs/` and inherit from `ScriptableObject`.

### Current Specs:

1. **`CarSpec.cs`** - Main Spec, contains references to all other Specs
2. **`EngineSpec.cs`** - Engine parameters (power, torque, curves)
3. **`GearboxSpec.cs`** - Gearbox parameters (gear ratios, transmission type)
4. **`WheelSpec.cs`** - Wheel and tire parameters
5. **`ChassisSpec.cs`** - Chassis parameters (mass, center of mass, aerodynamics)
6. **`SteeringSpec.cs`** - Steering parameters
7. **`SuspensionSpec.cs`** - Suspension parameters (placeholder)
8. **`DrivetrainSpec.cs`** - Drivetrain parameters (placeholder)

### Rules for Creating New Specs:

1. **New Spec must:**
   - Inherit from `ScriptableObject`
   - Be in namespace `Vehicle.Specs`
   - Have `Spec` suffix in name
   - Have `[CreateAssetMenu]` attribute for Unity creation
   - Contain only data (parameters), no logic

2. **Spec must be added to `CarSpec.cs`:**
   ```csharp
   [Tooltip("Description")]
   public NewSpec newSpec;
   ```

3. **Spec must be added to `VehicleContext.cs`:**
   ```csharp
   public readonly NewSpec newSpec;
   ```

4. **Spec must be passed to VehicleContext in `VehicleController.FixedUpdate()`:**
   ```csharp
   var ctx = new VehicleContext(
       _rb, transform, carSpec, Time.fixedDeltaTime,
       carSpec?.engineSpec,
       carSpec?.gearboxSpec,
       carSpec?.wheelSpec,
       carSpec?.chassisSpec,
       carSpec?.steeringSpec,
       carSpec?.newSpec  // Add here
   );
   ```

5. **Asset files for specific vehicle should be located in:**
   ```
   Assets/ScriptableObjects/Vehicles/{car_name}/
   ```
   Example: `Assets/ScriptableObjects/Vehicles/golf5_proto/`

---

## Debug (Debug Utilities)

### Structure:

Debug utilities are located in `Assets/Scripts/Vehicle/Debug/`.

### Current Utilities:

- **`TelemetrySystem.cs`** - Vehicle telemetry logging

### Rules:

- Debug systems do not apply physical forces
- They are optional and enabled via settings in `VehicleController`
- Can implement `IVehicleCollisionListener` for collision handling

---

## Input (Input System)

### Structure:

Input system is located in `Assets/Scripts/Vehicle/Input/`.

### Components:

- **`VehicleInputProvider.cs`** - Input data provider
- **`IInputSource.cs`** - Input source interface
- **`UnityInputSource.cs`** - Unity Input System implementation
- **`InputMapping.cs`** - Key mapping

---

## UI (User Interface)

### Structure:

UI components are located in `Assets/Scripts/Vehicle/UI/`.

### Components:

- **`SpeedometerUI.cs`** - Telemetry display on screen

---

## System Extension Rules

### Adding a New System:

1. **Create System class:**
   ```csharp
   namespace Vehicle.Systems
   {
       public sealed class NewSystem : IVehicleModule
       {
           private readonly NewSpec _spec;
           
           public NewSystem(NewSpec spec)
           {
               _spec = spec;
           }
           
           public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
           {
               // System logic
           }
       }
   }
   ```

2. **Create Spec class:**
   ```csharp
   namespace Vehicle.Specs
   {
       [CreateAssetMenu(menuName = "Vehicle/New Spec", fileName = "NewSpec")]
       public sealed class NewSpec : ScriptableObject
       {
           // Parameters
       }
   }
   ```

3. **Add Spec to CarSpec:**
   ```csharp
   [Tooltip("New system specification")]
   public NewSpec newSpec;
   ```

4. **Add Spec to VehicleContext:**
   - Add field `public readonly NewSpec newSpec;`
   - Add parameter to constructor

5. **Add system to VehicleController.CreatePipeline():**
   ```csharp
   if (carSpec?.newSpec != null)
   {
       modules.Add(new NewSystem(carSpec.newSpec));
   }
   ```

6. **Update VehicleContext in VehicleController:**
   ```csharp
   var ctx = new VehicleContext(
       _rb, transform, carSpec, Time.fixedDeltaTime,
       carSpec?.engineSpec,
       carSpec?.gearboxSpec,
       carSpec?.wheelSpec,
       carSpec?.chassisSpec,
       carSpec?.steeringSpec,
       carSpec?.newSpec  // Add
   );
   ```

7. **Create asset files:**
   - Create `NewSpec_Default.asset` in specific vehicle folder
   - Assign it in `CarSpec_Default.asset`

### Important Principles:

- **Data and Logic Separation:** Specs contain only data, Systems contain only logic
- **System Independence:** Systems don't depend on each other, communicate via VehicleContext and VehicleState
- **Fixed Pipeline:** System order is defined in code, not via ScriptableObject
- **Unified Interface:** All systems implement `IVehicleModule` and have the same `Tick()` signature
- **Consistent Naming:** All systems end with `System`, all Specs end with `Spec`

---

## Testing

### Test Structure:

Tests are located in `Assets/Tests/Editor/Vehicle/`.

### Rules:

- Tests must be updated when systems change
- Tests use `NUnit.Framework`
- Tests create temporary ScriptableObjects for isolation

---

## Migration from Old Structure

### What Was Removed:

- `Modules/` folder - replaced with `Systems/`
- `Specs/Modules/` folder - removed, Specs now directly in `Specs/`
- `VehiclePipelineSpec` - pipeline now created in code
- `VehicleModuleSpec` - no longer needed
- All old ModuleSpec classes - replaced with direct references in CarSpec

### What Changed:

- `EngineDriveModel` → `DriveSystem` + `EngineSystem` + `GearboxSystem`
- `SteeringModule` + `PhysicsSteeringModel` → `SteeringSystem`
- `WheelModule` + `LateralGripModule` → `WheelSystem`
- `AerodragModule` → `AerodragSystem`
- `TelemetryModule` → `TelemetrySystem` (moved to Debug/)

---

## Contact and Support

When modifying the project structure, update this README to keep documentation current.
