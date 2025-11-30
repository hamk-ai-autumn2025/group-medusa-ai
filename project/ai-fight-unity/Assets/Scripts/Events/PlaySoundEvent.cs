using UnityEngine;
using dev.susybaka.Shared.Attributes;
using dev.susybaka.Shared.Audio;

namespace dev.susybaka.TurnBasedGame.Events
{
    [CreateAssetMenu(fileName = "New Play Sound Event", menuName = "Turn Based Game/Events/Play Sound Event")]
    public class PlaySoundEvent : ScriptableObject
    {
        [SoundName] public string soundName = "<None>";

        public void TriggerEvent()
        {
            if (!AudioManager.Instance)
            {
                Debug.LogWarning("AudioManager not currently available.");
                return;
            }

            AudioManager.Instance.Play(soundName);
        }
    }
}