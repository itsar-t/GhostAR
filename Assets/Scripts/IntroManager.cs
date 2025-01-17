using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;
using FMODUnity; // Import FMOD namespace for audio management

public class IntroManager : MonoBehaviour

    // === Public Variables ===
{
    // ARPlaneManager to manage plane detection and tracking in the AR environment
    public ARPlaneManager planeManager;

    // UI element to display status messages to the player
    public TextMeshProUGUI statusText;

    // Reference to the GameManager object to transition to gameplay
    public GameObject gameManager;

    // === Private Variables ===

    // Required total area (in square meters) to progress in the game
    private float requiredArea = 15f;

    // Accumulated area of detected planes
    private float scannedArea = 0f;

    // Interval (in seconds) between plane area calculations
    private float scanCheckInterval = 1f;

    // Reference to FMOD audio emitter for intro music
    [SerializeField] private StudioEventEmitter introBip;

    private void Start()
    {
        // Ensure ARPlaneManager is assigned, find it dynamically if not already set
        planeManager ??= FindObjectOfType<ARPlaneManager>();

        // Play intro music if the FMOD emitter is assigned
        if (introBip != null)
        {
            introBip.Play();
        }
        else
        {
            Debug.LogWarning("Intro Music FMOD Event Emitter not assigned!");
        }

        // Start scanning AR planes
        StartCoroutine(ScanPlanes());
    }

    private IEnumerator ScanPlanes()
    {
        // Set initial UI message to inform the player about scanning process
        statusText.text = "Scanning for paranormal signals...";

        // Continue scanning until the required area is reached
        while (scannedArea < requiredArea)
        {
            // Calculate the total scanned area by summing up areas of detected planes
            scannedArea = CalculateTotalPlaneArea();

            // Update the UI with the progress
            statusText.text = $"Searching for paranormal activity... \nScanned Area: {scannedArea:F2}m² / {requiredArea}m²";

            // Wait for the specified interval before the next calculation
            yield return new WaitForSeconds(scanCheckInterval);
        }

        // Notify the player that the required area has been detected
        statusText.text = "Paranormal activity detected!";

        // Wait a moment before transitioning to the next stage
        yield return new WaitForSeconds(2f);

        // Disable the AR plane detection updates
        planeManager.enabled = false;

        // Hide the visual representation of all AR planes in the scene
        foreach (var plane in planeManager.trackables)
        {
            // Disable the ARPlaneMeshVisualizer, which renders plane meshes
            var meshVisualizer = plane.GetComponent<ARPlaneMeshVisualizer>();
            if (meshVisualizer != null)
            {
                meshVisualizer.enabled = false;
            }

            // Disable the plane's renderer to hide its visual representation
            var renderer = plane.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        // Activate the GameManager object to transition to gameplay
        gameManager.SetActive(true);

        // Stop the intro music
        if (introBip != null)
        {
            introBip.Stop();
        }

        // Deactivate this script's GameObject
        gameObject.SetActive(false);
    }

    private float CalculateTotalPlaneArea()
    {
        // Initialize total area
        float totalArea = 0f;

        // Iterate over all detected AR planes and calculate their combined area
        foreach (var plane in planeManager.trackables)
        {
            // Area is approximated as the product of plane dimensions (width * height)
            totalArea += plane.size.x * plane.size.y;
        }

        // Return the total calculated area
        return totalArea;
    }
}
