using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Characters
{
    [System.Serializable]
    public class Party
    {
        private GameManager gameManager;

        public string name;
        public int maxMembers;
        public List<Character> members;
        [SerializeField] private int points = 0;
        public int Points => points;
        [SerializeField] private int maxPoints = 100;
        public int MaxPoints => maxPoints;

        public Party(string name, int maxMembers)
        {
            this.name = name;
            this.maxMembers = maxMembers;
            this.members = new List<Character>();
            this.points = 0;
        }

        public Party(string name, int maxMembers, List<Character> members)
        {
            this.name = name;
            this.maxMembers = maxMembers;
            this.members = members ?? new List<Character>();
            this.points = 0;
        }

        public void Initialize(GameManager manager)
        {
            points = 0;
            gameManager = manager;

            if (members == null)
            {
                members = new List<Character>();
            }
            else
            {
                for (int i = 0; i < members.Count; i++)
                {
                    if (members[i] != null)
                        members[i].Initialize(this);
                }
            }
        }

        public bool AddMember(Character character)
        {
            if (members.Count >= maxMembers || members.Contains(character))
                return false;

            members.Add(character);
            return true;
        }

        public bool RemoveMember(Character character)
        {
            if (members.Contains(character))
            {
                members.Remove(character);
                return true;
            }
            return false;
        }

        public Character GetFirstAliveMember() 
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].isAlive)
                {
                    return members[i];
                }
            }

            return null;
        }

        public List<Character> GetAllAliveMembers()
        {
            List<Character> alive = new List<Character>();
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].isAlive)
                {
                    alive.Add(members[i]);
                }
            }
            return alive;
        }

        public Character GetFirstWoundedOrAliveMember()
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].isAlive && members[i].health < members[i].maxHealth)
                {
                    return members[i];
                }
            }
            return GetFirstAliveMember();
        }

        public void ModifyPoints(int amount)
        {
            points += amount;

            if (points < 0) 
                points = 0;
            
            if (points > maxPoints) 
                points = maxPoints;
        }
    }

    [System.Serializable]
    public class Stat
    {
        public string name;
        [SerializeField] private int baseValue;
        [SerializeField] private int currentValue;
        public int Value => currentValue;
        [SerializeField] private int maxValue;
        public int MaxValue => maxValue;
        public bool preventNegative = true;

        private HashSet<Modifier> modifiers = new HashSet<Modifier>();

        public Stat(string name, int baseValue, int maxValue)
        {
            this.name = name;
            this.baseValue = baseValue;
            this.currentValue = baseValue;
            this.maxValue = maxValue;
        }
        
        public void Add(string name, int amount)
        {
            Modifier mod = new Modifier(name, amount);
            if (modifiers.Contains(mod))
                return;
            modifiers.Add(mod);
            Recalculate();
        }

        public void Remove(string name)
        {
            foreach (Modifier mod in modifiers)
            {
                if (mod.name == name)
                {
                    modifiers.Remove(mod);
                    Recalculate();
                    return;
                }
            }
        }

        public void Reset()
        {
            currentValue = baseValue;
            modifiers.Clear();
            Recalculate();
        }

        private void Recalculate()
        {
            int totalModifier = 0;
            foreach (Modifier mod in modifiers)
            {
                totalModifier += mod.amount;
            }
            currentValue = baseValue + totalModifier;
            if (preventNegative && currentValue < 0)
                currentValue = 0;
            if (currentValue > maxValue)
                currentValue = maxValue;
        }

        public readonly struct Modifier
        {
            public string name { get; init; }
            public int amount { get; init; }

            public Modifier(string name, int amount)
            {
                this.name = name;
                this.amount = amount;
            }
        }
    }

    [System.Serializable]
    public struct KnowledgeEntry
    {
        public string name;
        public string text;

        public KnowledgeEntry(string name, string text)
        {
            this.name = name;
            this.text = text;
        }
    }
}