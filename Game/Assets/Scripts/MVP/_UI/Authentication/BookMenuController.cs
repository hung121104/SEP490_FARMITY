using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BookMenuController : MonoBehaviour
{
    [Header("Book GameObject")]
    [SerializeField] private CanvasGroup book;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform targetPosition;
    [SerializeField] private Image bookImage;
    [SerializeField] private bool hideBookOnStart = true;
    [SerializeField] private bool onStartAnimation = true;

    [Header("Animation Selection")]
    [SerializeField] private BookAnimation onStartAnimationTrigger = BookAnimation.TurnLToR;
    [SerializeField] private BookAnimation onShowBookAnimationTrigger = BookAnimation.TurnLToR;
    [SerializeField] private BookAnimation onTransitionInAnimationTrigger = BookAnimation.TurnRToL;

    [Header("Movement")]
    [SerializeField] private float moveDuration = 0.7f;
    [Header("Delay")]
    [SerializeField] private float delay;

    [Header("Scale")]
    [SerializeField] private float scaleDuration = 0.5f;
    [SerializeField] private float scaleTarget =15;

    [Header("Buttons")]
    [SerializeField] private Button openBookBtn;
    [SerializeField] private Button closeBookBtn;

    [Header("Panel Controller")]
    [SerializeField] private BookPanelController panelController;

    private Coroutine _scalingCoroutine;
    private Coroutine _movingCoroutine;

    private void Awake()
    {
        BindButtons();

        if (book == null)
        {
            Debug.LogWarning("[BookMenu] 'book' CanvasGroup is not assigned on BookMenuController. Please assign it in the Inspector.");
        }

        if (hideBookOnStart)
        {
            if (book != null) book.Hide();
            bookImage.enabled = false;
        }

        if (onStartAnimation)
            PlayAnimation(onStartAnimationTrigger);

        if (panelController != null)
        {
            panelController.OnPanelOpened += (panelAnimation) => 
            {
                PlayAnimation(panelAnimation, resetAllTriggers: true);
            };
        }
    }

    private void BindButtons()
    {
        if (openBookBtn != null) openBookBtn.onClick.AddListener(ShowBook);
        if (closeBookBtn != null) closeBookBtn.onClick.AddListener(TranstionIn);
    }

    [ContextMenu("show book")]
    public void ShowBook()
    {
        if (book == null)
        {
            Debug.LogWarning("[BookMenu] ShowBook called but 'book' reference is missing. Reassign the CanvasGroup in the Inspector.");
            return;
        }

        book.Show();
        PlayAnimation(onShowBookAnimationTrigger);
        if (_movingCoroutine != null) StopCoroutine(_movingCoroutine);
        _movingCoroutine = StartCoroutine(MoveBook());
    }

    [ContextMenu("Transtion In")]
    public void TranstionIn()
    {
        StartCoroutine(TranstionInRoutine());
    }

    private IEnumerator TranstionInRoutine()
    {
        if (book == null)
        {
            Debug.LogWarning("[BookMenu] TranstionIn aborted: 'book' reference missing.");
            yield break;
        }

        ScalingBook(scaleTarget);
        yield return _scalingCoroutine;
       
        if (_movingCoroutine != null) StopCoroutine(_movingCoroutine);
        _movingCoroutine = StartCoroutine(MoveBook());
        yield return _movingCoroutine;
        bookImage.enabled = false;
        panelController.HideAllPanels();
        PlayAnimation(onTransitionInAnimationTrigger, resetAllTriggers: true);
        yield return new WaitForSeconds(0.75f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("AuthScene");

    }
    IEnumerator MoveBook()
    {
        yield return new WaitForSeconds(delay * 0.7f);
        Debug.Log("[BookMenu] enable book image");
        bookImage.enabled = true;
        yield return new WaitForSeconds(delay * 0.3f);
        //scale down book
        if (book == null)
        {
            Debug.LogWarning("[BookMenu] MoveBook aborted: 'book' reference missing.");
            yield break;
        }

        ScalingBook(scaleTarget);
        Debug.Log("[BookMenu] finished delay");
        Debug.Log("[BookMenu] preparing to move book");

        if (targetPosition == null)
        {
            Debug.LogWarning("[BookMenu] targetPosition is not assigned.");
            yield break;
        }

        Vector3 moveTarget = targetPosition.position;
        Vector3 startPos;
        try
        {
            startPos = book.transform.position;
        }
        catch (MissingReferenceException ex)
        {
            Debug.LogWarning($"[BookMenu] MoveBook aborted: 'book' reference became invalid when reading transform. book==null? {book == null}. Exception: {ex.Message}");
            if (!System.Object.ReferenceEquals(book, null))
            {
                try { Debug.Log($"[BookMenu] book.gameObject: {book.gameObject.name}, activeInHierarchy: {book.gameObject.activeInHierarchy}"); } catch { Debug.LogWarning("[BookMenu] Could not access book.gameObject"); }
            }
            yield break;
        }
        float elapsed = 0f;

        // Move over a fixed duration using Lerp, stepped at 24fps
        const float frameInterval = 1f / 24f;
        while (elapsed < moveDuration)
        {
            elapsed += frameInterval;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            try
            {
                book.transform.position = Vector3.Lerp(startPos, moveTarget, t);
            }
            catch (MissingReferenceException ex)
            {
                Debug.LogWarning($"[BookMenu] Aborting MoveBook: 'book' became invalid during move. Exception: {ex.Message}");
                yield break;
            }
            yield return new WaitForSeconds(frameInterval);
        }

        book.transform.position = moveTarget;
        Debug.Log("[BookMenu] moved");
        yield return new WaitForSeconds(.5f);
        _movingCoroutine = null;
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
        if (book == null)
        {
            Debug.LogWarning("[BookMenu] Scaling aborted: 'book' reference missing.");
            yield break;
        }

        Vector3 startScale;
        try
        {
            startScale = book.transform.localScale;
        }
        catch (MissingReferenceException ex)
        {
            Debug.LogWarning($"[BookMenu] Scaling aborted: 'book' reference became invalid when reading transform. book==null? {book == null}. Exception: {ex.Message}");
            if (!System.Object.ReferenceEquals(book, null))
            {
                try { Debug.Log($"[BookMenu] book.gameObject: {book.gameObject.name}, activeInHierarchy: {book.gameObject.activeInHierarchy}"); } catch { Debug.LogWarning("[BookMenu] Could not access book.gameObject"); }
            }
            yield break;
        }
        Vector3 endScale = new Vector3(targetScale, targetScale, targetScale);
        float elapsed = 0f;

        const float frameInterval = 1f / 24f;
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
    private void PlayAnimation(BookAnimation animation, bool resetAllTriggers = false)
    {
        if (animator == null) return;

        if (resetAllTriggers)
            ResetAllAnimationTriggers();

        animator.SetTrigger(animation.ToTriggerName());
    }

    private void ResetAllAnimationTriggers()
    {
        animator.ResetTrigger(BookAnimations.TurnRToL);
        animator.ResetTrigger(BookAnimations.TurnLToR);
        animator.ResetTrigger(BookAnimations.ResetState);
    }
}
