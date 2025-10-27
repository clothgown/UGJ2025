using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomExit : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Cancel()
    {
        Destroy(gameObject);
    }

    public void Exit()
    {
        Debug.Log("Exit");
        SceneManager.LoadScene("Map");
    }
}
