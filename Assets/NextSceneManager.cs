using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextSceneManager : MonoBehaviour
{
    public string nextSceneName;
    public bool canChange;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

            if(canChange) {
                if(FindAnyObjectByType<AllPlayerState>())
            {
                FindAnyObjectByType<AllPlayerState>().UpdateUnitStates();
            }
                    
                    SceneManager.LoadScene(nextSceneName);
            }
        
    }
}
