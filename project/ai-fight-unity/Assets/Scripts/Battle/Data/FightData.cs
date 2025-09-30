using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Dialogue.Data;
using dev.susybaka.Shared.Attributes;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(fileName = "New Fight Data", menuName = "Turn Based Game/Battles/Fight")]
    public class FightData : ScriptableObject
    {
        public string fightName = "New Fight";
        public GameObject environmentPrefab;
        [SoundName] public string music = "bgm_btl_dev";
        public Vector3 playerPosition;
        public Vector3 playerHeartPosition;
        public DialogueData startDialogue;
    }
}
