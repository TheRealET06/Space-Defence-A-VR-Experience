using UnityEngine;
using UnityEngine.Events;

public class CrystalAsteroid : MonoBehaviour
{
    public double gems = 1000;
    private double gemUpdate = 0;
    public bool depleated = false;
    public UnityEvent gemsGone;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        depleated = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(!depleated)
        { 
            gems -= Time.deltaTime * gemUpdate; 
            depleated = gems <= 0;
            gemsGone.Invoke();
        }
    }

    public void UpdateGems(double gemsPS)
    {
        gemUpdate = gemsPS;
    }
}
