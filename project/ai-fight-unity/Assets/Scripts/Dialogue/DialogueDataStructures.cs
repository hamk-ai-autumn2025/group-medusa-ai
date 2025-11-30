using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Characters.Data;
using dev.susybaka.TurnBasedGame.Items;

namespace dev.susybaka.TurnBasedGame.Dialogue
{
    public enum CharacterPortrait { neutral, happy, angry, sad, confused, special }

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

    public readonly struct DialogueContext
    {
        public readonly Character source { init; get; }
        public readonly IList<Character> targets { init; get; }
        public readonly AbilityData ability { init; get; }
        public readonly IList<ItemData> items { init; get; }

        public DialogueContext(Character source, IList<Character> targets, AbilityData action, IList<ItemData> items)
        {
            this.source = source;
            this.targets = targets ?? new List<Character>();
            this.ability = action;
            this.items = items;
        }
    }
}
