using System.Collections;
using System.Collections.Generic;
using dev.susybaka.Shared.UI;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class SpeechWindow : HudWindow
    {
        [Header("Speech Window")]
        [SerializeField] private TMPro.TextMeshProUGUI textLabel;
        [SerializeField] private float textSpeed = 0.05f;
        private Coroutine typingCoroutine;
        public bool IsTyping => typingCoroutine != null;
        public void ShowText(string text, bool instant = false)
        {
            OpenWindow();
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            if (instant || textSpeed <= 0f)
            {
                textLabel.text = text;
                typingCoroutine = null;
            }
            else
            {
                typingCoroutine = StartCoroutine(TypeText(text));
            }
        }
        public void ClearText()
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            textLabel.text = string.Empty;
            typingCoroutine = null;
        }
        private IEnumerator TypeText(string text)
        {
            textLabel.text = string.Empty;
            foreach (char c in text)
            {
                textLabel.text += c;
                yield return new WaitForSeconds(textSpeed);
            }
            typingCoroutine = null;
            yield return new WaitForSeconds(2f);
            CloseWindow();
        }
    }
}