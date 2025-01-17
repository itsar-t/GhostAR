// Required libraries for Unity and AR functionality
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

// Class defining the behavior of ghost objects in the AR environment
public class GhostBehaviour : MonoBehaviour
{
    //=== Public Variables ===

    // Tracks the number of hits the ghost has received
    public int HitCount { get; private set; } = 0;

    // Speed of vertical movement for the ghost
    public float moveSpeed;

    // Height of the vertical movement
    public float moveHeight;

    // Speed of ghost rotation
    public float rotationSpeed;

    // Reference to the GameManager for position validation
    public GameManager gameManager;

    // Indicates whether the ghost is currently moving to a new position
    public bool IsMoving { get; private set; } = false;

    // === Private Variables ===

    // The initial position of the ghost
    private Vector3 initialPosition;

    // Target position used for interpolated movement
    private Vector3 targetPosition;

    // Tracks whether the ghost is currently being moved using Lerp
    private bool isLerping = false;

    // Renderer component for visual effects and color changes
    private Renderer ghostRenderer;

    // Original color of the ghost
    private Color originalColor;

    // Color used to indicate the ghost has been hit
    private Color hurtColor = new Color(1f, 0f, 0f, 0.7f); // Red with 70% transparency

    // Initialization function called when the object is created
    void Start()
    {
        // Store the initial position for reference in movement calculations
        initialPosition = transform.position;

        // Randomize the ghost's movement properties for variety
        moveSpeed = Random.Range(3f, 6f); // Vertical speed between 3 and 6 units
        moveHeight = Random.Range(0.5f, 1.5f); // Vertical movement range between 0.5 and 1.5 units
        rotationSpeed = Random.Range(30f, 100f); // Rotation speed between 30 and 100 degrees per second

        // Get the Renderer component for color manipulation
        ghostRenderer = GetComponentInChildren<Renderer>();

        if (ghostRenderer != null)
        {
            // Ensure a unique material instance for each ghost
            ghostRenderer.material = new Material(ghostRenderer.material);
            originalColor = ghostRenderer.material.color; // Save the original color
            Debug.Log($"Renderer found on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"Renderer not found on {gameObject.name}!");
        }
    }

    // Called once per frame to update ghost behavior
    void Update()
    {
        // Calculate vertical oscillation using a sine wave
        float newY = Mathf.Sin(Time.time * moveSpeed) * moveHeight;
        Vector3 offset = new Vector3(0, newY, 0); // Vertical displacement only

        // Update position based on whether the ghost is moving to a new target
        if (!isLerping)
        {
            transform.position = initialPosition + offset;
        }
        else
        {
            transform.position = targetPosition + offset;
        }

        // Rotate the ghost around the Y-axis
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        Debug.Log($"Ghost {gameObject.name} updated position in Update: {transform.position}, initialPosition: {initialPosition}");
    }

    // Increment the hit counter when the ghost is hit
    public void IncrementHitCount()
    {
        HitCount++; // Increase hit count

        // Update the ghost's size and speed
        UpdateSizeAndSpeed();

        // Change ghost color based on hit count
        UpdateColorBasedOnHits();
    }

    // Adjust the ghost's size and movement speed based on the number of hits
    private void UpdateSizeAndSpeed()
    {
        // Reduce the ghost's size by 0.2 units per hit, ensuring a minimum size of 0.2
        Vector3 newScale = transform.localScale - new Vector3(0.2f, 0.2f, 0.2f);
        transform.localScale = Vector3.Max(newScale, new Vector3(0.2f, 0.2f, 0.2f));

        // Increase movement speed by 2 units per hit
        moveSpeed += 2f;

        Debug.Log($"Ghost {gameObject.name} updated size to {transform.localScale} and move speed to {moveSpeed}");
    }

    // Change the ghost's color to indicate hits
    private void UpdateColorBasedOnHits()
    {
        if (ghostRenderer != null)
        {
            if (HitCount == 1)
            {
                // Change color to red for the first hit
                ghostRenderer.material.color = hurtColor;
                Debug.Log($"Ghost changed to hurt color: {hurtColor}");
            }
            else if (HitCount >= 2)
            {
                // Reset to original color before potential destruction
                ghostRenderer.material.color = originalColor;
                Debug.Log($"Ghost reset to original color: {originalColor}");
            }
        }
        else
        {
            Debug.LogWarning("Cannot change color; Renderer is null.");
        }
    }

    // Smoothly move the ghost to a new position over a given duration
    public IEnumerator MoveToPosition(Vector3 newPosition, float duration)
    {
        IsMoving = true; // Mark the ghost as moving
        Vector3 startPosition = transform.position; // Starting position for interpolation
        float elapsed = 0f; // Timer for the interpolation process

        while (elapsed < duration)
        {
            // Interpolate position based on elapsed time
            targetPosition = Vector3.Lerp(startPosition, newPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            isLerping = true; // Indicate interpolation is in progress
            yield return null; // Wait for the next frame
        }

        // Finalize position and reset movement state
        targetPosition = newPosition;
        initialPosition = newPosition; // Update base position for future oscillations
        isLerping = false;
        IsMoving = false; // Mark the movement as complete
    }

    // Move the ghost to a new position on a valid plane
    public void MoveToNewPosition(List<ARPlane> planes)
    {
        if (planes.Count == 0)
        {
            Debug.LogWarning("No planes available for ghost movement!");
            return;
        }

        // Select a random target plane
        ARPlane targetPlane = planes[Random.Range(0, planes.Count)];
        Vector3 newPosition = transform.position; // Default to current position

        int maxAttempts = 10; // Maximum number of attempts to find a valid position
        float minimumDistance = 1.5f; // Minimum distance from other objects

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate a random position within the bounds of the plane
            Vector3 randomOffset = new Vector3(
                Random.Range(-targetPlane.size.x / 2, targetPlane.size.x / 2),
                0.5f,
                Random.Range(-targetPlane.size.y / 2, targetPlane.size.y / 2)
            );

            Vector3 candidatePosition = targetPlane.transform.TransformPoint(randomOffset);

            // Check if the position is valid using GameManager's validation method
            if (gameManager != null && gameManager.IsPositionValid(candidatePosition, minimumDistance))
            {
                newPosition = candidatePosition; // Accept the valid position
                break;
            }
        }

        // If no valid position found, calculate a random far position
        if (newPosition == transform.position)
        {
            Vector3 farRandomOffset = Random.insideUnitSphere * 3f; // Generate random offset
            newPosition += farRandomOffset;
        }

        // Start the movement to the new position
        StartCoroutine(MoveToPosition(newPosition, 0.5f));
        Debug.Log($"Ghost {gameObject.name} moved to new position: {newPosition}");
    }
}
