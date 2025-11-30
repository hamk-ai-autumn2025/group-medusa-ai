using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using dev.susybaka.TurnBasedGame.Input;

namespace dev.susybaka.TurnBasedGame.UI
{
    [RequireComponent(typeof(LabelWindow))]
    public class CaptureTextInput : MonoBehaviour
    {
        [SerializeField] private string defaultPrefill = "<i><color=grey>Type your action...</color></i>";
        public string DefaultPrefill => defaultPrefill;
        [SerializeField] private int maxInputLength = 50;
        [SerializeField] private bool blockProfanity = true;

        // Use fully qualified name in case of namespace/type clash
        private ProfanityFilter.ProfanityFilter filter = new ProfanityFilter.ProfanityFilter();
        private InputHandler input;
        private LabelWindow labelWindow;
        private string currentText = string.Empty;
        private bool isRunning;
        private bool confirmed;
        private bool inputCaptured;

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
            inputCaptured = false;

            currentText = prefill ?? string.Empty;
            labelWindow.SetText(currentText);

            // local helper
            void ApplyFrameInput(string frameInput)
            {
                if (string.IsNullOrEmpty(frameInput))
                    return;
                else if (!inputCaptured)
                {
                    inputCaptured = true;
                    currentText = string.Empty; // clear prefill on first input
                }

                for (int i = 0; i < frameInput.Length; i++)
                {
                    char c = frameInput[i];

                    if (c == '\b') // backspace
                    {
                        if (currentText.Length > 0)
                            currentText = currentText.Substring(0, currentText.Length - 1);

                        if (currentText.Length < 1)
                        {
                            currentText = prefill; // restore prefill if fully deleted
                            inputCaptured = false;
                        }
                    }
                    else if (c == '\n' || c == '\r')
                    {
                        confirmed = true;

                        // Profanity check on confirm and censor if needed
                        if (blockProfanity)
                        {
                            if (filter.ContainsProfanity(currentText))
                            {
                                currentText = filter.CensorString(currentText);
                            }
                        }
                    }
                    else
                    {
                        if (currentText.Length >= maxInputLength) // enforce max length
                            return;

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