using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class GemFactory : MonoBehaviour
{
    private PlayerStats pStats;
    public float procesTime = 0f;
    [SerializeField] GameObject gem;
    public bool done = false;
    public bool working = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pStats = Object.FindAnyObjectByType<PlayerStats>();
    }

    // Update is called once per frame
    void Update()
    {
        if (working) 
        { 
            procesTime += Time.deltaTime;
            float grow = procesTime * 0.1f;
            gem.transform.localScale = new Vector3(grow, grow, grow);
        }
        if (procesTime > 4f && !done)
        {
            done = true;
            working = false;
        }
    }

    public void interact()
    {
        if(done)
        { 
            pStats.changeCrystals(1);
            gem.transform.localScale = new Vector3(0f, 0f, 0f);
            procesTime = 0;
            done = false;
        }
        else if (!working)
        {
            if(pStats.gas > 0) 
            {
                pStats.changeGas(-10);
                working = true;
            }
        }
    }
}
