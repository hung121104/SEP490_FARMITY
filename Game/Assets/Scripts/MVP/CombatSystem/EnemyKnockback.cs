using UnityEngine;
using System.Collections;

public class EnemyKnockback : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    [Header("Knockback Settings")]
    public float knockbackPushDistance = 3f; // Horizontal push distance
    public float squashAmount = 0.1f; // How much to squash (0.7 = 30% shorter)
    public float stretchAmount = 0.1f; // How much to stretch (1.3 = 30% taller)
    public float waveDuration = 0.4f; // How long the wave animation lasts
    
    [Header("Flash Settings")]
    public float flashDuration = 0.3f;
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
        // Horizontal knockback push
        Vector2 direction = (transform.position - playerTransform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * knockbackForce, 0);
        
        // Visual wave effect (squash and stretch on Y axis)
        StartCoroutine(WaveEffect());
        
        // Flash red
        StartCoroutine(FlashRed());
        
        Debug.Log("Knockback applied with wave effect.");
    }
    
    private IEnumerator WaveEffect()
    {
        float elapsed = 0f;
        
        // Wave up (stretch Y - enemy gets taller)
        while (elapsed < waveDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (waveDuration / 2);
            // Keep X scale, change Y scale
            transform.localScale = new Vector3(originalScale.x, Mathf.Lerp(originalScale.y, originalScale.y * stretchAmount, progress), originalScale.z);
            yield return null;
        }
        
        elapsed = 0f;
        
        // Wave down (squash Y - enemy gets shorter)
        while (elapsed < waveDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (waveDuration / 2);
            // Keep X scale, change Y scale
            transform.localScale = new Vector3(originalScale.x, Mathf.Lerp(originalScale.y * stretchAmount, originalScale.y * squashAmount, progress), originalScale.z);
            yield return null;
        }
        
        elapsed = 0f;
        
        // Wave back to normal
        while (elapsed < waveDuration / 4)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (waveDuration / 4);
            transform.localScale = new Vector3(originalScale.x, Mathf.Lerp(originalScale.y * squashAmount, originalScale.y, progress), originalScale.z);
            yield return null;
        }
        
        // Ensure it's back to original
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