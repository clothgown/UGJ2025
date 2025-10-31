using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager instance;
    public int coins=0;
    public int thisLevelGetCoins;

    public bool isEntered1_5;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 检查当前场景名称是否为"1-5"
        if (scene.name == "1-5 1 laukinwang")
        {
            isEntered1_5 = true;
            Debug.Log("已进入场景1-5，isEntered1_5设置为true");
        }
        else
        {
            // 可选：如果不在场景1-5时重置标志
            // isEntered1_5 = false;
        }
    }

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
