using UnityEngine;

// This script handles the collision detection for ghosts.
// It checks for collisions with objects tagged as "Projectile" and communicates with the GameManager to handle the ghost hit.

public class GhostCollision : MonoBehaviour
{
    // This method is triggered when another collider enters the trigger collider attached to this GameObject.
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger detected on {gameObject.name} with {other.gameObject.name}");

        // Check if the colliding object is tagged as "Projectile".
        if (other.CompareTag("Projectile"))
        {
            // Find the GameManager in the scene to handle the ghost hit logic.
            GameManager manager = FindObjectOfType<GameManager>();
            if (manager != null)
            {
                // Call the HandleGhostHit method in GameManager to process the ghost hit.
                manager.HandleGhostHit(gameObject, other.gameObject);
            }

            // Destroy the projectile to prevent repeated collisions.
            Destroy(other.gameObject);
        }
    }
}
