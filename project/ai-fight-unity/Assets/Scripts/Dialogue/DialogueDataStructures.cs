using UnityEngine;
using dev.susybaka.TurnBasedGame.Characters.Data;

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
}
