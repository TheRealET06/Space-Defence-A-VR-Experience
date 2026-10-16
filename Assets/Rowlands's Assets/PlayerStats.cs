using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    private double money = 0;
    public double crystals = 0;
    public double gas = 0;
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

    public void changeCrystals(double _crystals) { crystals += _crystals; }
    public void changeGas(double _gas) { gas += _gas; }
}
