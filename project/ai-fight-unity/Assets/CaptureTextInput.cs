using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using dev.susybaka.TurnBasedGame.Input;

namespace dev.susybaka.TurnBasedGame.UI
{
    [RequireComponent(typeof(LabelWindow))]
    public class CaptureTextInput : MonoBehaviour
    {
        private InputHandler input;
        private LabelWindow labelWindow;
        private string currentText = string.Empty;
        private bool isRunning;
        private bool confirmed;

        // Exposed result after coroutine finishes
        public string Result { get; private set; }
        public bool WasCancelled { get; private set; }

        private void Awake()
        {
            labelWindow = GetComponent<LabelWindow>();
            labelWindow.ClearText();
            labelWindow.CloseWindow();

            input = GameManager.Instance.Input;
        }

        /// <summary>
        /// Coroutine that captures text until confirm (input.Confirm rising edge).
        /// Usage:
        ///   yield return StartCoroutine(capture.WaitForTextInput());
        ///   var text = capture.Result; // null if cancelled
        /// </summary>
        public IEnumerator IE_WaitForTextInput(string prefill = "", bool allowEscapeCancel = true)
        {
            if (isRunning)
                yield break; // hard stop; avoid nested capture

            confirmed = false;
            isRunning = true;
            WasCancelled = false;
            Result = null;

            currentText = prefill ?? string.Empty;
            labelWindow.SetText(currentText);

            // local helper
            void ApplyFrameInput(string frameInput)
            {
                if (string.IsNullOrEmpty(frameInput))
                    return;

                for (int i = 0; i < frameInput.Length; i++)
                {
                    char c = frameInput[i];

                    if (c == '\b') // backspace
                    {
                        if (currentText.Length > 0)
                            currentText = currentText.Substring(0, currentText.Length - 1);
                    }
                    else if (c == '\n' || c == '\r')
                    {
                        confirmed = true;
                    }
                    else
                    {
                        currentText += c;
                    }
                }

                labelWindow.SetText(currentText);
            }

            // run per-frame until confirm/cancel
            while (true)
            {
                // Capture typed chars this frame
                ApplyFrameInput(UnityEngine.Input.inputString);

                // Optional cancel with Esc
                if (allowEscapeCancel && input.BackInput)
                {
                    WasCancelled = true;
                    Result = null;
                    break;
                }

                if (confirmed) //input.ConfirmInput
                {
                    Result = currentText;
                    break;
                }

                yield return null; // next frame
            }

            isRunning = false;
        }

        public void ResetText()
        {
            currentText = string.Empty;
            labelWindow.SetText(currentText);
        }
    }
}