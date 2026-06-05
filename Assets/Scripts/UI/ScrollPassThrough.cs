using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollPassThrough : MonoBehaviour, IScrollHandler
{
    private ScrollRect parentScrollRect;

    void Awake()
    {
        // Automatically find the nearest parent ScrollRect.
        parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (parentScrollRect != null)
        {
            parentScrollRect.OnScroll(eventData);
        }
    }
}
