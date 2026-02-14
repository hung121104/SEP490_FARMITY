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
    public float squashAmount = 0.1f;
    public float stretchAmount = 0.1f;
    public float waveDuration = 0.4f;   
    
    [Header("Flash Settings")]
    public float flashDuration = 0.3f;
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
        
        while (elapsed < waveDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (waveDuration / 2);
            transform.localScale = new Vector3(
                originalScale.x,
                Mathf.Lerp(originalScale.y, originalScale.y * stretchAmount, progress),
                originalScale.z
            );
            yield return null;
        }
        
        elapsed = 0f;
        
        while (elapsed < waveDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (waveDuration / 2);
            transform.localScale = new Vector3(
                originalScale.x,
                Mathf.Lerp(originalScale.y * stretchAmount, originalScale.y * squashAmount, progress),
                originalScale.z
            );
            yield return null;
        }
        
        elapsed = 0f;
        
        while (elapsed < waveDuration / 4)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (waveDuration / 4);
            transform.localScale = new Vector3(
                originalScale.x,
                Mathf.Lerp(originalScale.y * squashAmount, originalScale.y, progress),
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