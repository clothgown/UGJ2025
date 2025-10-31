using UnityEngine;
using TMPro;

public class DamageTest : MonoBehaviour
{
    public DamageNumber damagePrefab;
    public Transform spawnParent;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var dmg = Instantiate(damagePrefab, spawnParent);
            dmg.GetComponent<TextMeshPro>().text = Random.Range(10, 100).ToString();
            dmg.gameObject.SetActive(true);
        }
    }
}
