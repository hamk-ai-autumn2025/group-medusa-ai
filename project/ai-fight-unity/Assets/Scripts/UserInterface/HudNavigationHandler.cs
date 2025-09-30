using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.Shared.UI;
using dev.susybaka.TurnBasedGame.Globals;
using dev.susybaka.TurnBasedGame.Input;
using dev.susybaka.TurnBasedGame.Interfaces;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class HudNavigationHandler : MonoBehaviour
    {
        private GameManager gameManager;
        private InputHandler input;

        public HudWindow Root { get; private set; }
        public HudWindow Current => windowStack.Count > 0 ? windowStack.Peek() : Root;
        public Flag hasHudWindowOpen = new Flag("hasHudWindowOpen", FlagAggregateLogic.AllTrue);

        private readonly Stack<HudWindow> windowStack = new Stack<HudWindow>();

#if UNITY_EDITOR
        private List<HudWindow> _windowStack = new List<HudWindow>();
#endif

        private bool busy = false;
        private bool initialized = false;

        private void Update()
        {
            if (!initialized)
                return;

            if (input.BackInput)
            {
                // At child level: let current handle back, else pop
                if (windowStack.Count > 0)
                {
                    if (Current is IHudNavigable nav)
                        nav.OnNavCommand(HudNavCommand.Back);
                    else
                        PopWindow();
                }
                else
                {
                    // At root: optionally forward Back to it, otherwise ignore
                    if (Root is IHudNavigable rnav)
                        rnav.OnNavCommand(HudNavCommand.Back);
                }
            }

            // Route simple 2-button list nav (+ optional Submit)
            if (Current is IHudNavigable n)
            {
                // input.PreviousInput and input.NextInput are reversed because they map to vertical axis and scrollable lists starts at top and go down
                if (input.NextInput)
                    n.OnNavCommand(HudNavCommand.Next);
                if (input.PreviousInput)
                    n.OnNavCommand(HudNavCommand.Previous);
                if (input.ConfirmInput)
                    n.OnNavCommand(HudNavCommand.Submit);
            }
        }

        public void Initialize(GameManager manager)
        {
            if (initialized)
                return;
            initialized = true;
            gameManager = manager;
            input = gameManager.Input;
            hasHudWindowOpen.SetFlag("base", false);
            UpdateState();
        }

        #region Root
        public void OpenRoot(HudWindow root, bool openNow = true, bool clearChildren = true)
        {
            if (busy)
                return;
            busy = true;

            if (clearChildren)
                CloseChildren();

            if (Root != null && Root != root)
            {
                if (Root is IHudNavigable oldNav)
                    oldNav.OnBlur();
                Root.CloseWindow();
            }

            Root = root;
            if (Root != null && openNow)
            {
                Root.OpenWindow();
                if (Root is IHudNavigable rnav)
                    rnav.OnFocus(this);
            }

            UpdateState();
            busy = false;
        }

        public void ReturnToRoot()
        {
            if (busy)
                return;
            busy = true;

            CloseChildren();

            if (Root != null)
            {
                // Ensure visible and focused
                if (!Root.isOpen)
                    Root.OpenWindow();
                if (Root is IHudNavigable rnav)
                    rnav.OnFocus(this);
            }

            UpdateState();
            busy = false;
        }

        public void CloseRoot()
        {
            ReturnToRoot();
            if (Root != null)
            {
                if (Root is IHudNavigable rnav)
                    rnav.OnBlur();
                Root.CloseWindow();
                Root = null;
            }
            UpdateState();
        }
        public void PushWindow(HudWindow window, bool hidePreviousNonRoot = true)
        {
            if (busy || window == null)
                return;
            busy = true;

            var prev = Current;

            // If trying to open the root again, or the window is already open and active, just ensure it's open and active
            if (window == Root || (window.isOpen && window.isActive))
            {
                window.OpenWindow();
                if (window is IHudNavigable nav)
                    nav.OnFocus(this);
                UpdateState();
                busy = false;
                return;
            }

            // Blur previous always
            if (prev is IHudNavigable prevNav)
                prevNav.OnBlur();

            // Hide previous if it isn't the root (root stays open)
            if (prev != null && hidePreviousNonRoot && prev != Root)
                prev.CloseWindow();

            windowStack.Push(window);
            window.OpenWindow();
            if (window is IHudNavigable nav2)
                nav2.OnFocus(this);

            UpdateState();
            busy = false;
        }

        public void PopWindow()
        {
            if (busy)
                return;
            if (windowStack.Count == 0)
            { return; } // already at root
            busy = true;

            var top = windowStack.Pop();
            if (top is IHudNavigable tnav)
                tnav.OnBlur();
            top.CloseWindow();

            var now = Current;
            if (now != null)
            {
                if (!now.isOpen)
                    now.OpenWindow();
                if (now is IHudNavigable nnav)
                    nnav.OnFocus(this);
            }

            UpdateState();
            busy = false;
        }

        public void CloseWindow(HudWindow window)
        {
            if (busy || window == null || windowStack.Count == 0)
                return;

            busy = true;
            // If trying to close the root, do that instead
            if (window == Root)
            {
                CloseRoot();
                busy = false;
                return;
            }
            // If the window isn't in the stack, ignore
            if (!windowStack.Contains(window))
            {
                busy = false;
                return;
            }
            // Pop until we find it
            HudWindow top = null;
            while (windowStack.Count > 0)
            {
                top = windowStack.Pop();
                if (top is IHudNavigable tnav)
                    tnav.OnBlur();
                top.CloseWindow();
                if (top == window)
                    break;
            }
            var now = Current;
            if (now != null)
            {
                if (!now.isOpen)
                    now.OpenWindow();
                if (now is IHudNavigable nnav)
                    nnav.OnFocus(this);
            }

            UpdateState();
            busy = false;
        }

        private void CloseChildren()
        {
            while (windowStack.Count > 0)
            {
                var w = windowStack.Pop();
                if (w is IHudNavigable nav)
                    nav.OnBlur();
                w.CloseWindow();
            }
        }
        #endregion

        private void UpdateState()
        {
            hasHudWindowOpen.SetFlag("base", Root != null || windowStack.Count > 0);

#if UNITY_EDITOR
            _windowStack = new List<HudWindow>(windowStack);
#endif
        }
    }
}