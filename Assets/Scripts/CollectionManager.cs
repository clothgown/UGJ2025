using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager instance;
    public int coins=0;
    public int thisLevelGetCoins;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update


    // Update is called once per frame
    void Update()
    {
        
    }
    public int GetCoinCount()
    {
        return coins; // 假设 coinCount 是你的金币变量
    }

    
    public void AddCoin(int coin)
    {
        coins += coin;
        if(FindAnyObjectByType<NextSceneManager>()!=null)
        {
            thisLevelGetCoins += coin;
        }
    }
}
