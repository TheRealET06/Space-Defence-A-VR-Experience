using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class GasExtractor : MonoBehaviour
{
    public UnityEvent myEvent;
    public float duration = 100f;
    private float timer;

    void Start()
    {
        timer = duration;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer < 0)
        {
            myEvent.Invoke();
            timer = duration;
        }
    }
}
