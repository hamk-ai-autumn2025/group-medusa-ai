using System;
using System.Collections;
using dev.susybaka.Shared.Attributes;
using dev.susybaka.Shared.Audio;
using dev.susybaka.Shared.UI;
using dev.susybaka.TurnBasedGame.Core;
using dev.susybaka.TurnBasedGame.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class DialogueHandler : MonoBehaviour
{
    public DialogueBoxWindow dialogueBox;
    public DialogueData data;

    private InputHandler input;
    private TextMeshProUGUI dialogueContents;
    private TextMeshProUGUI dialogueNewLineIndicators;
    private Image dialoguePortrait;
    
    private readonly string[] dialogueNewLineIndicator =
    {
        "<br><br><br>",
        "*<br><br><br>",
        "*<br>*<br><br>",
        "*<br>*<br>*<br>"
    };
    private const string LineBreakToken = "%n";
    private const int MinStaticLines = 3;
    private const char PaddingChar = '\u00A0'; // non-breaking space so TMP keeps the line

    private Coroutine ieDialogue;

    private void Start()
    {
        input = InputHandler.instance;

        if (dialogueBox != null)
        {
            dialogueBox.Initialize(this);
            dialogueBox.SetHorizontalScale(dialogueBox.horizontalScale);
            dialogueBox.CloseWindow();
        }
        dialogueContents = dialogueBox.transform.Find("DialogueContentBox").GetChild(0).GetComponent<TextMeshProUGUI>();
        dialogueNewLineIndicators = dialogueBox.transform.Find("DialogueContentBox").GetChild(1).GetComponent<TextMeshProUGUI>();
        dialoguePortrait = dialogueBox.transform.Find("DialoguePortrait").GetComponent<Image>();

        if (dialogueContents != null)
        {
            dialogueContents.richText = true;
            dialogueContents.alpha = 1f;
            dialogueContents.text = string.Empty;
        }

        if (dialogueNewLineIndicators != null)
            dialogueNewLineIndicators.text = dialogueNewLineIndicator[0];

        if (dialoguePortrait != null)
        {
            dialoguePortrait.sprite = null;
            dialoguePortrait.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void StartDialogue(DialogueData data)
    {
        this.data = data;

        Debug.Log("skibidi 1");

        if (ieDialogue == null)
        {
            Debug.Log("skibidi 2");
            ieDialogue = StartCoroutine(IE_ProcessDialogue());
        }
    }

    public void ResetDialogue()
    {
        StopAllCoroutines();
        ieDialogue = null;
    }

    private IEnumerator IE_ProcessDialogue()
    {
        if (dialogueBox != null && !dialogueBox.isOpen)
            dialogueBox.OpenWindow();

        Debug.Log("skibidi 3");

        // defensive guard
        if (data == null || data.dialogue == null)
        {
            Debug.LogWarning("DialogueHandler: No dialogue data to process.");
            CleanupAndClose();
            yield break;
        }

        Debug.Log("skibidi 4");

        foreach (var line in data.dialogue)
        {
            string raw = line.text ?? string.Empty;
            int breakCount = CountLineBreaks(raw);

            // Prepare base text and NBSP padding (as you already have)
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

            // Set initial/default portrait for this line (your original logic):
            {
                var portraits = line.speaker?.characterPortraits;
                int idxDefault = (int)line.portrait;
                Sprite s = (portraits != null && idxDefault >= 0 && idxDefault < portraits.Length) ? portraits[idxDefault] : null;
                dialoguePortrait.sprite = s;
                dialoguePortrait.color = new Color(1f, 1f, 1f, s != null ? 1f : 0f);
            }

            // Make layout stable from frame 0
            dialogueContents.richText = true;
            dialogueContents.text = "<alpha=#00>" + (renderText + paddingSuffix);
            dialogueContents.ForceMeshUpdate();

            // total visible glyphs in renderText (ignores TMP tags)
            int totalGlyphs = 0;
            { bool inTag = false; for (int i = 0; i < renderText.Length; i++) { char c = renderText[i]; if (c == '<') inTag = true; if (!inTag) totalGlyphs++; if (c == '>') inTag = false; } }

            // If the first token is at glyphIndex 0, apply immediately (before first char)
            int nextTrigger = 0;
            void ApplyPortraitByIndex(int portraitIndex)
            {
                var portraits = line.speaker?.characterPortraits;
                Sprite s = (portraits != null && portraitIndex >= 0 && portraitIndex < portraits.Length) ? portraits[portraitIndex] : null;
                dialoguePortrait.sprite = s;
                dialoguePortrait.color = new Color(1f, 1f, 1f, s != null ? 1f : 0f);
            }
            while (nextTrigger < triggers.Count && triggers[nextTrigger].glyphIndex <= 0)
            {
                ApplyPortraitByIndex(triggers[nextTrigger].portraitIndex);
                nextTrigger++;
            }

            // Indicator starts at first line
            dialogueNewLineIndicators.text = dialogueNewLineIndicator[1];

            float cps = (line.speed <= 0f) ? float.PositiveInfinity : line.speed;
            float delay = (cps == float.PositiveInfinity) ? 0f : 1f / cps;

            for (int visibleGlyphs = 1; visibleGlyphs <= totalGlyphs; visibleGlyphs++)
            {
                // Apply any triggers that occur BEFORE this glyph shows
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
                dialogueNewLineIndicators.text = dialogueNewLineIndicator[idx];

                // Reveal
                dialogueContents.text = visible + "<alpha=#00>" + hidden;

                // SFX
                if (!string.IsNullOrEmpty(line.speaker?.characterDialogueSound))
                    AudioManager.Instance.Play(line.speaker.characterDialogueSound);

                // Per-char delay
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
                else
                    yield return null;

                // Optional: your lineBreakPause hook (keep as you had it)
                if (line.lineBreakPause > 0f && visible.Length > 0 && visible[visible.Length - 1] == '\n')
                    yield return new WaitForSeconds(line.lineBreakPause);
            }

            // Show finished line (keep padding hidden or not, your choice)
            dialogueContents.text = renderText + paddingSuffix;

            // Wait for submit to advance
            while (!input.InteractInput)
                yield return null;
        }

        CleanupAndClose();
    }

    private void CleanupAndClose()
    {
        if (BattleManager.Instance != null && !BattleManager.Instance.active)
        {
            if (dialogueBox != null)
                dialogueBox.CloseWindow();
        }

        data.onComplete?.Invoke();

        ieDialogue = null;
        data = null;
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

    // Map a target “glyph count” (excluding TMP tags) to the raw string index boundary.
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

        bool inTag = false; // TMP rich text tag
        int glyphs = 0;     // visible glyphs (ignores TMP tags)

        for (int i = 0; i < input.Length;)
        {
            char c = input[i];

            // Pass TMP tags through verbatim, but don't count as glyphs
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