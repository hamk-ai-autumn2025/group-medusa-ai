using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.Shared.UI;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class PopupWindow : HudWindow
    {
        [Header("Prefabs")]
        [SerializeField] private TextPopup textPopup;
        [SerializeField] private SliderPopup sliderPopup;

        [Header("Timing")]
        [SerializeField] private float textPopupDelay = 0.5f;

        private Canvas canvas;
        private SliderPopup currentSlider;
        private WaitForSeconds wait;
        private readonly Queue<TextPopupRequest> textPopupQueue = new();
        private bool isProcessingTextQueue;
        private RectTransform rootRect;
        private float nextTextSpawnTimeUnscaled = 0f;

        private readonly struct TextPopupRequest
        {
            public readonly Vector3 screenPosition;
            public readonly string text;
            public TextPopupRequest(Vector3 screenPosition, string text)
            {
                this.screenPosition = screenPosition;
                this.text = text;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            rootRect = (RectTransform)transform;
            if (canvas == null)
                canvas = transform.GetComponentInParents<Canvas>();
        }

        private Camera UICamera =>
            canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

        public void SpawnTextPopup(Transform location, Vector2 offset, string text)
        {
            Camera cam = UICamera != null ? UICamera : Camera.main;
            Vector3 worldPosition = location.position + (Vector3)offset;
            Vector3 screenPosition = cam != null ? cam.WorldToScreenPoint(worldPosition) : worldPosition;
            SpawnTextPopup(screenPosition, text);
        }

        public void SpawnTextPopup(Vector3 screenPosition, string text)
        {
            textPopupQueue.Enqueue(new TextPopupRequest(screenPosition, text));
            if (!isProcessingTextQueue)
            {
                isProcessingTextQueue = true;
                StartCoroutine(ProcessTextPopupQueue());
            }
        }

        private IEnumerator ProcessTextPopupQueue()
        {
            while (textPopupQueue.Count > 0)
            {
                // Gate by unscaled time to ensure visible spacing
                float wait = nextTextSpawnTimeUnscaled - Time.unscaledTime;
                if (wait > 0f)
                    yield return new WaitForSecondsRealtime(wait);

                var req = textPopupQueue.Dequeue();

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        rootRect, req.screenPosition, UICamera, out Vector2 uiPosition))
                {
                    TextPopup p = Instantiate(textPopup, transform);
                    p.GetComponent<RectTransform>().anchoredPosition = uiPosition;
                    p.Show(req.text);
                }

                nextTextSpawnTimeUnscaled = Time.unscaledTime + textPopupDelay;

                // Let layout/anim kick a frame before next potential wait
                yield return null;
            }

            isProcessingTextQueue = false;
        }

        public void SpawnSliderPopup(Transform location, Vector2 offset, int value, int maxValue)
        {
            Camera cam = UICamera != null ? UICamera : Camera.main;
            Vector3 worldPosition = location.position + (Vector3)offset;
            Vector3 screenPosition = cam != null ? cam.WorldToScreenPoint(worldPosition) : worldPosition;
            SpawnSliderPopup(screenPosition, value, maxValue);
        }

        public void SpawnSliderPopup(Vector3 screenPosition, int value, int maxValue)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootRect,
                    screenPosition,
                    UICamera,
                    out Vector2 uiPosition))
            {
                if (currentSlider == null)
                {
                    currentSlider = Instantiate(sliderPopup, transform);
                    currentSlider.GetComponent<RectTransform>().anchoredPosition = uiPosition;
                }

                currentSlider.Show(value, maxValue);
            }
        }
    }
}