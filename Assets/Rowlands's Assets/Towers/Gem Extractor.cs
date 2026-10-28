using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class GemExtractor : MonoBehaviour
{
    public UnityEvent laserStart;
    public UnityEvent laserEnd;
    public float charge = 0;
    public float duration = 0;
    private float zdegPerS = 0;
    public bool on = false;
    private CrystalAsteroid asteroid;
    public float minedGems = 0;
    public float minerate = 1f;
    private PlayerStats pStats;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //xdegPerS = Random.Range(5, 10);
        pStats = Object.FindAnyObjectByType<PlayerStats>();
        zdegPerS = Random.Range(5, 10);
        transform.Rotate(Random.Range(0, 90), Random.Range(0, 180), 0);
        asteroid = GetComponentInParent<CrystalAsteroid>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, 0, zdegPerS * Time.deltaTime);
        if(!asteroid.depleated)
        {
            if (on)
            {
                duration += Time.deltaTime;
                minedGems += Time.deltaTime * minerate;
                if (duration > 1f)
                {
                    on = false;
                    charge = 0;
                    laserEnd.Invoke();
                    asteroid.UpdateGems(0);
                }
            }
            else
            {
                charge += Time.deltaTime;
                if (charge > 5f)
                {
                    on = true;
                    duration = 0;
                    laserStart.Invoke();
                    asteroid.UpdateGems(minerate);
                }
            }
        }

    }

    public void transfer()
    {
        pStats.changeCrystals(minedGems);
        minedGems = 0;
    }
}
