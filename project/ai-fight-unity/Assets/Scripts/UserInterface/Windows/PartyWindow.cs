using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using dev.susybaka.TurnBasedGame.Battle;
using dev.susybaka.TurnBasedGame.Characters;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class PartyWindow : CommandWindow
    {
        public List<PartyMemberEntry> members = new List<PartyMemberEntry>();
        
        private bool planningMode = false;
        private List<Character> pool = new List<Character>();
        private ActionWindow actionWindow;
        private HashSet<Character> disabled = new HashSet<Character>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (members == null)
                return;

            for (int i = 0; i < members.Count; i++)
            {
                PartyMemberEntry pm = members[i];
                if (pm.root != null)
                {
                    pm.name = pm.root.name;
                    if (pm.nameLabel == null)
                    {
                        foreach (Transform c in pm.root.transform)
                        {
                            c.TryGetComponent(out pm.nameLabel);
                        }
                    }
                    if (pm.healthBar == null)
                    {
                        pm.healthBar = pm.root.GetComponentInChildren<Slider>(true);
                    }
                    if (pm.healthLabel == null)
                    {
                        int cc = pm.healthBar?.transform.childCount ?? 1;
                        pm.healthLabel = pm.healthBar?.transform.GetChild(cc - 1).GetComponent<TextMeshProUGUI>();
                    }
                }
                members[i] = pm;
            }
        }
#endif

        public void SetActionWindow(ActionWindow window)
        {
            actionWindow = window;
        }     

        protected override void SelectLine(int index)
        {
            if (members == null)
                return;

            RefreshUI(index);
        }

        protected override void Back()
        {
            // Do nothing since this is a top-level window during battles
        }

        public void OpenForPlanning(Party party)
        {
            planningMode = true;
            OpenFor(party);
            ClearOrdersAndReenable();
        }

        public void OpenForRealtime(Party party)
        {
            planningMode = false;
            OpenFor(party);
            ClearOrdersAndReenable();
        }

        private void OpenFor(Party party)
        {
            pool = party.members;//BuildPool(this.targetGroup); // create from BattleHandler state

            // Reuse your existing UI by listing names:
            commands.Clear();
            for (int i = 0; i < pool.Count; i++)
            {
                var ch = pool[i];
                if (ch == null)
                {
                    Debug.LogError($"PartyWindow: Null character at index {i}");
                    continue;
                }

                Command cmd = new Command(ch.data.characterName, $"Select {ch.data.characterName}", true, string.Empty, () => OpenActionsFor(ch));
                commands.Add(cmd);
            }

            UpdateCommands(commands.ToArray());
        }

        public void MarkActorOrder(Character actor, int order)
        {
            // Update UI label to "1. Name" etc.
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] == actor)
                {
                    if (i < members.Count)
                    {
                        PartyMemberEntry pm = members[i];

                        pm.name = $"{order}. {actor.data.characterName}";
                        pm.nameLabel.text = pm.name;

                        members[i] = pm;
                    }
                    break;
                }
            }
            // Store for ClearOrders...
        }

        public void DisableActor(Character actor)
        {
            disabled.Add(actor);
            nav.AddSkipped(pool.IndexOf(actor));
            // Gray out / disable its command entry
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] == actor)
                {
                    if (i < members.Count)
                    {
                        PartyMemberEntry pm = members[i];

                        pm.name = $"<color=#2F2F2F>{pm.name}</color>";
                        pm.nameLabel.text = pm.name;

                        members[i] = pm;
                    }
                    break;
                }
            }
        }

        public void ClearOrdersAndReenable()
        {
            disabled.Clear();
            nav.ClearSkipped();
            // Remove badges, re-enable entries for next round
            if (members == null || members.Count < 1)
                return;

            for (int i = 0; i < members.Count; i++)
            {
                if (i < pool.Count && pool[i] != null)
                {
                    PartyMemberEntry pm = members[i];

                    pm.name = pool[i].data.characterName;
                    pm.nameLabel.text = pm.name;

                    members[i] = pm;
                }
                else
                {
                    members[i].nameLabel.text = "<color=#2F2F2F>Unknown</color>";
                }
            }
        }

        public void ClearOrders()
        {
            // Remove badges
            if (members == null || members.Count < 1)
                return;

            for (int i = 0; i < members.Count; i++)
            {
                if (i < pool.Count && pool[i] != null)
                {
                    PartyMemberEntry pm = members[i];

                    pm.name = pool[i].data.characterName;
                    pm.nameLabel.text = pm.name;

                    members[i] = pm;
                }
                else
                {
                    members[i].nameLabel.text = "<color=#2F2F2F>Unknown</color>";
                }
            }
        }

        public void RefreshUI(int selected = -1)
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (i < pool.Count && pool[i] != null)
                {
                    /*if (disabled != null && disabled.Count > 0 && disabled.Contains(pool[i]))
                    {
                        if (i >= commands.Count - 1)
                            SelectLine(0);
                        else
                            SelectLine(i + 1);
                        return;
                    }*/

                    var name = members[i].name;
                    if (selected > -1)
                        members[i].nameLabel.text = (i == selected) ? $"<color=yellow>* {name}</color>" : name;
                    else
                        members[i].nameLabel.text = name;
                    members[i].healthBar.value = pool[i].health;
                    members[i].healthBar.maxValue = pool[i].maxHealth;
                    members[i].healthLabel.text = pool[i].isAlive ? $"{pool[i].health} / {pool[i].maxHealth}" : $"<color=red>{pool[i].health} / {pool[i].maxHealth}</color>";
                    members[i].root.SetActive(true);
                }
                else
                {
                    members[i].nameLabel.text = "<color=#2F2F2F>Unknown</color>";
                    members[i].healthBar.value = 1;
                    members[i].healthBar.maxValue = 1;
                    members[i].healthLabel.text = "<color=#2F2F2F>0 / 0</color>";
                    members[i].root.SetActive(false);
                }
            }
        }

        private void OpenActionsFor(Character ch)
        {
            if (actionWindow == null)
            {
                Debug.LogError("PartyWindow: ActionWindow reference missing.");
                return;
            }

            if (isActive == false)
            {
                Debug.LogWarning("PartyWindow: Cannot open actions, window is not active.");
                return;
            }

            if (!ch.isAlive)
                return;

            if (planningMode)
            {
                if (disabled.Contains(ch))
                    return;

                actionWindow.OpenForNodesPlanning(ch, ch.data.rootCommands.children);
            }
            else
            {
                actionWindow.OpenForNodesRealtime(ch, ch.data.rootCommands.children);
            }

            navHandler?.PushWindow(actionWindow);
        }

        /*private List<Character> BuildPool(TargetGroup g)
        {
            var bh = GameManager.Instance.BattleHandler;

            // Example stub:
            switch (g)
            {
                case TargetGroup.self:
                    return new List<Character> { bh.playerCharacter };
                case TargetGroup.ally:
                    return new List<Character>(bh.allies.members);
                case TargetGroup.allies:
                    return new List<Character>(bh.allies.members);
                case TargetGroup.enemy:
                    return new List<Character>(bh.enemies.members);
                case TargetGroup.enemies:
                    return new List<Character>(bh.enemies.members);
                default:
                    return new List<Character>();
            }
        }*/

        [System.Serializable]
        public struct PartyMemberEntry
        {
            public string name;
            public GameObject root;
            public TextMeshProUGUI nameLabel;
            public TextMeshProUGUI healthLabel;
            public Slider healthBar;
        }
    }
}