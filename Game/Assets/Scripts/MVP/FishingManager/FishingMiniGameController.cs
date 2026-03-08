using UnityEngine;

public class FishingMiniGameController : MonoBehaviour
{
    public FishingView view;
    public FishingService fishingService;

    [Header("Player")]
    private PlayerMovement playerMovement;

    private FishingPresenter presenter;
    private FishingModel model;

    void Start()
    {
        model = new FishingModel();

        presenter = new FishingPresenter(
            model,
            view,
            fishingService,
            this
        );

        view.Hide();
    }

    void Update()
    {
        presenter.Update(Time.deltaTime);
    }

    public void StartMiniGame()
    {
        // ❗ turn off movement
        if (playerMovement != null)
            playerMovement.enabled = false;

        presenter.Start();
    }

    public void StopMiniGame()
    {
        // ❗ turn on movement
        if (playerMovement != null)
            playerMovement.enabled = true;

        presenter.Stop();
    }
}