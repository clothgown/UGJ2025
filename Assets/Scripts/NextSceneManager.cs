using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextSceneManager : MonoBehaviour
{
    public string nextSceneName;
    public bool canChange;
    public EnemyUnit enemyMaid;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        if (canChange)
        {
            
            if((Input.GetMouseButtonDown(0)))
            {
                if (FindAnyObjectByType<AllPlayerState>())
                {
                    FindAnyObjectByType<AllPlayerState>().UpdateUnitStates();
                }
                
                SceneManager.LoadScene(nextSceneName);
                FindAnyObjectByType<LevelSelectRoot>().SetGridManagerTrue();
            }
            
        }

    }

    public void EnemySpawn()
    {
        enemyMaid.gameObject.SetActive(true);
    }
}
