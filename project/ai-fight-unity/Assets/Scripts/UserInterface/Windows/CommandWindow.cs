using System.Collections.Generic;
using dev.susybaka.Shared.UI;
using dev.susybaka.TurnBasedGame.Battle;
using dev.susybaka.TurnBasedGame.Interfaces;
using UnityEngine;
using static dev.susybaka.TurnBasedGame.UI.ActionWindow;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class CommandWindow : HudWindow, IHudNavigable
    {
        protected readonly ListNavigator nav = new ListNavigator();

        protected LabelWindow descriptionWindow;
        protected TurnSystem turnSystem;
        protected HudNavigationHandler navHandler;
        protected ScrollBox scrollBox;
        protected List<Command> commands = new List<Command>();

#if UNITY_EDITOR
        private int currentIndex = 0;
#endif

        public override void Initialize(GameManager manager)
        {
            if (initialized)
                return;

            base.Initialize(manager);
            scrollBox = GetComponentInChildren<ScrollBox>();
            navHandler = manager.HudNavigationHandler;
            turnSystem = manager.BattleHandler?.TurnSystem;
            descriptionWindow = manager.BattleHandler?.battleWindow?.DescriptionWindow;
        }

        public override void OpenWindow()
        {
            base.OpenWindow();

            nav.SetCount(commands.Count);
            nav.OnIndexChanged += OnIndexChanged;
            OnIndexChanged(nav.Index);
        }

        public override void CloseWindow()
        {
            base.CloseWindow();
            nav.Reset();
            nav.OnIndexChanged -= OnIndexChanged;
            OnIndexChanged(nav.Index);
        }

        public void UpdateCommands(Command[] cmds)
        {
            commands.Clear();
            commands.AddRange(cmds);

            /*if (commands == null || commands.Count <= 0)
            {
                Debug.LogWarning("CommandWindow: No commands to display.");
                return;
            }
            else
            {
                Debug.Log($"CommandWindow: Loaded {commands.Count} commands.");
            }*/

            if (scrollBox != null)
            {
                scrollBox.lines.Clear();
                scrollBox.lines.Capacity = commands.Count;
                foreach (var cmd in commands)
                    scrollBox.lines.Add(cmd.name);
                scrollBox.SelectLine(0);
                scrollBox.ForceRefresh();
            }
            UpdateDescription(0);
        }

        public void OnFocus(HudNavigationHandler handler)
        {
            isActive = true;

            if (scrollBox != null)
                scrollBox.SelectLine(nav.Index);

            UpdateDescription(nav.Index);
        }

        public void OnBlur() { isActive = false; }

        public void OnNavCommand(HudNavCommand cmd)
        {
            if (!isActive)
                return;

            switch (cmd)
            {
                case HudNavCommand.Next:
                    nav.Next();
                    break;
                case HudNavCommand.Previous:
                    nav.Prev();
                    break;
                case HudNavCommand.Submit:
                    Execute(commands[nav.Index]);
                    break;
                case HudNavCommand.Back:
                    Back();
                    break;
            }
        }

        private void OnIndexChanged(int index)
        {
            if (!isActive)
                return;

#if UNITY_EDITOR
            currentIndex = index;
#endif
            SelectLine(index);
        }

        private void Execute(Command command)
        {
            if (!isActive)
                return;

            command.action?.Invoke();
            UpdateDescription(nav.Index);
        }

        protected virtual void SelectLine(int index)
        {
            if (scrollBox != null)
                scrollBox.SelectLine(index);
            UpdateDescription(index);
        }

        protected virtual void Back()
        {
            navHandler?.PopWindow();
        }

        protected void UpdateDescription(int index)
        {
            if (!isOpen || (isOpen && !isActive))
                return;

            if (commands != null && commands.Count > 0)
            {
                descriptionWindow.SetText(commands[index].description);
            }
        }
    }
}