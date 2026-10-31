using System;
using UnityEngine;
using UnityEngine.Events;

public class Home : MonoBehaviour
{
    [SerializeField] private float health = 100f;
    public float degPerS = 0.0f;
    public UnityEvent myEvent;
    public bool gameOver = false;
    private PlayerStats pStats;
    public CurrencyManager currency;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pStats = UnityEngine.Object.FindAnyObjectByType<PlayerStats>();
        currency = UnityEngine.Object.FindAnyObjectByType<CurrencyManager>();
    }

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
            Application.Quit();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the overlapping object has a specific tag
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();
            health -= enemy.danger;
            Destroy(other);
        }
    }

    public void trade()
    {
        if (pStats.crystals > 0)
        {
            pStats.changeCrystals(-1);
            currency.AddMoney(50);
        }
    }
}
