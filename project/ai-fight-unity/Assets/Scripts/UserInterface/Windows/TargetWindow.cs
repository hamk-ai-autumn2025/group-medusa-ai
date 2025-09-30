using System;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Battle;
using dev.susybaka.TurnBasedGame.Characters;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class TargetWindow : CommandWindow
    {
        private TargetGroup targetGroup;
        private List<Character> pool;
        private Action<IList<Character>> onSelected;

        public void OpenFor(TargetGroup targetGroup, Action<IList<Character>> onSelected)
        {
            this.targetGroup = targetGroup;
            this.onSelected = onSelected;

            pool = BuildPool(this.targetGroup); // create from BattleHandler state

            // Reuse your existing UI by listing names:
            commands.Clear();
            for (int i = 0; i < pool.Count; i++)
            {
                Character ch = pool[i];

                if (ch == null)
                {
                    Debug.LogError($"TargetWindow: Null character in target list at index {i}");
                    continue;
                }

                Command cmd = new Command(ch.data.characterName, $"Target {ch.data.characterName}", true, string.Empty, () =>
                {
                    this.onSelected?.Invoke(new List<Character> { ch });
                    navHandler.PopWindow();
                });
                commands.Add(cmd);
            }
            UpdateCommands(commands.ToArray());
        }

        private List<Character> BuildPool(TargetGroup g)
        {
            var bh = GameManager.Instance.BattleHandler;
            // Example stub:
            switch (g)
            {
                case TargetGroup.self:
                    return new List<Character> { bh.PlayerCharacter };
                case TargetGroup.ally:
                case TargetGroup.allies:
                    return new List<Character>(bh.allies.members);
                case TargetGroup.enemy:
                case TargetGroup.enemies:
                    return new List<Character>(bh.enemies.members);
                default:
                    return new List<Character>();
            }
        }
    }
}