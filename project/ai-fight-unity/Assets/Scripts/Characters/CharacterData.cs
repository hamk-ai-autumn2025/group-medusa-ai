using UnityEngine;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Items;
using dev.susybaka.Shared.Attributes;

namespace dev.susybaka.TurnBasedGame.Characters.Data
{
    [CreateAssetMenu(fileName = "New Character Data", menuName = "Turn Based Game/Characters/Character")]
    public class CharacterData : ScriptableObject
    {
        public string characterName;
        public Sprite[] characterPortraits;
        [SoundName] public string characterDialogueSound;
        public CommandNodeData rootCommands;
        public InventoryData inventory;
    }
}