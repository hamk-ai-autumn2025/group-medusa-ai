using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Characters;

namespace dev.susybaka.TurnBasedGame.Core
{
    public static class GlobalData
    {
        /*public struct InteractionEvent
        {
            public string name;
            public Entity source;
            public Entity target;

            public InteractionEvent(string name, Entity source, Entity target)
            {
                this.name = name;
                this.source = source;
                this.target = target;
            }
        }*/

        public enum  CharacterPortrait { neutral, happy, angry, sad, confused, special }

        [System.Serializable]
        public struct DialogueString
        {
            public CharacterData speaker;
            public CharacterPortrait portrait;
            [Min(0)] public float speed;
            [Min(0)] public float lineBreakPause;
            [Multiline] public string text;

            public DialogueString(CharacterData speaker, CharacterPortrait portrait, float speed, float lineBreakPause, string text)
            {
                this.speaker = speaker;
                this.portrait = portrait;
                this.speed = speed;
                this.lineBreakPause = lineBreakPause;
                this.text = text;
            }
        }

        [System.Serializable]
        public class Flag
        {
            public enum AggregateLogic
            {
                AllTrue,   // AND: All flags must be true
                AnyTrue,   // OR: At least one flag must be true
                Threshold  // At least a certain percentage of flags must be true
            }

            public string name;
            public List<FlagValue> values = new List<FlagValue>(); // Unity-friendly serialization
            private Dictionary<string, bool> runtimeDictionary; // Backing dictionary for runtime lookups
            private bool isDirty = true; // Tracks if the dictionary needs updating

            public AggregateLogic logic = AggregateLogic.AllTrue;
            [Min(0f)] public float thresholdPercentage = 0.0f;

            public bool value => Evaluate();

            public Flag(Flag copyFrom)
            {
                name = copyFrom.name;
                logic = copyFrom.logic;
                thresholdPercentage = copyFrom.thresholdPercentage;
                values = new List<FlagValue>(copyFrom.values);
            }

            public Flag(string name, AggregateLogic logic = AggregateLogic.AllTrue, float thresholdPercentage = 0.0f)
            {
                this.name = name;
                this.logic = logic;
                this.thresholdPercentage = thresholdPercentage;
                this.values = new List<FlagValue>();
            }

            public Flag(string name, List<FlagValue> values = null, AggregateLogic logic = AggregateLogic.AllTrue, float thresholdPercentage = 0.0f)
            {
                this.name = name;
                this.logic = logic;
                this.thresholdPercentage = thresholdPercentage;

                if (values != null)
                {
                    this.values = values;
                    isDirty = true; // Mark dictionary for update
                }
                else
                {
                    this.values = new List<FlagValue>();
                }
            }

            public void SetFlag(string name, bool value)
            {
                FlagValue existing = values.Find(v => v.name == name);
                if (existing != null)
                {
                    existing.value = value;
                }
                else
                {
                    values.Add(new FlagValue(name, value));
                }

                isDirty = true; // Mark dictionary for update
            }

            public void RemoveFlag(string name)
            {
                if (values.RemoveAll(v => v.name == name) > 0)
                {
                    isDirty = true; // Mark dictionary for update
                }
            }

            public void ResetFlag()
            {
                values.RemoveAll(v => v.name != "base" && v.name != "toggleTargetable");
                isDirty = true; // Mark dictionary for update
            }

            public void ForceUpdate()
            {
                isDirty = true; // Mark dictionary for update
                UpdateRuntimeDictionary();
            }

            private void UpdateRuntimeDictionary()
            {
                if (isDirty)
                {
                    runtimeDictionary = new Dictionary<string, bool>();
                    foreach (var flagValue in values)
                    {
                        if (!runtimeDictionary.ContainsKey(flagValue.name))
                        {
                            runtimeDictionary.Add(flagValue.name, flagValue.value);
                        }
                        else
                        {
                            Debug.LogWarning($"Duplicate key '{flagValue.name}' found in Flag '{name}'. Skipping this entry.");
                        }
                    }
                    isDirty = false;
                }
            }

            public bool GetFlagValue(string key)
            {
                UpdateRuntimeDictionary();
                return runtimeDictionary.TryGetValue(key, out var value) ? value : false;
            }

            private bool Evaluate()
            {
                UpdateRuntimeDictionary();

                switch (logic)
                {
                    case AggregateLogic.AllTrue:
                        if (runtimeDictionary != null && runtimeDictionary.Count > 0)
                            return !runtimeDictionary.ContainsValue(false);
                        else
                            return false;

                    case AggregateLogic.AnyTrue:
                        if (runtimeDictionary != null && runtimeDictionary.Count > 0)
                            return runtimeDictionary.ContainsValue(true);
                        else
                            return false;

                    case AggregateLogic.Threshold:
                        if (runtimeDictionary == null || runtimeDictionary.Count == 0)
                            return false;
                        int trueCount = runtimeDictionary.Values.Count(v => v);
                        float percentage = (float)trueCount / runtimeDictionary.Count * 100;
                        return percentage >= thresholdPercentage;

                    default:
                        throw new InvalidOperationException("Unsupported logic type.");
                }
            }

            [System.Serializable]
            public class FlagValue
            {
                public string name;
                public bool value;

                public FlagValue(string name, bool value)
                {
                    this.name = name;
                    this.value = value;
                }
            }
        }

        [System.Serializable]
        public struct Axis
        {
            public bool x;
            public bool y;
            public bool z;

            public Axis(bool x, bool y, bool z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public bool All()
            {
                return x && y && z;
            }

            public bool Any()
            {
                return x || y || z;
            }

            public bool None()
            {
                return !x && !y && !z;
            }
        }
    }
}