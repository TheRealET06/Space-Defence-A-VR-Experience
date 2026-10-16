using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class GasExtractor : MonoBehaviour
{
    public UnityEvent laserStart;
    public UnityEvent laserEnd;
    public float charge = 0;
    public float duration = 0;
    private float zdegPerS = 0;
    public bool on = false;
    private GasPlanet planet;
    public double gasExtracted = 0;

    private PlayerStats pStats;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //xdegPerS = Random.Range(5, 10);
        pStats = Object.FindAnyObjectByType<PlayerStats>();
        zdegPerS = Random.Range(5, 10);
        transform.Rotate(Random.Range(0, 90), Random.Range(0, 180), 0);
        planet = GetComponentInParent<GasPlanet>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, 0, zdegPerS * Time.deltaTime);
        if (on)
        {
            duration += Time.deltaTime;
            if (duration > 1f)
            {
                on = false;
                charge = 0;
                laserEnd.Invoke();
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
                gasExtracted = planet.Extract();
            }
        }

    }

    public void transfer()
    {
        pStats.changeGas(gasExtracted);
        gasExtracted = 0;
    }
}
