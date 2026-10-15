using UnityEngine;
using UnityEngine.Events;

public class Enemystats : MonoBehaviour
{
    [SerializeField] private float health = 100f;
    [SerializeField] private Transform aimPoint;

    public UnityEvent myEvent;
    private float sustained_dmg = 0;
    public bool IsDead = false;
    public Vector3 AimPoint => aimPoint != null ? aimPoint.position : transform.position + Vector3.up;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (IsDead) { return; }
        health -= Time.deltaTime * sustained_dmg;
        if(health < 0)
        {
            IsDead = true;
            myEvent.Invoke();
            Destroy(gameObject);
        }
    }

    public void Hurt(float dmg) {  health -= dmg;}
    public void ticiking_hurt(float dmg) { sustained_dmg += dmg;}

}
