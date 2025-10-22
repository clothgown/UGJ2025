using DG.Tweening;
using UnityEngine;
using System.Collections;

public class WormWiggle : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(WiggleCoroutine());
    }

    IEnumerator WiggleCoroutine()
    {
        while (true)
        {
            // 在0.01秒内旋转8度
            transform.DORotate(new Vector3(0, 0, 8), 0.001f, RotateMode.LocalAxisAdd);

            // 等待0.25秒
            yield return new WaitForSeconds(0.25f);
        }
    }
}