using UnityEngine;

// This script manages the behavior of a projectile (pellet) shot by the player.
// It handles automatic destruction after a certain lifespan and processes collisions with ghosts or other objects.

public class GhostPellet : MonoBehaviour
{
    public float lifespan = 5f; // Time before the projectile destroys itself

    void Start()
    {
        // Destroy the projectile after its defined lifespan to avoid cluttering the scene.
        Destroy(gameObject, lifespan);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the projectile collided with an object tagged as "Ghost".
        if (collision.gameObject.CompareTag("Ghost"))
        {
            // Notify the GameManager about the ghost hit, passing the ghost and this projectile.
            FindObjectOfType<GameManager>().HandleGhostHit(collision.gameObject, gameObject);
        }
        else
        {
            // Destroy the projectile if it hits any other object (not a ghost).
            Destroy(gameObject);
        }
    }
}
