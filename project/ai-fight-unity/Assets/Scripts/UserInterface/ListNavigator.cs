using System;
using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.UI
{
    [Serializable]
    public sealed class ListNavigator
    {
        public int Index { get; private set; } = 0;
        public int Count { get; private set; } = 0;
        public bool Loop { get; set; } = true;
        public HashSet<int> SkippedIndicies { get; private set; } = new HashSet<int>();

        public event Action<int> OnIndexChanged;

        public void SetCount(int count, bool clampIndex = true)
        {
            Count = Mathf.Max(0, count);
            if (clampIndex)
                Index = Mathf.Clamp(Index, 0, Mathf.Max(Count - 1, 0));
            OnIndexChanged?.Invoke(Index);
        }

        public void SetSkipped(IEnumerable<int> indicies)
        {
            SkippedIndicies = new HashSet<int>(indicies);
        }

        public void ClearSkipped()
        {
            SkippedIndicies.Clear();
        }

        public void AddSkipped(int index)
        {
            if (SkippedIndicies.Contains(index)) 
                return;

            SkippedIndicies.Add(index);
        }

        public void RemoveSkipped(int index)
        {
            if (!SkippedIndicies.Contains(index))
                return;

            SkippedIndicies.Remove(index);
        }

        public void Reset()
        {
            Index = 0;
            OnIndexChanged?.Invoke(Index);
        }

        public void Next()
        {
            if (Count == 0)
                return;

            int startIndex = Index;
            int attempts = 0;
            do
            {
                Index = (Index + 1 >= Count) ? (Loop ? 0 : Count - 1) : Index + 1;
                attempts++;

                // If not looping and we've reached the end, break
                if (!Loop && Index == Count - 1 && SkippedIndicies.Contains(Index))
                    break;

                // If we've looped all possible indices, break
                if (attempts > Count)
                    break;

                // If current index is not skipped, we're done
                if (!SkippedIndicies.Contains(Index))
                    break;

                // If not looping and next index is skipped, and all remaining are skipped, stay at current
                if (!Loop && Index == Count - 1)
                {
                    bool allSkipped = true;
                    for (int i = Index; i < Count; i++)
                    {
                        if (!SkippedIndicies.Contains(i))
                        {
                            allSkipped = false;
                            break;
                        }
                    }
                    if (allSkipped)
                    {
                        Index = startIndex;
                        break;
                    }
                }
            }
            while (SkippedIndicies.Contains(Index) && attempts <= Count);

            OnIndexChanged?.Invoke(Index);
        }

        public void Prev()
        {
            if (Count == 0)
                return;

            int startIndex = Index;
            int attempts = 0;
            do
            {
                Index = (Index - 1 < 0) ? (Loop ? Count - 1 : 0) : Index - 1;
                attempts++;

                // If not looping and we've reached the start, break
                if (!Loop && Index == 0 && SkippedIndicies.Contains(Index))
                    break;

                // If we've looped all possible indices, break
                if (attempts > Count)
                    break;

                // If current index is not skipped, we're done
                if (!SkippedIndicies.Contains(Index))
                    break;

                // If not looping and prev index is skipped, and all remaining are skipped, stay at current
                if (!Loop && Index == 0)
                {
                    bool allSkipped = true;
                    for (int i = Index; i >= 0; i--)
                    {
                        if (!SkippedIndicies.Contains(i))
                        {
                            allSkipped = false;
                            break;
                        }
                    }
                    if (allSkipped)
                    {
                        Index = startIndex;
                        break;
                    }
                }
            }
            while (SkippedIndicies.Contains(Index) && attempts <= Count);

            OnIndexChanged?.Invoke(Index);
        }
    }
}