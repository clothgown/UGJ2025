using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CheckPanel : MonoBehaviour
{
    public TextMeshProUGUI totalCoins;
    public TextMeshProUGUI thisLevelCoins;
    // Start is called before the first frame update
    void Start()
    {
        totalCoins.text = CollectionManager.instance.coins.ToString();
        thisLevelCoins.text = CollectionManager.instance.thisLevelGetCoins.ToString();
        CollectionManager.instance.thisLevelGetCoins = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
