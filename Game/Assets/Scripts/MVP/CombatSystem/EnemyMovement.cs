using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    
    [Header("Movement Settings")]
    public float friction = 3f; 
    public float maxVelocity = 10f; 

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("EnemyMovement: No Rigidbody2D found on " + gameObject.name);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;
        
       
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * friction);
        
        
        if (rb.linearVelocity.magnitude > maxVelocity)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
        }
    }
    
    
    public void TakeKnockback(Vector2 knockbackDirection, float knockbackForce)
    {
        if (rb == null) return;
        rb.linearVelocity = knockbackDirection * knockbackForce;
    }
    
    
    public void Stop()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector2.zero;
    }
}