using UnityEngine;

public class ChangeColor : MonoBehaviour
{
    public Color newColor = Color.red; // S�tt en standardf�rg i Inspector

    void Start()
    {
        // �ndra objektets materialf�rg vid start
        GetComponent<Renderer>().material.color = newColor;
    }
}
