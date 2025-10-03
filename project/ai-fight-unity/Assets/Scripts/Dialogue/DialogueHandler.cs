using System;
using System.Collections;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Dialogue.Data;
using dev.susybaka.TurnBasedGame.Input;
using dev.susybaka.TurnBasedGame.UI;
using dev.susybaka.Shared.Audio;

namespace dev.susybaka.TurnBasedGame.Dialogue
{
    public class DialogueHandler : MonoBehaviour
    {
        private GameManager gameManager;
        private InputHandler input;

        public DialogueWindow dialogueBox;
        public DialogueData data;

        private readonly string[] dialogueNewLineIndicator = 
        {
            "<br><br><br>",
            "*<br><br><br>",
            "*<br>*<br><br>",
            "*<br>*<br>*<br>"
        };
        private const string LineBreakToken = "%n";
        private const int MinStaticLines = 3;
        private const char PaddingChar = '\u00A0'; // non-breaking space so TextMeshPro keeps the line

        private Coroutine ieDialogue;
        private bool initialized = false;

        public void Initialize(GameManager manager)
        {
            if (initialized)
                return;

            initialized = true;
            gameManager = manager;
            input = gameManager.Input;

            dialogueBox = manager.currentGameWindow.DialogueBox;

            if (dialogueBox != null)
            {
                dialogueBox.CloseWindow();
            }

            dialogueBox.DialogueContents.richText = true;
            dialogueBox.DialogueContents.alpha = 1f;
            dialogueBox.DialogueContents.text = string.Empty;

            dialogueBox.DialogueNewLineIndicators.text = dialogueNewLineIndicator[0];

            dialogueBox.DialoguePortrait.sprite = null;
            dialogueBox.DialoguePortrait.color = new Color(1f, 1f, 1f, 0f);
        }

        public void StartDialogue(DialogueData data)
        {
            this.data = data;

            dialogueBox = gameManager.currentGameWindow.DialogueBox;

            if (ieDialogue == null)
            {
                ieDialogue = StartCoroutine(IE_ProcessDialogue());
            }
        }

        public IEnumerator IE_QueueDialogue(DialogueData data)
        {
            this.data = data;

            dialogueBox = gameManager.currentGameWindow.DialogueBox;

            yield return IE_ProcessDialogue();
            gameManager.HudNavigationHandler.Root.isActive = false;
        }

        private IEnumerator IE_ProcessDialogue()
        {
            if (dialogueBox != null && !dialogueBox.isOpen && GameManager.HudNavigationHandlerAvailable)
                gameManager.HudNavigationHandler.PushWindow(dialogueBox);
            
            if (data == null || data.dialogue == null)
            {
                Debug.LogWarning("DialogueHandler: No dialogue data to process.");
                CleanupAndClose();
                yield break;
            }

            foreach (var line in data.dialogue)
            {
                string raw = line.text ?? string.Empty;
                int breakCount = CountLineBreaks(raw);

                // Prepare base text and NBSP padding
                string baseText = raw.Replace(LineBreakToken, "\n");

                // strip % p tokens and collect when to switch portraits
                var triggers = new System.Collections.Generic.List<PortraitTrigger>();
                string renderText = StripPortraitTokensAndCollect(baseText, out triggers);

                // Build padding so there are always at least 3 lines present from the start
                int visualLines = CountVisualLines(baseText);
                int toAdd = Mathf.Max(0, MinStaticLines - visualLines);
                System.Text.StringBuilder pad = new System.Text.StringBuilder();
                for (int i = 0; i < toAdd; i++)
                { pad.Append('\n'); pad.Append(PaddingChar); }
                string paddingSuffix = pad.ToString();

                // Set initial/default portrait for this line
                {
                    var portraits = line.speaker?.characterPortraits;
                    int idxDefault = (int)line.portrait;
                    Sprite s = (portraits != null && idxDefault >= 0 && idxDefault < portraits.Length) ? portraits[idxDefault] : null;
                    dialogueBox.DialoguePortrait.sprite = s;
                    dialogueBox.DialoguePortrait.color = new Color(1f, 1f, 1f, s != null ? 1f : 0f);
                }

                // Make layout stable from frame 0
                dialogueBox.DialogueContents.richText = true;
                dialogueBox.DialogueContents.text = "<alpha=#00>" + (renderText + paddingSuffix);
                dialogueBox.DialogueContents.ForceMeshUpdate();

                // Total visible glyphs in renderText (ignores TextMeshPro tags)
                int totalGlyphs = 0;
                { bool inTag = false; for (int i = 0; i < renderText.Length; i++) { char c = renderText[i]; if (c == '<') inTag = true; if (!inTag) totalGlyphs++; if (c == '>') inTag = false; } }

                // If the first token is at glyphIndex 0, apply immediately before first char
                int nextTrigger = 0;
                void ApplyPortraitByIndex(int portraitIndex)
                {
                    var portraits = line.speaker?.characterPortraits;
                    Sprite s = (portraits != null && portraitIndex >= 0 && portraitIndex < portraits.Length) ? portraits[portraitIndex] : null;
                    dialogueBox.DialoguePortrait.sprite = s;
                    dialogueBox.DialoguePortrait.color = new Color(1f, 1f, 1f, s != null ? 1f : 0f);
                }
                while (nextTrigger < triggers.Count && triggers[nextTrigger].glyphIndex <= 0)
                {
                    ApplyPortraitByIndex(triggers[nextTrigger].portraitIndex);
                    nextTrigger++;
                }

                // Indicator starts at first line, because 0 is no lines
                dialogueBox.DialogueNewLineIndicators.text = dialogueNewLineIndicator[1];

                float cps = (line.speed <= 0f) ? float.PositiveInfinity : line.speed;
                float delay = (cps == float.PositiveInfinity) ? 0f : 1f / cps;

                for (int visibleGlyphs = 1; visibleGlyphs <= totalGlyphs; visibleGlyphs++)
                {
                    // Apply any triggers that occur before this glyph shows
                    while (nextTrigger < triggers.Count && triggers[nextTrigger].glyphIndex < visibleGlyphs)
                    {
                        ApplyPortraitByIndex(triggers[nextTrigger].portraitIndex);
                        nextTrigger++;
                    }

                    int rawCut = RawIndexForGlyphCount(renderText, visibleGlyphs);
                    string visible = renderText.Substring(0, rawCut);
                    string hidden = renderText.Substring(rawCut) + paddingSuffix;

                    // Live indicator based on current line started
                    int idx = IndicatorIndexForVisible(visible);
                    dialogueBox.DialogueNewLineIndicators.text = dialogueNewLineIndicator[idx];

                    // Reveal
                    dialogueBox.DialogueContents.text = visible + "<alpha=#00>" + hidden + "</alpha>";

                    // SFX
                    if (!string.IsNullOrEmpty(line.speaker?.characterDialogueSound))
                        AudioManager.Instance.Play(line.speaker.characterDialogueSound);

                    // Per-char delay
                    if (delay > 0f && !input.ConfirmInput)
                        yield return new WaitForSeconds(delay);
                    else
                        yield return null;

                    // Line break pause
                    if (line.lineBreakPause > 0f && visible.Length > 0 && visible[visible.Length - 1] == '\n')
                        yield return new WaitForSeconds(line.lineBreakPause);
                }

                // Show finished line
                dialogueBox.DialogueContents.text = renderText + paddingSuffix;

                // Wait for submit to advance
                while (!input.ConfirmInput)
                    yield return null;
            }

            CleanupAndClose();
        }

