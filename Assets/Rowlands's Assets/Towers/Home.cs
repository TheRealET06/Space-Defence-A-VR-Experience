using System;
using UnityEngine;
using UnityEngine.Events;

public class Home : MonoBehaviour
{
    [SerializeField] private float health = 100f;
    public float degPerS = 0.0f;
    public UnityEvent myEvent;
    public bool gameOver = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{
        
    //}

    // Update is called once per frame
    void Update()
    {
        if (gameOver) { return; }
        if (degPerS != 0)
        {
            transform.Rotate(0, degPerS * Time.deltaTime, 0);
        }

        if (health < 0)
        {
            gameOver = true;
            myEvent.Invoke();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the overlapping object has a specific tag
        if (other.CompareTag("Enemy"))
        {
            Enemystats enemy = other.GetComponentInParent<Enemystats>();
            health -= enemy.danger;
            Destroy(other);
        }
    }
}
