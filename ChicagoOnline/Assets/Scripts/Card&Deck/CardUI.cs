using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private float cardPosOffsetX = 60;
    public int cardIndex;
    RectTransform rect;
    private float selectedTargetYPos;
    private float idleYPos;
    private float targetXPos;
    private float targetYPos;

    private bool canSelect = true;
    public bool selected;
    private int childIndex = 0;

    [Header("References")]
    public PlayerScript playerScript;
    [SerializeField] Outline _outline;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        idleYPos = rect.localPosition.y;
        selectedTargetYPos = idleYPos + 100;

        if (playerScript)
        {
            playerScript.myTurn.OnValueChanged += ResetCards;
        }
        childIndex = transform.GetSiblingIndex();
    }
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
        if (!canSelect) return;
        if (!playerScript.myTurn.Value) return;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySelectCard();
        }

        selected = !selected;

        SelectCard(selected);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!canSelect) return;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayHoverCard();
        }
        
        if (!selected)
        {
            targetYPos = 20f;
        }

        // Sets Card UI on top
        transform.SetSiblingIndex(transform.parent.childCount - 1);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!canSelect) return;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayHoverCard();
        }
        if (!selected)
        {
            targetYPos = 0f;
        }

        // Sets Card UI on top
        transform.SetSiblingIndex(childIndex);
    }

    public void SelectCard(bool select)
    {
        if (select)
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

    public void CanSelect(bool selectable)
    {
        canSelect = selectable;

        if (selectable)
        {
            GetComponent<RawImage>().color = Color.white;
        }
        else
        {
            GetComponent<RawImage>().color = Color.gray;
        }
    }

    public void Outline(bool enable)
    {
        _outline.enabled = enable;
    }
}
