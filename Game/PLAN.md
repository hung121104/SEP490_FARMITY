# Auth UI Refactor Plan

## Status Legend
- [ ] Not started
- [~] In progress
- [x] Done

---

## Step 1 — Create `CanvasGroupExtensions` helper
- [x] Create `Assets/Scripts/Utils/CanvasGroupExtensions.cs`
- [x] Add `Show()` and `Hide()` extension methods on `CanvasGroup`

## Step 2 — Create `BookAnimations` constants
- [x] Create `Assets/Scripts/MVP/_UI/Authentication/BookAnimations.cs`
- [x] Define `TurnRToL` and `TurnLToR` as `public const string`

## Step 3 — Refactor `BookPanelController`
- [x] Remove `[SerializeField] private Animator animator`
- [x] Remove `turnRToL` / `turnLToR` private strings, use `BookAnimations` constants
- [x] Add `public event System.Action OnShowLogin`
- [x] Add `public event System.Action OnShowRegister`
- [x] Add `public event System.Action OnShowTitle`
- [x] Fire the appropriate event inside `ShowLogin()`, `ShowRegister()`, `ShowTitle()`
- [x] Replace 3-line canvas group blocks with `.Show()` / `.Hide()` extension calls

## Step 4 — Refactor `BookMenuController`
- [x] Remove duplicate `turnRToL` string, use `BookAnimations.TurnRToL`
- [x] Add `[SerializeField] private BookPanelController panelController` reference
- [x] In `Awake`, subscribe to `panelController.OnShowLogin`, `OnShowRegister`, `OnShowTitle`
- [x] Each subscription calls `animator.SetTrigger(BookAnimations.TurnRToL/TurnLToR)`

## Step 5 — Refactor `RegisterView`
- [x] Replace 3-line canvas group blocks in `ShowOtpPanel()` / `HideOtpPanel()` with `.Show()` / `.Hide()`

## Step 6 — Bring `AuthenticateLoginView` to parity with `RegisterView`
- [x] Add `[SerializeField] private Text errorText`
- [x] Add `ShowError(string message)`
- [x] Add `SetInteractable(bool interactable)`
- [x] Add `OnLoginSuccess()`
- [x] Add null-guards to `GetUsername()` and `GetPassword()`

---

## Files Affected
| File | Change |
|---|---|
| `Assets/Scripts/Utils/CanvasGroupExtensions.cs` | **Create** |
| `Assets/Scripts/MVP/_UI/Authentication/BookAnimations.cs` | **Create** |
| `Assets/Scripts/MVP/_UI/Authentication/BookPanelController.cs` | Refactor |
| `Assets/Scripts/MVP/_UI/Authentication/BookMenuController.cs` | Refactor |
| `Assets/Scripts/MVP/Authenticate/Views/RegisterView.cs` | Refactor |
| `Assets/Scripts/MVP/Authenticate/Views/AuthenticateLoginView.cs` | Refactor |
