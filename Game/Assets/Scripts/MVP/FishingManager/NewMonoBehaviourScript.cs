using System;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] Transform topPivot;
    [SerializeField] Transform bottomPivot;

    [SerializeField] Transform fish;
    
    float fishPosition ;
    float fishDestination ;
    float fishTimer;
    [SerializeField] float timerMutilicator = 3f;
    float fishSpeed ;
    [SerializeField] float smoothMotion = 1f;

    [SerializeField] Transform hook;
    float hookPosition;
    [SerializeField] float hookSize = 0.1f;
    [SerializeField] float hookPower = 5f;

    float hookProgress;
    float hookPullVelocity;
    [SerializeField] float hookPullPower = 0.01f;
    [SerializeField] float hoookGravityPower = 0.005f;
    [SerializeField] float hookProgessDegradationPower = 0.1f;

    [SerializeField] SpriteRenderer hookspriteRenderer;

    [SerializeField] Transform progressBarContainer;

    bool pause = false;

    [SerializeField] float failTimer = 10f;

    private void Start()
    {
       Resize();
    }

  
    private void Update()
    {
        if (pause) { return; }
       Fish();
        Hook();
        ProgressCheck();
    }

    private void Resize()
    {
        Bounds b = hookspriteRenderer.bounds;
        float ySize = b.size.y;
        Vector3 ls = hook.localScale;
        float distance = Vector3.Distance(topPivot.position, bottomPivot.position);
        ls.y = distance / ySize * hookSize;
        hook.localScale = ls;
    }
    private void ProgressCheck()
    {
        Vector3 ls = progressBarContainer.localScale;
        ls.y = hookProgress;
        progressBarContainer.localScale = ls;

        float min = hookPosition - hookSize / 2;
        float max = hookPosition + hookSize / 2;


        if( min < fishPosition && fishPosition < max)
        {
            hookProgress += hookPower * Time.deltaTime;
        }
        else
        {
            hookProgress -= hookProgessDegradationPower * Time.deltaTime;
            failTimer -= Time.deltaTime;
            if (failTimer < 0f)
            {
               Lose();
            }
        }
        if ( hookProgress >= 1f)
        {
            Win();
        }

        hookProgress = Mathf.Clamp(hookProgress, 0f,1f);
    }

    private void Lose()
    {
       pause = true;
        Debug.Log("You lose!");
    }

    private void Win()
    {
       pause = true;
        Debug.Log("You win!");
    }

    void Hook()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            hookPullVelocity += hookPullPower * Time.deltaTime;
        }
        hookPullVelocity -= hoookGravityPower * Time.deltaTime;

        hookPosition += hookPullVelocity;

        if (hookPosition - hookSize /2 <= 0f && hookPullVelocity < 0f)
        {
            hookPullVelocity = 0f;
        }
        if (hookPosition + hookSize /2 >= 1f && hookPullVelocity > 0f)
        {
            hookPullVelocity = 0f;
        }

        hookPosition = Mathf.Clamp(hookPosition, hookSize / 2 ,1 - hookSize / 2 );
        hook.position = Vector3.Lerp(bottomPivot.position, topPivot.position, hookPosition);
    }
    void Fish()
    {
        fishTimer -= Time.deltaTime;
        if (fishTimer < 0f)
        {
            fishTimer = UnityEngine.Random.value * timerMutilicator;

            fishDestination = UnityEngine.Random.value;
        }
        fishPosition = Mathf.SmoothDamp(fishPosition, fishDestination, ref fishSpeed, smoothMotion);
        fish.position = Vector3.Lerp(bottomPivot.position, topPivot.position, fishPosition);
    }
}
