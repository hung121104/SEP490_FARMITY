using UnityEngine;
using UnityEngine.EventSystems;
using System;


public class MouseHoverTrigger : MonoBehaviour
{
    public Action OnHoverEnter;
    public Action OnHoverExit;

    private Collider2D _collider;
    private bool _isHovering;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        // Reset so Update() re-evaluates hover state fresh on next frame.
        _isHovering = false;
    }

    private void OnDisable()
    {
        // If hovering when disabled (e.g. returned to pool), fire exit event.
        if (_isHovering)
        {
            _isHovering = false;
            OnHoverExit?.Invoke();
        }
    }

    private void Update()
    {
        if (_collider == null || Camera.main == null) return;

        // Block hover detection when the pointer is over a UI element
        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool currentlyHovering = !pointerOverUI && _collider.OverlapPoint(mousePos);

        if (currentlyHovering && !_isHovering)
        {
            _isHovering = true;
            OnHoverEnter?.Invoke();
        }
        else if (!currentlyHovering && _isHovering)
        {
            _isHovering = false;
            OnHoverExit?.Invoke();
        }
    }
}
