using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class RoomExit : MonoBehaviour
{
    public GameObject checkObject;
    private GameObject instantiatedPanel; // 生成的面板实例
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
        if (checkObject != null)
        {
            Canvas canvas = GameObject.Find("UI").GetComponentInChildren<Canvas>();
            // 实例化新的 UI 面板
            instantiatedPanel = Instantiate(checkObject, canvas.transform);
            instantiatedPanel.SetActive(true);
            FindAnyObjectByType<NextSceneManager>().canChange = true;
        }
        Debug.Log("Exit");
        Destroy(gameObject);
        //SceneManager.LoadScene("Map");
    }
}
