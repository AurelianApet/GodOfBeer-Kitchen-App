using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class dragEvent : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
{
    //  event that gets fired when a drag begins on this item
    public delegate void OnDragBegin(PointerEventData eventData, int oNo, int mNo);
    public event OnDragBegin OnBeginDragCallback;

    //  event that gets fired when a drag ends on this item
    public delegate void OnDragEnd(PointerEventData eventData, int oNo, int mNo);
    public event OnDragEnd OnEndDragCallback;

    public static dragEvent DraggedItem;
    private bool beingDragged = false;
    public int oNo = -1;
    public int mNo = -1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (beingDragged)
        {
            PointerEventData data = new PointerEventData(EventSystem.current);
            OnEndDrag(data);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if ((eventData != null && eventData.used) || DraggedItem != null)
        {
            return;
        }

        Debug.Log("begin drag");
        if (OnBeginDragCallback != null)
        {
            OnBeginDragCallback(eventData, oNo, mNo);
        }
        DraggedItem = this;
        beingDragged = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (DraggedItem == null)
        {
            return;
        }

        DraggedItem = null;
        beingDragged = false;
        Debug.Log("end drag");
        //  trigger necessary events
        if (OnEndDragCallback != null)
        {
            OnEndDragCallback(eventData, oNo, mNo);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.used)
        {
            return;
        }

        if (DraggedItem == null)
        {
            OnBeginDrag(eventData);
        }
    }
}
