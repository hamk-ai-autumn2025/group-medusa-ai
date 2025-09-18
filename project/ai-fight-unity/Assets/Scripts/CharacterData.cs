using UnityEngine;
using dev.susybaka.Shared.Attributes;

namespace dev.susybaka.TurnBasedGame.Characters
{
    [CreateAssetMenu(fileName = "New Character Data", menuName = "Turn Based Game/Character Data")]
    public class CharacterData : ScriptableObject
    {
        public string characterName;
        public Sprite[] characterPortraits;
        [SoundName] public string characterDialogueSound;
    }
}