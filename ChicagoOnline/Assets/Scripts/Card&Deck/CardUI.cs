using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField]
    private float cardPosOffsetX = 60;

    public int cardIndex;

    RectTransform rect;

    private float selectedTargetYPos;
    private float idleYPos;
    private float targetXPos;
    private float targetYPos;

    private bool hover;
    public bool selected;

    public PlayerScript playerScript;
    private int childIndex = 0;

    
    // Start is called before the first frame update
    void Start()
    {
        rect = GetComponent<RectTransform>();
        idleYPos = rect.localPosition.y;
        //targetXPos = rect.position.x;
        selectedTargetYPos = idleYPos + 100;

        if (playerScript)
        {
            playerScript.myTurn.OnValueChanged += ResetCards;
        }
        childIndex = transform.GetSiblingIndex();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetPos = new(targetXPos, targetYPos);
        rect.localPosition = Vector3.Lerp(rect.localPosition, targetPos, Time.deltaTime * 10);

    }

    public void UpdateXPos()
    {
        targetXPos = cardIndex * cardPosOffsetX;
    }

    void ResetCards(bool prevValue, bool newValue)
    {
        targetYPos = 0;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!playerScript.myTurn.Value) return;

        selected = !selected;

        if (selected)
        {
            targetYPos = selectedTargetYPos;
            playerScript.SelectCard(cardIndex);
        }
        else
        {
            targetYPos = idleYPos;
            playerScript.UnselectCard(cardIndex);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hover = true;
        
        if (!selected)
        {
            targetYPos = 20f;
        }

        // Sets Card UI on top
        transform.SetSiblingIndex(transform.parent.childCount - 1);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hover = false;
        if (!selected)
        {
            targetYPos = 0f;
        }

        // Sets Card UI on top
        transform.SetSiblingIndex(childIndex);
    }
}
