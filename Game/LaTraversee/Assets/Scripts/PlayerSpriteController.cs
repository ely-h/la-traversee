using UnityEngine;

/// <summary>
/// Defines the logic for the player's visual state.
/// </summary>
public enum PlayerState
{
    Survivor,
    Infected
}

/// <summary>
/// A struct to group all sprites needed for a single state (Survivor or Infected).
/// </summary>
[System.Serializable]
public struct PlayerStateSprites
{
    [Tooltip("Used when idle or moving down")]
    public Sprite idleOrDown;
    
    [Tooltip("Used when moving up")]
    public Sprite up;
    
    [Tooltip("Used when moving left")]
    public Sprite left;
    
    [Tooltip("Used when moving right")]
    public Sprite right;
}

/// <summary>
/// Manages the player's SpriteRenderer based on direction and state, without using Unity's Animator.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSpriteController : MonoBehaviour
{
    [Header("Sprite Configuration")]
    [Tooltip("Assign the 'sv' sprites here")]
    [SerializeField] private PlayerStateSprites survivorSprites;
    
    [Tooltip("Assign the 'zb' sprites here")]
    [SerializeField] private PlayerStateSprites infectedSprites;

    private SpriteRenderer spriteRenderer;
    private PlayerState currentState = PlayerState.Survivor;
    
    // We store the current facing direction, defaulting to down.
    private Vector2 currentFacingDirection = Vector2.down;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Changes the player state (Survivor / Infected) and immediately refreshes the graphic.
    /// Call this from PlayerCollision.cs when 'Infect()' is triggered.
    /// </summary>
    public void SetState(PlayerState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;
        RefreshSprite(currentFacingDirection, false); // Keep current facing direction, force update
    }

    /// <summary>
    /// Pass the current movement input or velocity to this method to update the sprite direction.
    /// Call this from your movement update loop (e.g., in NetworkManager.cs).
    /// </summary>
    /// <param name="movement">Movement vector (can be absolute or raw input)</param>
    public void UpdateDirection(Vector2 movement)
    {
        bool isMoving = movement.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            // Determine the dominant movement axis to pick a specific 4-way direction
            if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
            {
                currentFacingDirection = movement.x > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                currentFacingDirection = movement.y > 0 ? Vector2.up : Vector2.down;
            }
        }

        RefreshSprite(currentFacingDirection, isMoving);
    }

    /// <summary>
    /// Resolves and applies the correct sprite based on the current state and requested direction.
    /// </summary>
    private void RefreshSprite(Vector2 direction, bool isMoving)
    {
        // Select the relevant sprite struct based on the current state
        PlayerStateSprites activeSprites = (currentState == PlayerState.Survivor) ? survivorSprites : infectedSprites;
        
        Sprite selectedSprite = activeSprites.idleOrDown; // Default base

        // As specified: idle OR moving down both use the same idle/down sprite.
        if (!isMoving || direction == Vector2.down)
        {
            selectedSprite = activeSprites.idleOrDown;
        }
        else if (direction == Vector2.up)
        {
            selectedSprite = activeSprites.up;
        }
        else if (direction == Vector2.left)
        {
            selectedSprite = activeSprites.left;
        }
        else if (direction == Vector2.right)
        {
            selectedSprite = activeSprites.right;
        }

        // Only assign if it's different to save performance
        if (spriteRenderer.sprite != selectedSprite)
        {
            spriteRenderer.sprite = selectedSprite;
        }
    }
}
