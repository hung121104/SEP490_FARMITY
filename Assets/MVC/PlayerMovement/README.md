# MVC Player Movement System

A clean Model-View-Controller implementation for Unity player movement with stamina system.

## Architecture Overview

### Model (`Assets/MVC/Model/PlayerModel.cs`)
- **Responsibilities**: Pure data and state management
- **Contains**: Position, velocity, stamina values, configuration
- **No Unity dependencies**: Can be unit tested easily
- **Events**: Notifies observers when state changes (position, stamina)

### View (`Assets/MVC/View/PlayerView.cs`)
- **Responsibilities**: Unity-specific rendering, animation, and input capture
- **Contains**: Rigidbody2D, Animator references
- **Input handling**: Captures Input System events and exposes them to Controller
- **Visual updates**: Receives model state changes and updates GameObject

### Controller (`Assets/MVC/Controller/PlayerController.cs`)
- **Responsibilities**: Game logic orchestration
- **Updates Model**: Based on View input
- **Applies rules**: Movement speed, stamina drain/regen, speed penalties
- **Mediates**: Between Model and View via events

### Bootstrap (`Assets/MVC/MVCBootstrap.cs`)
- **Responsibilities**: Wiring Model-View-Controller together
- **Configuration**: Exposes tunable parameters in Inspector
- **Lifecycle**: Creates instances and manages Update loop

## Setup Instructions

### 1. Prepare Your Player GameObject

1. Select your Player GameObject in the Hierarchy
2. Ensure it has:
   - `Rigidbody2D` component (set Body Type to Dynamic)
   - `Animator` component (with your animation controller)
   - Optional: `Collider2D` for physics interactions

### 2. Add MVC Components

1. **Add PlayerView**:
   - Select Player GameObject
   - Add Component → Search `PlayerView`
   - The script will auto-find Rigidbody2D and Animator (or assign manually)

2. **Add MVCBootstrap**:
   - Select Player GameObject
   - Add Component → Search `MVCBootstrap`
   - Configure in Inspector:
     - Max Stamina: `100`
     - Move Speed: `5`
     - Stamina Drain Rate: `10`
     - Stamina Regen Rate: `12`
     - Stamina Regen Delay: `1`
   - Assign `PlayerView` reference (or leave empty to auto-find)

### 3. Wire Up Input System

**Option A: Using PlayerInput Component (Recommended)**

1. Add Component → `Player Input` to your Player GameObject
2. In PlayerInput:
   - Actions: Assign your `InputSystem_Actions` asset
   - Behavior: Set to `Invoke Unity Events`
3. In the Events section:
   - Find `Player → Move` event
   - Add a new event listener
   - Drag your Player GameObject to the object field
   - Select `PlayerView → OnMove`

**Option B: Manual Event Binding**

If you already have input handling elsewhere, call:
```csharp
playerView.OnMove(context);
```

### 4. Set Up UI (Optional but Recommended)

1. **Create Stamina Bar**:
   - In your Canvas, create: GameObject → UI → Slider
   - Rename it to "StaminaBar"
   - Configure Slider:
     - Uncheck `Interactable`
     - Set Fill color to green/yellow
     - Position in your HUD

2. **Add StaminaBarUI Component**:
   - Select your StaminaBar (or a parent Panel)
   - Add Component → Search `StaminaBarUI`
   - Assign references:
     - Player Bootstrap: Drag your Player GameObject
     - Stamina Slider: Drag the Slider component

### 5. Update Animator Parameters (if using animations)

Ensure your Animator Controller has these parameters:
- `bool isWalking` - true when moving
- `float InputX` - horizontal input (-1 to 1)
- `float InputY` - vertical input (-1 to 1)
- `float LastInputX` - last horizontal direction when stopped
- `float LastInputY` - last vertical direction when stopped

### 6. Test In Play Mode

1. Press Play
2. Use WASD or Arrow keys to move
3. Watch stamina drain while moving and regenerate when idle
4. When stamina is empty, movement speed should reduce to 50%

## Key Features

✅ **Separation of Concerns**: Model, View, Controller are cleanly separated
✅ **Testable**: Model has no Unity dependencies, easily unit testable
✅ **Event-Driven**: Model notifies View of changes via C# events
✅ **Configurable**: All parameters exposed in Inspector
✅ **Stamina System**: Drains while moving, regenerates after delay
✅ **Speed Penalty**: 50% speed when stamina is empty
✅ **Input System Support**: Works with Unity's new Input System

## Extending The System

### Add Sprint Functionality

In `PlayerController.Update()`:
```csharp
// Check for sprint input
bool isSprinting = Input.GetKey(KeyCode.LeftShift); // or from View
if (isSprinting && !model.IsStaminaEmpty)
{
    model.MoveSpeed = 10f; // faster
    drain *= 2f; // drain more
}
```

### Add Health System

1. Add to `PlayerModel.cs`:
```csharp
public float CurrentHealth { get; private set; }
public float MaxHealth { get; private set; }
public event Action<float> OnHealthChanged;
```

2. Create similar UI observer pattern as stamina

### Access Model From Other Systems

```csharp
// From any MonoBehaviour:
var bootstrap = FindObjectOfType<MVCBootstrap>();
var model = bootstrap.GetPlayerModel();
float stamina = model.CurrentStamina;
```

## Troubleshooting

**Player doesn't move**:
- Check `PlayerInput` component is wired to `PlayerView.OnMove`
- Verify Rigidbody2D is set to Dynamic
- Check `MVCBootstrap` is attached and enabled

**Stamina doesn't drain**:
- Verify `staminaDrainRate` > 0 in MVCBootstrap Inspector
- Check Console for initialization message

**UI doesn't update**:
- Ensure `StaminaBarUI` has references assigned
- Check Model events are being fired (add Debug.Log in `PlayerModel.NotifyStaminaChanged`)

## File Structure

```
Assets/MVC/
├── Model/
│   └── PlayerModel.cs          # Pure data/state
├── View/
│   └── PlayerView.cs           # Unity rendering/input
├── Controller/
│   └── PlayerController.cs     # Game logic
├── UI/
│   └── StaminaBarUI.cs         # UI observer
└── MVCBootstrap.cs             # Wiring/initialization
```

## Benefits Over Monolithic Approach

- **Testability**: Model can be unit tested without Unity
- **Maintainability**: Clear responsibilities, easier to debug
- **Reusability**: Model/Controller can be reused for NPCs
- **Scalability**: Easy to add new features (health, inventory)
- **Team Collaboration**: Multiple developers can work on different layers

---

Created: November 15, 2025
Unity Version: 2022.3+
Input System: New Input System (com.unity.inputsystem)
