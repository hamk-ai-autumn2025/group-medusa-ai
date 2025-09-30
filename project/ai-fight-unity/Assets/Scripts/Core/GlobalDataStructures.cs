using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Globals
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

    public enum FlagAggregateLogic
    {
        AllTrue,   // AND: All flags must be true
        AnyTrue,   // OR: At least one flag must be true
        Threshold  // At least a certain percentage of flags must be true
    }

    [System.Serializable]
    public class Flag
    {
        public string name;
        public List<Value> values = new List<Value>(); // Unity-friendly serialization
        private Dictionary<string, bool> runtimeDictionary; // Backing dictionary for runtime lookups
        private bool isDirty = true; // Tracks if the dictionary needs updating

        public FlagAggregateLogic logic = FlagAggregateLogic.AllTrue;
        [Min(0f)] public float thresholdPercentage = 0.0f;

        public bool value => Evaluate();

        public Flag(Flag copyFrom)
        {
            name = copyFrom.name;
            logic = copyFrom.logic;
            thresholdPercentage = copyFrom.thresholdPercentage;
            values = new List<Value>(copyFrom.values);
        }

        public Flag(string name, FlagAggregateLogic logic = FlagAggregateLogic.AllTrue, float thresholdPercentage = 0.0f)
        {
            this.name = name;
            this.logic = logic;
            this.thresholdPercentage = thresholdPercentage;
            this.values = new List<Value>();
        }

        public Flag(string name, List<Value> values = null, FlagAggregateLogic logic = FlagAggregateLogic.AllTrue, float thresholdPercentage = 0.0f)
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
                this.values = new List<Value>();
            }
        }

        public void SetFlag(string name, bool value)
        {
            Value existing = values.Find(v => v.name == name);
            if (existing != null)
            {
                existing.value = value;
            }
            else
            {
                values.Add(new Value(name, value));
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
                case FlagAggregateLogic.AllTrue:
                    if (runtimeDictionary != null && runtimeDictionary.Count > 0)
                        return !runtimeDictionary.ContainsValue(false);
                    else
                        return false;

                case FlagAggregateLogic.AnyTrue:
                    if (runtimeDictionary != null && runtimeDictionary.Count > 0)
                        return runtimeDictionary.ContainsValue(true);
                    else
                        return false;

                case FlagAggregateLogic.Threshold:
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
        public class Value
        {
            public string name;
            public bool value;

            public Value(string name, bool value)
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