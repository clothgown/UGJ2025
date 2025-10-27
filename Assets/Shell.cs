using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Shell : MonoBehaviour
{
    public TextMeshProUGUI shellText;
    // Start is called before the first frame update
    void Start()
    {


    }

    // Update is called once per frame
    void Update()
    {
        if (FindAnyObjectByType<CollectionManager>() != null)
        {
            shellText.text = FindAnyObjectByType<CollectionManager>().coins.ToString();
        }
    }
}
