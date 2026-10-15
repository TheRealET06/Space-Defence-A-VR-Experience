using UnityEngine;

public class CrystalAsteroid : MonoBehaviour
{
    private double gems = 1000;
    private double gemUpdate = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        gems = Time.deltaTime * gemUpdate;
    }
}
