using System.Collections.Generic;

namespace dev.susybaka.TurnBasedGame.Characters
{
    [System.Serializable]
    public struct Party
    {
        public string name;
        public int maxMembers;
        public List<Character> members;

        public Party(string name, int maxMembers)
        {
            this.name = name;
            this.maxMembers = maxMembers;
            this.members = new List<Character>();
        }

        public Party(string name, int maxMembers, List<Character> members)
        {
            this.name = name;
            this.maxMembers = maxMembers;
            this.members = members ?? new List<Character>();
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
    }
}