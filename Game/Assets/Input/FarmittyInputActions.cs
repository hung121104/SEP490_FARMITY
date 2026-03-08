// ──────────────────────────────────────────────────────────────
// AUTO-GENERATED C# WRAPPER for FarmittyInputActions.inputactions
//
// In the Unity Editor you would normally tick "Generate C# Class"
// on the .inputactions asset inspector and Unity creates this file.
//
// This hand-written version is provided so the project compiles
// immediately.  Once you open Unity, you can re-generate it from
// the asset inspector to stay up-to-date.
// ──────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class FarmittyInputActions : IInputActionCollection2, IDisposable
{
    // ───── Internal asset ─────
    public InputActionAsset asset { get; private set; }

    public FarmittyInputActions()
    {
        asset = InputActionAsset.FromJson(@"{
            ""name"": ""FarmittyInputActions"",
            ""maps"": [
                {
                    ""name"": ""Player"",
                    ""id"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                    ""actions"": [
                        { ""name"": ""Move"",          ""type"": ""Value"",  ""id"": ""11111111-1111-1111-1111-111111111111"", ""expectedControlType"": ""Vector2"", ""processors"": """", ""interactions"": """", ""initialStateCheck"": true },
                        { ""name"": ""Interact"",      ""type"": ""Button"", ""id"": ""22222222-2222-2222-2222-222222222222"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""Harvest"",       ""type"": ""Button"", ""id"": ""33333333-3333-3333-3333-333333333333"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""OpenInventory"", ""type"": ""Button"", ""id"": ""44444444-4444-4444-4444-444444444444"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""Attack"",        ""type"": ""Button"", ""id"": ""55555555-5555-5555-5555-555555555555"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""UseSkill"",      ""type"": ""Button"", ""id"": ""66666666-6666-6666-6666-666666666666"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""OpenChat"",      ""type"": ""Button"", ""id"": ""77777777-7777-7777-7777-777777777777"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""UseItem"",       ""type"": ""Button"", ""id"": ""aaaa0001-0001-0001-0001-000000000001"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""ScrollItem"",    ""type"": ""Value"",  ""id"": ""aaaa0002-0001-0001-0001-000000000001"", ""expectedControlType"": ""Axis"",    ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""HotbarSlot1"",   ""type"": ""Button"", ""id"": ""bbbb0001-0001-0001-0001-000000000001"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""HotbarSlot2"",   ""type"": ""Button"", ""id"": ""bbbb0002-0001-0001-0001-000000000001"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""HotbarSlot3"",   ""type"": ""Button"", ""id"": ""bbbb0003-0001-0001-0001-000000000001"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""HotbarSlot4"",   ""type"": ""Button"", ""id"": ""bbbb0004-0001-0001-0001-000000000001"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""HotbarSlot5"",   ""type"": ""Button"", ""id"": ""bbbb0005-0001-0001-0001-000000000001"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""HotbarSlot6"",   ""type"": ""Button"", ""id"": ""bbbb0006-0001-0001-0001-000000000001"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""HotbarSlot7"",   ""type"": ""Button"", ""id"": ""bbbb0007-0001-0001-0001-000000000001"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""HotbarSlot8"",   ""type"": ""Button"", ""id"": ""bbbb0008-0001-0001-0001-000000000001"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                        { ""name"": ""HotbarSlot9"",   ""type"": ""Button"", ""id"": ""bbbb0009-0001-0001-0001-000000000001"", ""expectedControlType"": ""Button"",  ""processors"": """", ""interactions"": """", ""initialStateCheck"": false }
                    ],
                    ""bindings"": [
                        { ""name"": ""WASD"", ""id"": ""b0000001-0001-0001-0001-000000000001"", ""path"": ""2DVector"",              ""action"": ""Move"", ""isComposite"": true,  ""isPartOfComposite"": false },
                        { ""name"": ""up"",   ""id"": ""b0000001-0001-0001-0001-000000000002"", ""path"": ""<Keyboard>/w"",          ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true  },
                        { ""name"": ""down"", ""id"": ""b0000001-0001-0001-0001-000000000003"", ""path"": ""<Keyboard>/s"",          ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true  },
                        { ""name"": ""left"", ""id"": ""b0000001-0001-0001-0001-000000000004"", ""path"": ""<Keyboard>/a"",          ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true  },
                        { ""name"": ""right"",""id"": ""b0000001-0001-0001-0001-000000000005"", ""path"": ""<Keyboard>/d"",          ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true  },
                        { ""name"": ""Arrow Keys"", ""id"": ""b0000002-0001-0001-0001-000000000001"", ""path"": ""2DVector"",         ""action"": ""Move"", ""isComposite"": true,  ""isPartOfComposite"": false },
                        { ""name"": ""up"",   ""id"": ""b0000002-0001-0001-0001-000000000002"", ""path"": ""<Keyboard>/upArrow"",    ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true  },
                        { ""name"": ""down"", ""id"": ""b0000002-0001-0001-0001-000000000003"", ""path"": ""<Keyboard>/downArrow"",  ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true  },
                        { ""name"": ""left"", ""id"": ""b0000002-0001-0001-0001-000000000004"", ""path"": ""<Keyboard>/leftArrow"",  ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true  },
                        { ""name"": ""right"",""id"": ""b0000002-0001-0001-0001-000000000005"", ""path"": ""<Keyboard>/rightArrow"", ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true  },
                        { ""name"": """", ""id"": ""b0000003-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/e"",              ""action"": ""Interact"",      ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""b0000004-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/f"",              ""action"": ""Harvest"",       ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""b0000005-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/tab"",            ""action"": ""OpenInventory"", ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""b0000006-0001-0001-0001-000000000001"", ""path"": ""<Mouse>/leftButton"",        ""action"": ""Attack"",        ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""b0000007-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/q"",              ""action"": ""UseSkill"",      ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""b0000008-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/enter"",          ""action"": ""OpenChat"",      ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""cc000001-0001-0001-0001-000000000001"", ""path"": ""<Mouse>/leftButton"",        ""action"": ""UseItem"",       ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""cc000002-0001-0001-0001-000000000001"", ""path"": ""<Mouse>/scroll/y"",          ""action"": ""ScrollItem"",    ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""dd000001-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/1"",              ""action"": ""HotbarSlot1"",   ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""dd000002-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/2"",              ""action"": ""HotbarSlot2"",   ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""dd000003-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/3"",              ""action"": ""HotbarSlot3"",   ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""dd000004-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/4"",              ""action"": ""HotbarSlot4"",   ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""dd000005-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/5"",              ""action"": ""HotbarSlot5"",   ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""dd000006-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/6"",              ""action"": ""HotbarSlot6"",   ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""dd000007-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/7"",              ""action"": ""HotbarSlot7"",   ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""dd000008-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/8"",              ""action"": ""HotbarSlot8"",   ""isComposite"": false, ""isPartOfComposite"": false },
                        { ""name"": """", ""id"": ""dd000009-0001-0001-0001-000000000001"", ""path"": ""<Keyboard>/9"",              ""action"": ""HotbarSlot9"",   ""isComposite"": false, ""isPartOfComposite"": false }
                    ]
                }
            ],
            ""controlSchemes"": [
                {
                    ""name"": ""Keyboard&Mouse"",
                    ""bindingGroup"": ""Keyboard&Mouse"",
                    ""devices"": [
                        { ""devicePath"": ""<Keyboard>"", ""isOptional"": false },
                        { ""devicePath"": ""<Mouse>"",    ""isOptional"": false }
                    ]
                }
            ]
        }");

        _player = new PlayerActions(this);
    }

    // ───── IDisposable ─────
    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    // ───── IInputActionCollection2 ─────
    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public IEnumerable<InputBinding> bindings => asset.bindings;

    public bool Contains(InputAction action) => asset.Contains(action);
    public System.Collections.Generic.IEnumerator<InputAction> GetEnumerator() => asset.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public void Enable()  => asset.Enable();
    public void Disable() => asset.Disable();

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
        => asset.FindAction(actionNameOrId, throwIfNotFound);

    public int FindBinding(InputBinding bindingMask, out InputAction action)
        => asset.FindBinding(bindingMask, out action);

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Player action map wrapper
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private readonly PlayerActions _player;
    public PlayerActions Player => _player;

    public struct PlayerActions
    {
        private FarmittyInputActions _wrapper;

        internal PlayerActions(FarmittyInputActions wrapper) { _wrapper = wrapper; }

        // ── Core actions ──
        public InputAction Move          => _wrapper.asset.FindAction("Player/Move",          throwIfNotFound: true);
        public InputAction Interact      => _wrapper.asset.FindAction("Player/Interact",      throwIfNotFound: true);
        public InputAction Harvest       => _wrapper.asset.FindAction("Player/Harvest",       throwIfNotFound: true);
        public InputAction OpenInventory => _wrapper.asset.FindAction("Player/OpenInventory", throwIfNotFound: true);
        public InputAction Attack        => _wrapper.asset.FindAction("Player/Attack",        throwIfNotFound: true);
        public InputAction UseSkill      => _wrapper.asset.FindAction("Player/UseSkill",      throwIfNotFound: true);
        public InputAction OpenChat      => _wrapper.asset.FindAction("Player/OpenChat",      throwIfNotFound: true);

        // ── Hotbar / Item actions ──
        public InputAction UseItem       => _wrapper.asset.FindAction("Player/UseItem",       throwIfNotFound: true);
        public InputAction ScrollItem    => _wrapper.asset.FindAction("Player/ScrollItem",    throwIfNotFound: true);
        public InputAction HotbarSlot1   => _wrapper.asset.FindAction("Player/HotbarSlot1",   throwIfNotFound: true);
        public InputAction HotbarSlot2   => _wrapper.asset.FindAction("Player/HotbarSlot2",   throwIfNotFound: true);
        public InputAction HotbarSlot3   => _wrapper.asset.FindAction("Player/HotbarSlot3",   throwIfNotFound: true);
        public InputAction HotbarSlot4   => _wrapper.asset.FindAction("Player/HotbarSlot4",   throwIfNotFound: true);
        public InputAction HotbarSlot5   => _wrapper.asset.FindAction("Player/HotbarSlot5",   throwIfNotFound: true);
        public InputAction HotbarSlot6   => _wrapper.asset.FindAction("Player/HotbarSlot6",   throwIfNotFound: true);
        public InputAction HotbarSlot7   => _wrapper.asset.FindAction("Player/HotbarSlot7",   throwIfNotFound: true);
        public InputAction HotbarSlot8   => _wrapper.asset.FindAction("Player/HotbarSlot8",   throwIfNotFound: true);
        public InputAction HotbarSlot9   => _wrapper.asset.FindAction("Player/HotbarSlot9",   throwIfNotFound: true);

        // Map accessors
        public InputActionMap Get() => _wrapper.asset.FindActionMap("Player", throwIfNotFound: true);
        public void Enable()  => Get().Enable();
        public void Disable() => Get().Disable();

        public InputAction FindAction(string name) => Get().FindAction(name);
    }
}
