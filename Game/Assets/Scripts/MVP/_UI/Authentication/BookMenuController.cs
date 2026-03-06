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

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup loginCanvasGroup;
    [SerializeField] private CanvasGroup registerCanvasGroup;
    [SerializeField] private CanvasGroup titleCanvasGroup;

    [Header("Page Animator")]
    [SerializeField] private Animator pageAnimator;

    [Header("Buttons")]
    [SerializeField] private Button openBookBtn;
    [SerializeField] private Button closeBookBtn;
    [SerializeField] private Button openLoginBtn;
    [SerializeField] private Button openRegisterBtn;
    [SerializeField] private Button openTitleBtn;

    private bool checkInitAnimation = true;
    private string turnRToL = "turnRToL";
    private string turnLToR = "turnLToR";

    private void Awake()
    {
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
        StartCoroutine(MoveBook());

    }

    IEnumerator MoveBook()
    {
        yield return new WaitForSeconds(delay * 0.8f);
        Debug.Log("[BookMenu] enable book image");
        bookImage.enabled = true;
        yield return new WaitForSeconds(delay * 0.2f);
        Debug.Log("[BookMenu] finished delay");
        Debug.Log("[BookMenu] preparing to move book");
        ScaleDownBook();
        if (targetPosition == null)
        {
            Debug.LogWarning("[BookMenu] targetPosition is not assigned.");
            yield break;
        }

        Vector3 moveTarget = targetPosition.position;
        Vector3 startPos = book.transform.position;
        float elapsed = 0f;

        // Move over a fixed duration using Lerp
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            book.transform.position = Vector3.Lerp(startPos, moveTarget, t);
            yield return null;
        }

        book.transform.position = moveTarget;
        Debug.Log("[BookMenu] moved");
        yield return new WaitForSeconds(.5f);


    }

    [ContextMenu("hide book")]
    public void HideBook()
    {
        animator.SetTrigger(turnRToL);
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
    }

    /// <summary>
    /// Smoothly scales the book down to zero over <see cref="scaleDuration"/> seconds,
    /// then deactivates it.
    /// </summary>
    public void ScaleDownBook()
    {
        StartCoroutine(ScaleDown());
    }

    private IEnumerator ScaleDown()
    {
        Vector3 startScale = book.transform.localScale;
        Vector3 endScale = new Vector3(15f, 15f, 15f);
        float elapsed = 0f;

        while (elapsed < scaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scaleDuration);
            book.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        book.transform.localScale = endScale;
        Debug.Log("[BookMenu] scale down complete");
    }
}