        private void CleanupAndClose()
        {
            if (dialogueBox != null && GameManager.HudNavigationHandlerAvailable)
                gameManager.HudNavigationHandler.CloseWindow(dialogueBox);

            DialogueData temp = data;

            ieDialogue = null;
            data = null;

            temp.onComplete?.Invoke();
        }

        private static int CountLineBreaks(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;
            int count = 0;
            int idx = 0;
            while ((idx = s.IndexOf(LineBreakToken, idx, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                idx += LineBreakToken.Length;
            }
            return count;
        }

        private static int CountVisualLines(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 1;
            int lines = 1;
            for (int i = 0; i < s.Length; i++)
                if (s[i] == '\n')
                    lines++;
            return lines;
        }

        // Map a target "glyph count" (excluding TextMeshPro tags) to the raw string index boundary.
        // Ensures we never cut inside a <tag>.
        private static int RawIndexForGlyphCount(string s, int glyphCount)
        {
            if (glyphCount <= 0)
                return 0;
            bool inTag = false;
            int glyphs = 0;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '<')
                    inTag = true;
                if (!inTag)
                {
                    glyphs++;
                    if (glyphs == glyphCount)
                        return i + 1;
                }
                if (c == '>')
                    inTag = false;
            }
            return s.Length;
        }

        private static int CountNewlines(string s)
        {
            int n = 0;
            for (int i = 0; i < s.Length; i++)
                if (s[i] == '\n')
                    n++;
            return n;
        }

        // Indicator should reflect the line that is CURRENTLY being typed.
        // If visible ends with '\n', we're between lines -> don't advance indicator yet.
        private int IndicatorIndexForVisible(string visible)
        {
            int breaks = CountNewlines(visible);
            if (visible.Length > 0 && visible[visible.Length - 1] == '\n')
                breaks = Mathf.Max(0, breaks - 1);

            // map: 0 breaks -> index 1, 1 break -> index 2, 2+ -> index 3
            return Mathf.Clamp(1 + breaks, 1, dialogueNewLineIndicator.Length - 1);
        }

        private struct PortraitTrigger { public int glyphIndex; public int portraitIndex; }

        private static string StripPortraitTokensAndCollect(string input, out System.Collections.Generic.List<PortraitTrigger> triggers)
        {
            var sb = new System.Text.StringBuilder(input.Length);
            triggers = new System.Collections.Generic.List<PortraitTrigger>();

            bool inTag = false; // TextMeshPro rich text tag
            int glyphs = 0;     // visible glyphs (ignores TextMeshPro tags)

            for (int i = 0; i < input.Length;)
            {
                char c = input[i];

                // Pass TextMeshPro tags through verbatim, but don't count as glyphs
                if (c == '<')
                {
                    inTag = true;
                    sb.Append(c);
                    i++;
                    while (i < input.Length)
                    {
                        char t = input[i];
                        sb.Append(t);
                        i++;
                        if (t == '>')
                        {
                            inTag = false;
                            break;
                        }
                    }
                    continue;
                }

                // %pN or %PN => portrait switch control token
                if (c == '%' && i + 2 <= input.Length)
                {
                    char p = input[i + 1];
                    if (p == 'p' || p == 'P')
                    {
                        int j = i + 2;
                        int val = 0;
                        bool hasDigit = false;
                        while (j < input.Length && char.IsDigit(input[j]))
                        {
                            hasDigit = true;
                            val = val * 10 + (input[j] - '0');
                            j++;
                        }
                        if (hasDigit)
                        {
                            // consume token, don't append; trigger at current glyph boundary
                            triggers.Add(new PortraitTrigger { glyphIndex = glyphs, portraitIndex = val });
                            i = j;
                            continue;
                        }
                    }
                }

                // Normal visible char
                sb.Append(c);
                if (!inTag)
                    glyphs++;
                i++;
            }
            return sb.ToString();
        }
    }
}