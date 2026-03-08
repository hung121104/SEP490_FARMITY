using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BookMenuController : MonoBehaviour
{
    [Header("Book GameObject")]
    [SerializeField] private GameObject book;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform targetPosition;
    [SerializeField] private Image bookImage;

    [Header("Movement")]
    [SerializeField] private float moveDuration = 0.7f;
    [Header("Delay")]
    [SerializeField] private float delay;

    [Header("Scale")]
    [SerializeField] private float scaleDuration = 0.5f;
    [SerializeField] private float scaleTarget =15;

    [Header("On start animation")]
    [SerializeField] private string onStartAnimationName = "turnRToL";

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup loginCanvasGroup;
    [SerializeField] private CanvasGroup registerCanvasGroup;
    [SerializeField] private CanvasGroup titleCanvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button openBookBtn;
    [SerializeField] private Button closeBookBtn;
    [SerializeField] private Button openLoginBtn;
    [SerializeField] private Button openRegisterBtn;
    [SerializeField] private Button openTitleBtn;

    private string turnRToL = "turnRToL";
    private string turnLToR = "turnLToR";
    private Coroutine _scalingCoroutine;
    private Coroutine _movingCoroutine;

    private void Awake()
    {
        PlayOnstartAnimation(onStartAnimationName);
        BindButtons();
    }

    private void BindButtons()
    {
        if (openBookBtn != null) openBookBtn.onClick.AddListener(ShowBook);
        if (closeBookBtn != null) closeBookBtn.onClick.AddListener(HideBook);
        if (openLoginBtn != null) openLoginBtn.onClick.AddListener(ShowLogin);
        if (openRegisterBtn != null) openRegisterBtn.onClick.AddListener(ShowRegister);
        if (openTitleBtn != null) openTitleBtn.onClick.AddListener(ShowTitle);
    }

    [ContextMenu("show book")]
    public void ShowBook()
    {
        book.SetActive(!book.activeInHierarchy);
        PlayOnstartAnimation(onStartAnimationName);
        if (_movingCoroutine != null) StopCoroutine(_movingCoroutine);
        _movingCoroutine = StartCoroutine(MoveBook());
    }

    [ContextMenu("hide book")]
    public void HideBook()
    {
        StartCoroutine(HideBookRoutine());
    }

    private IEnumerator HideBookRoutine()
    {
        animator.SetTrigger(turnRToL);
        ScalingBook(scaleTarget);
        yield return _scalingCoroutine;
        bookImage.enabled = false;
        if (_movingCoroutine != null) StopCoroutine(_movingCoroutine);
        _movingCoroutine = StartCoroutine(MoveBook());
        animator.SetTrigger(turnRToL);
    }
    IEnumerator MoveBook()
    {
        yield return new WaitForSeconds(delay * 0.7f);
        Debug.Log("[BookMenu] enable book image");
        bookImage.enabled = true;
        yield return new WaitForSeconds(delay * 0.3f);
        //scale down book
        ScalingBook(scaleTarget);
        Debug.Log("[BookMenu] finished delay");
        Debug.Log("[BookMenu] preparing to move book");

        if (targetPosition == null)
        {
            Debug.LogWarning("[BookMenu] targetPosition is not assigned.");
            yield break;
        }

        Vector3 moveTarget = targetPosition.position;
        Vector3 startPos = book.transform.position;
        float elapsed = 0f;

        // Move over a fixed duration using Lerp, stepped at 32fps
        const float frameInterval = 1f / 32f;
        while (elapsed < moveDuration)
        {
            elapsed += frameInterval;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            book.transform.position = Vector3.Lerp(startPos, moveTarget, t);
            yield return new WaitForSeconds(frameInterval);
        }

        book.transform.position = moveTarget;
        Debug.Log("[BookMenu] moved");
        yield return new WaitForSeconds(.5f);
        _movingCoroutine = null;
    }


    public void ShowLogin()
    {
        loginCanvasGroup.alpha = 1f;
        loginCanvasGroup.interactable = true;
        loginCanvasGroup.blocksRaycasts = true;
        animator.SetTrigger(turnRToL);
    }

    public void ShowRegister()
    {
    }

    public void ShowTitle()
    {
        animator.SetTrigger(turnLToR);
    }

    /// <summary>
    /// Smoothly scales the book down to zero over <see cref="scaleDuration"/> seconds,
    /// then deactivates it.
    /// </summary>
    public void ScalingBook(float targetScale)
    {
        if (_scalingCoroutine != null)
            StopCoroutine(_scalingCoroutine);
        _scalingCoroutine = StartCoroutine(Scaling(targetScale));
    }

    private IEnumerator Scaling(float targetScale)
    {
        yield return new WaitForSeconds(delay * .3f);
        Vector3 startScale = book.transform.localScale;
        Vector3 endScale = new Vector3(targetScale, targetScale, targetScale);
        float elapsed = 0f;

        const float frameInterval = 1f / 32f;
        while (elapsed < scaleDuration)
        {
            elapsed += frameInterval;
            float t = Mathf.Clamp01(elapsed / scaleDuration);
            book.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return new WaitForSeconds(frameInterval);
        }

        book.transform.localScale = endScale;
        Debug.Log($"[BookMenu] scaling complete → {targetScale}");
        _scalingCoroutine = null;
    }
    private void PlayOnstartAnimation(string animation)
    {
        animator.SetTrigger(animation);
    }
}
