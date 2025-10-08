using UnityEngine;
using UnityEngine.Events;
using dev.susybaka.TurnBasedGame.Interfaces;

namespace dev.susybaka.TurnBasedGame.Characters
{
    public class FriendCharacter : Character, IInteractable
    {
        private NPCOverworldController npcController;
        private CharacterTrailRecorder trailRecorder;

        public UnityEvent<Character> onInteract;

        public bool isFollowing = false;

        protected override void Awake()
        {
            base.Awake();
            npcController = GetComponentInChildren<NPCOverworldController>(true);
        }

        private void Update()
        {
            if (npcController == null || Party?.leader == null || trailRecorder == null || !isFollowing)
                return;

            isSprinting = Party.leader.isSprinting;
            npcController.sprint = isSprinting;
        }

        public override void Initialize(Party party = null)
        {
            base.Initialize(party);

            if (party != null && party.leader != null)
            {
                party.leader.transform.TryGetComponentInChildren(true, out trailRecorder);
            }
        }

        public void Interact()
        {
            Debug.Log("Interact");
            onInteract?.Invoke(this);
        }

        public void FollowPartyLeader(int distance = -1)
        {
            if (trailRecorder == null || npcController == null)
                return;

            if (distance < 0)
            {
                for (int i = 0; i < Party.members.Count; i++)
                {
                    if (Party.members[i] == this)
                    {
                        distance = i + (1 * i); // 1, 3, 5, ...
                        break;
                    }
                }
                npcController.FollowCharacterTrail(trailRecorder, distance);
            }
            else
            {
                npcController.FollowCharacterTrail(trailRecorder, distance);
            }
            isFollowing = true;
        }

        public void StopFollowing()
        {
            if (npcController == null)
                return;

            npcController.Stop();
            npcController.ClearPath();
            isFollowing = false;
        }
    }
}