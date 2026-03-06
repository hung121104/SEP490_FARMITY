using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for player pointer state and settings.
    /// Tracks pointer direction, references, and initialization status.
    /// </summary>
    [System.Serializable]
    public class PlayerPointerModel
    {
        #region Settings

        [Header("Settings")]
        public float orbitRadius = 1.5f;
        public float initializationDelay = 0.5f;

        #endregion

        #region References

        [Header("References")]
        public GameObject pointerPrefab = null;
        public Transform playerTransform = null;
        public Transform centerPoint = null;
        public Camera mainCamera = null;
        public Transform pointerTransform = null;
        public SpriteRenderer pointerSpriteRenderer = null;

        #endregion

        #region Direction State

        [Header("Direction State")]
        public Vector3 currentDirection = Vector3.right;

        #endregion

        #region Initialization

        [Header("Initialization")]
        public bool isInitialized = false;

        #endregion

        #region Constructor

        public PlayerPointerModel()
        {
            orbitRadius = 1.5f;
            initializationDelay = 0.5f;
            currentDirection = Vector3.right;
            isInitialized = false;
            
            pointerPrefab = null;
            playerTransform = null;
            centerPoint = null;
            mainCamera = null;
            pointerTransform = null;
            pointerSpriteRenderer = null;
        }

        #endregion
    }
}