using System.Collections;
using System.Collections.Generic;
using dev.susybaka.Shared.UI;
using dev.susybaka.TurnBasedGame.Battle;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Interfaces;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class ActionWindow : HudWindow, IHudNavigable
    {
        public enum MenuMode { Nodes, Abilities }

        [SerializeField] private ActionWindow subWindow;

        private readonly ListNavigator nav = new ListNavigator();
        private readonly List<CommandNodeData> nodes = new List<CommandNodeData>();
        private readonly List<AbilityData> abilities = new List<AbilityData>();

#if UNITY_EDITOR
        private List<CommandNodeData> _nodes = new();
        private List<AbilityData> _abilities = new();
#endif

        private MenuMode mode;
        private TargetWindow targetWindow;
        private HudNavigationHandler navHandler;
        private AbilitySystem abilityRunner;
        private ScrollBox scrollBox;
        private Character actor;
        private NodeProviderData currentNodeProvider;
        private TurnSystem turnSystem;
        private bool planningMode;

        public override void Initialize(GameManager manager)
        {
            if (initialized)
                return;

            base.Initialize(manager);
            scrollBox = GetComponentInChildren<ScrollBox>();
            navHandler = manager.HudNavigationHandler;
            abilityRunner = manager.BattleHandler?.AbilitySystem;
            turnSystem = manager.BattleHandler?.TurnSystem;

            if (subWindow != null)
            {
                subWindow.Initialize(manager);
                subWindow.CloseWindow();
            }

            if (scrollBox != null)
            {
                scrollBox.lines.Clear();
            }
        }

        public void SetTargetWindow(TargetWindow window)
        {
            targetWindow = window;
            if (subWindow != null)
                subWindow.SetTargetWindow(window);
        }

        public override void CloseWindow()
        {
            base.CloseWindow();
            nav.Reset();
            SelectLine();
        }

        public void OpenForNodesPlanning(Character actor, IEnumerable<CommandNodeData> nodes)
        {
            planningMode = true;
            this.actor = actor;
            OpenForNodes(actor, nodes); // your existing populate
        }

        public void OpenForAbilitiesPlanning(Character actor, IEnumerable<AbilityData> abilities, NodeProviderData nodeProvider = null)
        {
            planningMode = true;
            this.actor = actor;
            OpenForAbilities(actor, abilities, nodeProvider); // your existing populate
        }

        public void OpenForNodesRealtime(Character actor, IEnumerable<CommandNodeData> nodes)
        {
            planningMode = false;
            this.actor = actor;
            OpenForNodes(actor, nodes); // your existing populate
        }

        public void OpenForAbilitiesRealtime(Character actor, IEnumerable<AbilityData> abilities, NodeProviderData nodeProvider = null)
        {
            planningMode = false;
            this.actor = actor;
            OpenForAbilities(actor, abilities, nodeProvider); // your existing populate
        }

        private void OpenForNodes(Character actor, IEnumerable<CommandNodeData> src)
        {
            this.actor = actor;
            mode = MenuMode.Nodes;
            nodes.Clear();
            nodes.AddRange(src);

#if UNITY_EDITOR
            _nodes = new List<CommandNodeData>(nodes);
#endif

            RebuildUI();
        }

        private void OpenForAbilities(Character actor, IEnumerable<AbilityData> src, NodeProviderData nodeProvider = null)
        {
            this.actor = actor;
            mode = MenuMode.Abilities;
            abilities.Clear();
            abilities.AddRange(src);
            currentNodeProvider = nodeProvider;

#if UNITY_EDITOR
            _abilities = new List<AbilityData>(abilities);
#endif

            RebuildUI();
        }

        private void RebuildUI()
        {
            scrollBox.lines.Clear();

            if (mode == MenuMode.Abilities)
            {
                foreach (AbilityData abilityData in abilities)
                {
                    scrollBox.lines.Add(currentNodeProvider != null ? currentNodeProvider.GetLabel(actor, abilityData) : abilityData.displayName);
                }
            }
            else
            {
                foreach (CommandNodeData cmdNodeData in nodes)
                {
                    scrollBox.lines.Add(cmdNodeData.displayName);
                }
            }

            nav.SetCount(scrollBox.lines.Count);
            scrollBox.SelectLine(0);
            scrollBox.ForceRefresh();
        }

        public void OnFocus(HudNavigationHandler _) { if (scrollBox) scrollBox.SelectLine(nav.Index); isActive = true; }
        public void OnBlur() { isActive = false; }

        public void OnNavCommand(HudNavCommand cmd)
        {
            switch (cmd)
            {
                case HudNavCommand.Next:
                    nav.Next();
                    SelectLine();
                    break;
                case HudNavCommand.Previous:
                    nav.Prev();
                    SelectLine();
                    break;
                case HudNavCommand.Submit:
                    ActivateCurrent();
                    break;
                case HudNavCommand.Back:
                    navHandler.PopWindow();
                    break;
            }
        }

        private void SelectLine() { if (scrollBox) scrollBox.SelectLine(nav.Index); }

        private void ActivateCurrent()
        {
            if (mode == MenuMode.Nodes)
                ActivateNode(nodes[nav.Index]);
            else
                ActivateAbility(abilities[nav.Index]);
        }

        private void ActivateNode(CommandNodeData node)
        {
            if (node.type == NodeType.abilityLeaf)
            {
                ActivateAbility(node.ability);
                return;
            }

            if (node.type == NodeType.groupStatic)
            {
                if (!subWindow)
                { Debug.LogWarning("No subwindow assigned!"); return; }
                subWindow.OpenForNodes(actor, node.children);
                navHandler.PushWindow(subWindow);
                return;
            }

            if (node.type == NodeType.groupDynamic)
            {
                if (!subWindow)
                { Debug.LogWarning("No subwindow assigned!"); return; }
                var built = node.provider ? node.provider.BuildAbilities(actor) : null;
                // guard empty
                IEnumerable<AbilityData> list = built == null ? System.Array.Empty<AbilityData>() : new List<AbilityData>(built);
                if (list.IsEmpty())
                { /* show "none available" or beep */ return; }
                
                if (planningMode)
                    subWindow.OpenForAbilitiesPlanning(actor, built, node.provider);
                else
                    subWindow.OpenForAbilitiesRealtime(actor, built, node.provider);
                navHandler.PushWindow(subWindow);
            }
        }

        private void ActivateAbility(AbilityData ability)
        {
            if (ability == null)
                return;

            if (ability.requiresTarget)
            {
                targetWindow.OpenFor(ability.targetGroup, selection =>
                {
                    if (planningMode)
                    {
                        StartCoroutine(IE_CommitThenReturn(ability, actor, selection));
                    }
                    else
                    {
                        StartCoroutine(IE_ExecuteThenReturn(ability, actor, selection));
                    }
                });
                navHandler.PushWindow(targetWindow, false);
            }
            else
            {
                if (planningMode)
                {
                    StartCoroutine(IE_CommitThenReturn(ability, actor, System.Array.Empty<Character>()));
                }
                else
                {
                    StartCoroutine(IE_ExecuteThenReturn(ability, actor, System.Array.Empty<Character>()));
                }
            }
        }

        private IEnumerator IE_ExecuteThenReturn(AbilityData ability, Character actor, IList<Character> targets)
        {
            yield return abilityRunner.Run(ability, actor, targets);
            navHandler?.ReturnToRoot();
        }

        private IEnumerator IE_CommitThenReturn(AbilityData ability, Character actor, IList<Character> targets)
        {
            turnSystem.CommitIntent(actor, ability, targets);
            yield return null;
            navHandler?.ReturnToRoot();
        }
    }
}