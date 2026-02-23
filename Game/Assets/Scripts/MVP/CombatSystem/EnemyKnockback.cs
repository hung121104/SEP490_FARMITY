using UnityEngine;
using System.Collections;

public class EnemyKnockback : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    [Header("Knockback Settings")]
    public float knockbackPushDistance = 3f;
    public float squashPixels = 0.05f; // Absolute scale change (very small)
    public float stretchPixels = 0.05f; // Absolute scale change (very small)
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
        originalScale = transform.localScale;
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void Knockback(Transform playerTransform, float knockbackForce)
    {
        Vector2 direction = (transform.position - playerTransform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * knockbackForce, 0);
        
        StartCoroutine(WaveEffect());
        StartCoroutine(FlashRed());
    }
    
    private IEnumerator WaveEffect()
    {
        float elapsed = 0f;
        float targetStretch = originalScale.y + stretchPixels;
        float targetSquash = originalScale.y - squashPixels;
        
        // Stretch Y (absolute pixels)
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
        
        // Squash Y (absolute pixels)
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
        
        // Return to normal
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