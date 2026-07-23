using DG.Tweening;
using System.Collections;
using UnityEngine;

public class KTH_StarEvent : MonoBehaviour
{
    [SerializeField] private float moveDistance = 3f;
    [SerializeField] private float duration = 1f;
    [SerializeField]private float delayTime = 1f;
    private void Start()
    {
        StartCoroutine(Delay());
    }

    private IEnumerator Delay()
    {
        yield return new WaitForSeconds(delayTime);

        transform.DOMoveY(transform.position.y - moveDistance, duration)
           .SetEase(Ease.OutBounce);
    }
}
