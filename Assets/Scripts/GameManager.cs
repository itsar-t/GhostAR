using System.Collections.Generic; // Provides access to collections such as List
using UnityEngine; // Unity-specific namespace for core game development functions
using UnityEngine.XR.ARFoundation; // Namespace for working with AR functionality such as ARPlaneManager
using TMPro; // Namespace for managing text with TextMeshPro
using FMODUnity; // Namespace for integrating FMOD for audio management

public class GameManager : MonoBehaviour
{
    // === Public Variables ===
    public ARPlaneManager planeManager; // Manages AR planes for detecting and tracking flat surfaces
    public GameObject ghostPrefab; // Prefab used to instantiate ghost objects in the game
    public GameObject projectilePrefab; // Prefab used to instantiate projectiles
    public Camera arCamera; // Reference to the AR camera for projecting projectile direction
    public TextMeshProUGUI timerText; // UI text element to display the game timer
    public TextMeshProUGUI statusText; // UI text element to display the game status

    // FMOD Studio event emitters for playing various sounds
    [SerializeField] private StudioEventEmitter fmodEmitter; // Plays background sound for the game
    [SerializeField] private StudioEventEmitter shootingSound; // Plays sound when a projectile is fired
    [SerializeField] private StudioEventEmitter ghostHit; // Plays sound when a ghost is hit
    [SerializeField] private StudioEventEmitter victory; // Plays victory sound when all ghosts are defeated

    // === Private Variables ===
    private List<GameObject> ghosts = new List<GameObject>(); // Keeps track of all active ghost objects
    private float timer = 0f; // Tracks the elapsed time since the game started
    private float lastShootTime = -1f; // Tracks the time when the last projectile was fired
    private float shootCooldown = 1f; // Cooldown time between successive shots
    private bool gameActive = true; // Indicates if the game is currently active
    private int firedProjectiles = 0; // Counter to track the total number of projectiles fired

    // Start is called before the first frame update
    void Start()
    {
        // Spawns initial ghosts at the start of the game
        SpawnInitialGhosts();

        // Logs the positions and parent information of all spawned ghosts
        foreach (var ghost in ghosts)
        {
            Debug.Log($"Initial Ghost {ghost.name} position: {ghost.transform.position}, parent: {ghost.transform.parent}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Updates the game timer and UI while the game is active
        if (gameActive)
        {
            timer += Time.deltaTime; // Increment the timer by the time elapsed since the last frame
            UpdateTimerUI(); // Update the timer display on the UI

            // Detects touch input to initiate projectile shooting
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                TryShootProjectile(); // Attempt to fire a projectile
            }
        }
    }

    // Spawns the initial set of ghosts in the game
    private void SpawnInitialGhosts()
    {
        List<ARPlane> planes = new List<ARPlane>(); // Collect all detected AR planes
        foreach (var plane in planeManager.trackables)
        {
            planes.Add(plane); // Add each ARPlane to the list
        }

        // If no planes are available, display a warning and return
        if (planes.Count == 0)
        {
            Debug.LogWarning("No planes available for spawning ghosts!");
            return;
        }

        // Spawn 5 ghosts randomly across the detected planes
        for (int i = 0; i < 5; i++)
        {
            ARPlane targetPlane = planes[Random.Range(0, planes.Count)]; // Select a random plane

            // Generate a random position on the selected plane
            Vector3 randomOffset = new Vector3(
                Random.Range(-targetPlane.size.x / 2, targetPlane.size.x / 2),
                0.5f, // Offset ghosts slightly above the plane
                Random.Range(-targetPlane.size.y / 2, targetPlane.size.y / 2)
            );
            Vector3 spawnPosition = targetPlane.transform.TransformPoint(randomOffset);

            // Ensure ghosts do not spawn too close to the AR camera
            if (Vector3.Distance(spawnPosition, arCamera.transform.position) < 2.0f)
            {
                spawnPosition += targetPlane.transform.forward * 2.0f; // Push the position farther from the camera
            }

            // Play background sound when spawning ghosts
            if (fmodEmitter != null)
            {
                fmodEmitter.Play();
            }

            SpawnGhost(spawnPosition); // Instantiate the ghost at the calculated position
        }
    }

    // Spawns a single ghost at a specific position
    private void SpawnGhost(Vector3 initialPosition)
    {
        float minimumDistance = 1.5f; // Minimum distance required between ghosts
        int maxAttempts = 10; // Maximum attempts to find a valid spawn position
        Vector3 position = initialPosition; // Start with the provided position

        // Try to find a valid position for the ghost
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (IsPositionValid(position, minimumDistance))
            {
                break; // Exit the loop if a valid position is found
            }

            // Generate a new random position if the current one is invalid
            position += new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
        }

        // Instantiate the ghost at the final position
        GameObject ghost = Instantiate(ghostPrefab, position, Quaternion.identity);

        // Randomly scale the ghost to create size variations
        float randomScale = Random.Range(0.4f, 1.2f);
        ghost.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

        // Ensure the ghost has no parent object
        ghost.transform.SetParent(null);

        // Pass the GameManager reference to the ghost for interaction
        GhostBehaviour ghostBehaviour = ghost.GetComponent<GhostBehaviour>();
        if (ghostBehaviour != null)
        {
            ghostBehaviour.gameManager = this;
        }

        ghosts.Add(ghost); // Add the ghost to the active ghost list
        Debug.Log($"Spawned ghost: {ghost.name}. Total ghosts: {ghosts.Count}");
    }

