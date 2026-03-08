# Farmitty Input System – Integration Guide

> Reference for integrating the **New Input System** into existing or future features.
> The project uses **"Both"** input handling so legacy `Input.GetKeyDown` still works during migration.

---

## Architecture Overview

```
┌──────────────────────────────────────┐
│  FarmittyInputActions.inputactions   │  ← JSON asset: defines all actions & default bindings
└──────────────┬───────────────────────┘
               │
┌──────────────▼───────────────────────┐
│  FarmittyInputActions.cs             │  ← C# wrapper: typed access via .Player.ActionName
└──────────────┬───────────────────────┘
               │
┌──────────────▼───────────────────────┐
│  InputManager.cs  (Singleton)        │  ← Owns the asset, enables/disables, save/load bindings
│  Access: InputManager.Instance.X     │
└──────────────┬───────────────────────┘
               │ .performed / .canceled / ReadValue<T>()
┌──────────────▼───────────────────────┐
│  Your Gameplay Script                │  ← Subscribe in OnEnable, unsubscribe in OnDisable
└──────────────────────────────────────┘
```

---

## Step-by-Step: Adding a New Input Action

### 1. Define the Action in the Asset

Open `Assets/Input/FarmittyInputActions.inputactions` in the Unity Input Actions editor.

1. Select the **Player** action map (left column).
2. Click **+** to add a new action.
3. Set:
   - **Name**: e.g. `OpenMap`
   - **Type**: `Button` (for press) or `Value` (for continuous like scroll/axis)
4. Add a **Binding** → pick a key (e.g. `M`).
5. Save the asset.

### 2. Add the Wrapper Property

In `Assets/Input/FarmittyInputActions.cs`, add a property inside the `PlayerActions` struct:

```csharp
// Inside struct PlayerActions:
public InputAction OpenMap => _wrapper.asset.FindAction("Player/OpenMap", throwIfNotFound: true);
```

### 3. Add the InputManager Accessor

In `Assets/Scripts/InputSystem/InputManager.cs`, add a convenience property:

```csharp
public InputAction OpenMap => _actions.Player.OpenMap;
```

### 4. Subscribe in Your Gameplay Script

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class MapController : MonoBehaviour
{
    [SerializeField] private GameObject mapPanel;

    private void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OpenMap.performed += OnToggleMap;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OpenMap.performed -= OnToggleMap;
    }

    private void OnToggleMap(InputAction.CallbackContext ctx)
    {
        mapPanel.SetActive(!mapPanel.activeSelf);
    }
}
```

**Done.** The key is now rebindable via the settings menu and persisted automatically.

---

## Common Patterns

### Pattern A: Toggle (Button Press → On/Off)

Use `.performed` only. Good for inventory, map, chat windows.

```csharp
private void OnEnable()
{
    InputManager.Instance.OpenInventory.performed += OnToggle;
}

private void OnDisable()
{
    InputManager.Instance.OpenInventory.performed -= OnToggle;
}

private void OnToggle(InputAction.CallbackContext ctx)
{
    panel.SetActive(!panel.activeSelf);
}
```

### Pattern B: Continuous Input (Hold / Axis)

Use `ReadValue<T>()` in `Update()`. Good for movement, camera zoom.

```csharp
private void Update()
{
    if (InputManager.Instance == null) return;

    Vector2 move = InputManager.Instance.Move.ReadValue<Vector2>();
    transform.Translate(new Vector3(move.x, 0, move.y) * speed * Time.deltaTime);
}
```

### Pattern C: Press & Release (Hold to Sprint)

Use both `.performed` (key down) and `.canceled` (key up).

```csharp
private void OnEnable()
{
    InputManager.Instance.Sprint.performed += _ => isSprinting = true;
    InputManager.Instance.Sprint.canceled  += _ => isSprinting = false;
}
// WARNING: If using lambdas like above, store them in fields for proper unsubscription!
```

**Proper version with stored delegates:**

```csharp
private System.Action<InputAction.CallbackContext> _onSprintStart;
private System.Action<InputAction.CallbackContext> _onSprintStop;

private void Awake()
{
    _onSprintStart = _ => isSprinting = true;
    _onSprintStop  = _ => isSprinting = false;
}

private void OnEnable()
{
    InputManager.Instance.Sprint.performed += _onSprintStart;
    InputManager.Instance.Sprint.canceled  += _onSprintStop;
}

