using UnityEngine;

public class FishingPresenter
{
    FishingModel model;
    FishingView view;
    FishingService service;
    FishingMiniGameController controller;

    float gravity = 2f;
    float jumpForce = 4f;

    float zoneTarget;
    float zoneSpeed = 0.6f;

    public FishingPresenter(
    FishingModel model,
    FishingView view,
    FishingService service,
    FishingMiniGameController controller)
    {
        this.model = model;
        this.view = view;
        this.service = service;
        this.controller = controller;

        zoneTarget = Random.value;
    }

    public void Start()
    {
        model.fishPosition = 0.5f;
        model.zonePosition = 0.5f;
        model.progress = 0;
        model.failTimer = 0;
        model.isFishing = true;

        view.Show();
        model.fishVelocity = 0;
    }

    public void Stop()
    {
        model.isFishing = false;
        view.Hide();
    }

    public void Update(float dt)
    {
        if (!model.isFishing) return;

        HandleInput(dt);
        MoveFish(dt);
        MoveZone(dt);
        CheckProgress(dt);

        view.SetFishPosition(model.fishPosition);
        view.SetZonePosition(model.zonePosition);
        view.SetProgress(model.progress);
    }

    void HandleInput(float dt)
    {
        if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
        {
            model.fishVelocity += jumpForce * dt;
        }
    }

    void MoveFish(float dt)
    {
        model.fishVelocity -= gravity * dt;

        model.fishPosition += model.fishVelocity * dt;

        model.fishPosition = Mathf.Clamp01(model.fishPosition);
    }

    void MoveZone(float dt)
    {
        if (Mathf.Abs(model.zonePosition - zoneTarget) < 0.02f)
        {
            zoneTarget = Random.value;
        }

        model.zonePosition = Mathf.MoveTowards(
            model.zonePosition,
            zoneTarget,
            zoneSpeed * dt
        );
    }

    void CheckProgress(float dt)
    {
        float min = model.zonePosition;
        float max = model.zonePosition + model.zoneSize;

        bool inside = model.fishPosition > min && model.fishPosition < max;

        if (inside)
        {
            model.progress += dt * 0.3f;
            model.failTimer = 0;
        }
        else
        {
            model.failTimer += dt;
        }

        model.progress = Mathf.Clamp01(model.progress);

        if (model.failTimer > 2f)
        {
            Debug.Log("Fishing Failed");

            controller.StopMiniGame();
        }

        if (model.progress >= 1f)
        {
            var fish = service.RollFish();

            service.AddFishToInventory(fish);

            Debug.Log("Caught fish: " + fish.fishName);

            controller.StopMiniGame();
        }
    }
}