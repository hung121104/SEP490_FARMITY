using UnityEngine;
using System.Collections;

public class PlayerKnockback : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerMovement playerMovement;
    private Coroutine knockbackRoutine;

    [Header("Knockback Settings")]
    public float knockbackDuration = 0.15f;
    public float squashPixels = 0.05f; // Absolute scale change
    public float stretchPixels = 0.05f; // Absolute scale change
    public float waveDuration = 0.3f;
    
    [Header("Flash Settings")]
    public float flashDuration = 0.2f;
    public int flashCount = 2;
    
    private Color originalColor;
    private Vector3 originalScale;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerMovement = GetComponent<PlayerMovement>();
        originalScale = transform.localScale;
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void Knockback(Transform enemyTransform, float knockbackForce)
    {
        Vector2 direction = (transform.position - enemyTransform.position).normalized;
        Vector2 velocity = direction * knockbackForce;

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
        }

        knockbackRoutine = StartCoroutine(ApplyKnockback(velocity));

        StartCoroutine(WaveEffect());
        StartCoroutine(FlashRed());
    }

    private IEnumerator ApplyKnockback(Vector2 velocity)
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }

        yield return new WaitForSeconds(knockbackDuration);

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }

    private IEnumerator WaveEffect()
    {
        float elapsed = 0f;
        float targetStretch = originalScale.y + stretchPixels;
        float targetSquash = originalScale.y - squashPixels;
        
        // Stretch
        while (elapsed < waveDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (waveDuration / 2);
            transform.localScale = new Vector3(
                originalScale.x,
                Mathf.Lerp(originalScale.y, targetStretch, progress),
                originalScale.z
            );
            yield return null;
        }
        
        elapsed = 0f;
        
        // Squash
        while (elapsed < waveDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (waveDuration / 2);
            transform.localScale = new Vector3(
                originalScale.x,
                Mathf.Lerp(targetStretch, targetSquash, progress),
                originalScale.z
            );
            yield return null;
        }
        
        elapsed = 0f;
        
        // Return
        while (elapsed < waveDuration / 4)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (waveDuration / 4);
            transform.localScale = new Vector3(
                originalScale.x,
                Mathf.Lerp(targetSquash, originalScale.y, progress),
                originalScale.z
            );
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    private IEnumerator FlashRed()
    {
        if (spriteRenderer == null) yield break;
        
        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(flashDuration / 2);
            
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration / 2);
        }
    }
}