    // Tries to shoot a projectile and checks for cooldown
    private void TryShootProjectile()
    {
        float remainingCooldown = shootCooldown - (Time.time - lastShootTime); // Calculate cooldown remaining

        if (remainingCooldown <= 0f)
        {
            ShootProjectile(); // Shoot the projectile
            lastShootTime = Time.time; // Update the last shot time
            statusText.text = "Paranormal activity detected!\nGun reloaded."; // Update status text
        }
        else
        {
            // Update status text to show remaining cooldown time
            statusText.text = $"Paranormal activity detected!\nGun reloaded in {remainingCooldown:F1}s...";
            Debug.Log("Cannot shoot yet! Cooldown is active.");
        }
    }

    // Instantiates a projectile and applies velocity
    private void ShootProjectile()
    {
        GameObject projectile = Instantiate(projectilePrefab, arCamera.transform.position, Quaternion.identity); // Spawn the projectile

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.velocity = arCamera.transform.forward * 10f; // Propel the projectile forward

        firedProjectiles++; // Increment the projectile counter
        UpdateTimerUI(); // Update the UI with the updated projectile count

        if (shootingSound != null)
        {
            shootingSound.Stop(); // Stop any existing shooting sound
            shootingSound.Play(); // Play the shooting sound
        }
    }

    // Handles what happens when a ghost is hit by a projectile
    public void HandleGhostHit(GameObject ghost, GameObject projectile)
    {
        Debug.Log($"HandleGhostHit called for {ghost.name} and {projectile.name}");

        // Play ghost hit sound using FMOD
        if (ghostHit != null)
        {
            ghostHit.Stop(); // Stop any existing hit sound
            ghostHit.Play(); // Play the hit sound
        }

        // Destroy the projectile after it hits the ghost
        Destroy(projectile);

        // Retrieve the GhostBehaviour script attached to the ghost
        GhostBehaviour ghostBehaviour = ghost.GetComponent<GhostBehaviour>();
        if (ghostBehaviour != null)
        {
            ghostBehaviour.IncrementHitCount(); // Increment the ghost's hit counter
            Debug.Log($"Ghost {ghost.name} HitCount: {ghostBehaviour.HitCount}");

            // Check if the ghost has been hit enough times to be destroyed
            if (ghostBehaviour.HitCount >= 2)
            {
                // Remove the ghost from the list of active ghosts
                if (ghosts.Contains(ghost))
                {
                    ghosts.Remove(ghost);
                    Debug.Log($"Removed ghost: {ghost.name}. Remaining ghosts: {ghosts.Count}");
                }
                else
                {
                    // Log a warning if the ghost is not in the list
                    Debug.LogWarning($"Ghost {ghost.name} was not found in the list! Listing all ghosts:");
                    foreach (var g in ghosts)
                    {
                        Debug.Log($"Ghost in list: {g.name}");
                    }
                }

                // Destroy the ghost object
                Destroy(ghost);

                // Check if there are no ghosts left
                if (ghosts.Count == 0)
                {
                    // Play the victory sound if all ghosts are destroyed
                    if (fmodEmitter != null && victory != null)
                    {
                        fmodEmitter.Stop(); // Stop background sound
                        victory.Play(); // Play the victory sound
                    }

                    EndGame(); // End the game
                }
            }
            else
            {
                // If the ghost is not destroyed, attempt to move it to a new position
                List<ARPlane> planes = new List<ARPlane>();
                foreach (var plane in planeManager.trackables)
                {
                    planes.Add(plane); // Add all detected planes to a list
                }

                if (planes.Count > 0)
                {
                    Debug.Log($"Moving ghost to a new position. Available planes: {planes.Count}");
                    ghostBehaviour.MoveToNewPosition(planes); // Move the ghost to a new position
                }
                else
                {
                    Debug.LogWarning("No planes available! Ghost cannot be moved.");
                }
            }
        }
    }


    // Checks if a position is valid based on the minimum distance from other ghosts
    public bool IsPositionValid(Vector3 position, float minimumDistance)
    {
        foreach (var ghost in ghosts)
        {
            if (Vector3.Distance(position, ghost.transform.position) < minimumDistance)
            {
                return false; // Position is too close to another ghost
            }
        }
        return true; // Position is valid
    }

    // Updates the game timer and projectile count on the UI
    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(timer / 60f); // Calculate elapsed minutes
        int seconds = Mathf.FloorToInt(timer % 60f); // Calculate elapsed seconds
        int milliseconds = Mathf.FloorToInt((timer * 100f) % 100f); // Calculate elapsed milliseconds

        // Display the timer and the number of fired projectiles
        timerText.text = $"{minutes:D2}:{seconds:D2}:{milliseconds:D2}\nShoots Fired: {firedProjectiles}";
    }

    // Ends the game and displays a message
    private void EndGame()
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false); // Hide the status text
        }

        gameActive = false; // Set game as inactive
        timerText.text += " - Good Job!"; // Append a message to the timer text
        Debug.Log("Game Over");
    }
}
