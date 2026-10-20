using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    private double money = 0;
    private double moneyUpdate = 0.1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        money = Time.deltaTime * moneyUpdate;
    }
}
