using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager instance;
    public int coins=0;
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

    public void AddCoin(int coin)
    {
        coins += coin;
    }
}