private void OnDisable()
{
    InputManager.Instance.Sprint.performed -= _onSprintStart;
    InputManager.Instance.Sprint.canceled  -= _onSprintStop;
}
```

### Pattern D: Multiple Indexed Actions (Hotbar Slots)

Use `GetHotbarSlotAction(int)` + stored delegate array.

```csharp
private System.Action<InputAction.CallbackContext>[] _callbacks;

private void Awake()
{
    _callbacks = new System.Action<InputAction.CallbackContext>[9];
    for (int i = 0; i < 9; i++)
    {
        int slot = i;
        _callbacks[i] = ctx => SelectSlot(slot);
    }
}

private void OnEnable()
{
    for (int i = 0; i < 9; i++)
    {
        var action = InputManager.Instance.GetHotbarSlotAction(i);
        if (action != null) action.performed += _callbacks[i];
    }
}

private void OnDisable()
{
    for (int i = 0; i < 9; i++)
    {
        var action = InputManager.Instance.GetHotbarSlotAction(i);
        if (action != null) action.performed -= _callbacks[i];
    }
}
```

---

## Migrating Legacy Input

| Legacy Code | New Input System Replacement |
|---|---|
| `Input.GetKeyDown(KeyCode.E)` | `InputManager.Instance.Interact.performed += callback` |
| `Input.GetKey(KeyCode.LeftShift)` | `InputManager.Instance.Sprint.ReadValue<float>() > 0` |
| `Input.GetKeyUp(KeyCode.E)` | `InputManager.Instance.Interact.canceled += callback` |
| `Input.GetAxis("Horizontal")` | `InputManager.Instance.Move.ReadValue<Vector2>().x` |
| `Input.GetMouseButtonDown(0)` | `InputManager.Instance.UseItem.performed += callback` |
| `Input.mousePosition` | `Mouse.current.position.ReadValue()` |
| `Input.GetAxis("Mouse ScrollWheel")` | `InputManager.Instance.ScrollItem.ReadValue<float>()` |

---

## Rebinding Composite Actions (e.g. Move WASD)

For composite actions like Move, you **cannot rebind index 0** (the composite parent).
Use indices for each direction instead:

| Binding Index | Key  | Direction |
|:---:|---|---|
| 0 | *(composite parent — skip)* | — |
| 1 | W | Up |
| 2 | S | Down |
| 3 | A | Left |
| 4 | D | Right |

Set up **4 separate** `RebindUIController` rows:

- Row "Move Up":    `actionName = "Move"`, `bindingIndex = 1`
- Row "Move Down":  `actionName = "Move"`, `bindingIndex = 2`
- Row "Move Left":  `actionName = "Move"`, `bindingIndex = 3`
- Row "Move Right": `actionName = "Move"`, `bindingIndex = 4`

---

## Available Actions Reference

| Action | Default Binding | Type | Use Case |
|---|---|---|---|
| `Move` | WASD / Arrows | Vector2 | Player movement |
| `Interact` | E | Button | NPC talk, pick up |
| `Harvest` | F | Button | Harvest crops |
| `OpenInventory` | Tab | Button | Toggle inventory |
| `Attack` | Mouse Left | Button | Attack/tool use |
| `UseSkill` | Q | Button | Active skill |
| `OpenChat` | Enter | Button | Chat window |
| `UseItem` | Mouse Left | Button | Use held item |
| `ScrollItem` | Scroll Wheel Y | Axis | Cycle hotbar |
| `HotbarSlot1-9` | Keys 1-9 | Button | Select hotbar slot |

---

## Rules & Gotchas

1. **Always guard** with `if (InputManager.Instance != null)` — the singleton may not exist in every scene.
2. **Always unsubscribe** in `OnDisable()` — leaked subscriptions cause duplicate fires and errors.
3. **Never use lambdas for subscribe/unsubscribe** — store delegates in fields or arrays (lambdas create new instances every time and `−=` won't match).
4. **Rebinding saves automatically** via `InputManager.SaveBindings()` → `PlayerPrefs`.
5. **Reset to defaults** by calling `InputManager.Instance.ResetBindingsToDefault()`.
6. **Disable input during UI** by calling `InputManager.Instance.DisablePlayerActions()` and re-enabling with `EnablePlayerActions()`.
