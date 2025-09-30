using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class ScrollBox : MonoBehaviour
    {
        [SerializeField, Min(1)] private int showLines = 3;
        public List<string> lines = new List<string>();

        [SerializeField] private TextMeshProUGUI lineContent;
        [SerializeField] private TextMeshProUGUI lineSelector;

        private const string paddingChar = "\u00A0"; // non-breaking space so TMP keeps the line

        // Zero-based index of the selected line in the full list
        private int currentLine = 0;

        // First visible index of the sliding window
        private int firstVisible = 0;

        private void Awake() => Refresh();
        private void OnEnable() => Refresh();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (showLines < 1)
                showLines = 1;
            Refresh();
        }

        [NaughtyAttributes.Button("Next")]
        public void SelectNextEditor()
        {
            SelectLine(currentLine + 1);
        }

        [NaughtyAttributes.Button("Previous")]
        public void SelectPreviousEditor()
        {
            SelectLine(currentLine - 1);
        }
#endif

        /// <summary>
        /// Selects an absolute line index (0-based) and scrolls the window if needed.
        /// </summary>
        public void SelectLine(int line)
        {
            if (lines == null || lines.Count == 0)
            {
                currentLine = 0;
                firstVisible = 0;
                Refresh();
                return;
            }

            currentLine = Mathf.Clamp(line, 0, lines.Count - 1);

            // Ensure current is inside the visible window
            if (currentLine < firstVisible)
            {
                firstVisible = currentLine;
            }
            else if (currentLine > firstVisible + showLines - 1)
            {
                firstVisible = currentLine - showLines + 1;
            }

            // Clamp start so we don't scroll past the end when list is short or near the end
            int maxStart = Mathf.Max(0, lines.Count - showLines);
            firstVisible = Mathf.Clamp(firstVisible, 0, maxStart);

            Refresh();
        }

        /// <summary>
        /// Convenience: move selection by delta (e.g., -1 for up, +1 for down). No wrap.
        /// </summary>
        public void MoveSelection(int delta) => SelectLine(currentLine + delta);

        /// <summary>
        /// Call after you externally modify 'lines' or change 'showLines'.
        /// Keeps the selection valid and refreshes the UI.
        /// </summary>
        public void ForceRefresh()
        {
            if (lines == null)
                lines = new List<string>();
            currentLine = Mathf.Clamp(currentLine, 0, Mathf.Max(0, lines.Count - 1));
            int maxStart = Mathf.Max(0, lines.Count - showLines);
            firstVisible = Mathf.Clamp(firstVisible, 0, maxStart);
            Refresh();
        }

        private void Refresh()
        {
            // CONTENT: exactly 'showLines' rows (empty if beyond list)
            if (lineContent != null)
            {
                var sb = new System.Text.StringBuilder();
                int count = lines?.Count ?? 0;
                bool hasSelection = (lines != null && lines.Count > 0);
                int selectedVisibleIndex = hasSelection
                    ? Mathf.Clamp(currentLine - firstVisible, 0, showLines - 1)
                    : -1; // no star when list is empty

                for (int i = 0; i < showLines; i++)
                {
                    int idx = firstVisible + i;
                    string row =
                        (idx >= 0 && idx < count)
                            ? (string.IsNullOrEmpty(lines[idx]) ? paddingChar : lines[idx])
                            : paddingChar;

                    if (i == selectedVisibleIndex)
                        sb.Append("<color=yellow>");
                    
                    sb.Append(row);

                    if (i == selectedVisibleIndex)
                        sb.Append("</color>");

                    if (i < showLines - 1)
                        sb.Append('\n');
                }

                lineContent.text = sb.ToString();
            }

            // SELECTOR: "<br>" * showLines with a "*" before the currently visible selected row
            if (lineSelector != null)
            {
                var sbSel = new StringBuilder();
                bool hasSelection = (lines != null && lines.Count > 0);
                int selectedVisibleIndex = hasSelection
                    ? Mathf.Clamp(currentLine - firstVisible, 0, showLines - 1)
                    : -1; // no star when list is empty

                for (int i = 0; i < showLines; i++)
                {
                    if (i == selectedVisibleIndex)
                    {
                        sbSel.Append("<color=yellow>");
                        sbSel.Append('*');
                        sbSel.Append("</color>");
                    }
                    sbSel.Append("<br>");
                }
                lineSelector.text = sbSel.ToString();
            }
        }
    }
}