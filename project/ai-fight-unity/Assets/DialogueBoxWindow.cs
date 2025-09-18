using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.Shared.UI;

public class DialogueBoxWindow : HudWindow
{
    RectTransform rectTransform;
    DialogueHandler dialogueHandler;
    public DialogueHandler DialogueHandler { get { return dialogueHandler; } }

    public float horizontalScale = 1920;
    public float contentBoxSize = -350f;

    private float currentHorizonatlScale;
    private RectTransform dialogueContentBox;
    private CanvasGroup portraitGroup;

    protected override void Awake()
    {
        base.Awake();
        rectTransform = GetComponent<RectTransform>();
        dialogueContentBox = transform.Find("DialogueContentBox").GetComponent<RectTransform>();
        portraitGroup = transform.Find("DialoguePortrait").GetComponent<CanvasGroup>();
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, horizontalScale);
        currentHorizonatlScale = horizontalScale;
        SetPortraitVisibility(true);
    }

    public void Initialize(DialogueHandler handler)
    {
        dialogueHandler = handler;
    }

    public void SetHorizontalScale(float scale)
    {
        currentHorizonatlScale = scale;
        rectTransform.sizeDelta = new Vector2(currentHorizonatlScale, 350f);
    }

    public void ResetHorizontalScale()
    {
        currentHorizonatlScale = horizontalScale;
        rectTransform.sizeDelta = new Vector2(currentHorizonatlScale, 350f);
    }

    public void SetPortraitVisibility(bool state)
    {
        if (state)
        {
            dialogueContentBox.sizeDelta = new Vector2(contentBoxSize, 0f);
            portraitGroup.alpha = 1f;
        }
        else
        {
            // Force both Left and Right to 0 regardless of pivot.
            var min = dialogueContentBox.offsetMin; // (left, bottom)
            var max = dialogueContentBox.offsetMax; // (right, top)
            min.x = 0f;             // Left = 0
            max.x = 0f;             // Right = 0
            dialogueContentBox.offsetMin = min;
            dialogueContentBox.offsetMax = max;
            portraitGroup.alpha = 0f;
        }
    }
}
