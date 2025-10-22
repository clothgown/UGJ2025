using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager instance;
    public int coins=0;
    public TextMeshProUGUI coinText;
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
    void Start()
    {
        coinText.text = coins.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void AddCoin(int coin)
    {
        coins += coin;
        coinText.text = coins.ToString();
    }
}
