using System;

namespace dev.susybaka.TurnBasedGame.UI
{
    [System.Serializable]
    public struct Command
    {
        public string name;
        public string description;
        public bool enabled;
        public string disabledReason;
        public Action action;

        public Command(string name, string description, bool enabled, string disabledReason, Action action)
        {
            this.name = name;
            this.description = description;
            this.enabled = enabled;
            this.disabledReason = disabledReason;
            this.action = action;
        }
    }
}