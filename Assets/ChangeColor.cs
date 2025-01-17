using UnityEngine;

public class ChangeColor : MonoBehaviour
{
    public Color newColor = Color.red; // Sätt en standardfärg i Inspector

    void Start()
    {
        // Ändra objektets materialfärg vid start
        GetComponent<Renderer>().material.color = newColor;
    }
}
