using System.Collections;
using System.Collections.Generic;
using dev.susybaka.Shared.Attributes;
using dev.susybaka.Shared.Audio;
using dev.susybaka.Shared.UI;
using dev.susybaka.TurnBasedGame.Input;
using dev.susybaka.TurnBasedGame.Minigame;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class CreditsWindow : HudWindow
    {
        public GameObject winText;
        public GameObject loseText;
        public GameObject doneText;
        public MinigameMoveObject moveObject;
        [SoundName] public string winSound = "<None>";
        [SoundName] public string loseSound = "<None>";
        [SoundName] public string bgmSound = "<None>";

        private InputHandler input;

        protected override void Awake()
        {
            base.Awake();
            input = GameManager.Instance.Input;
        }

        public void TriggerEnd(bool win)
        {
            doneText?.SetActive(false);
            moveObject?.Initialize();

            if (win)
            {
                winText?.SetActive(true);
                loseText?.SetActive(false);
            }
            else
            {
                winText?.SetActive(false);
                loseText?.SetActive(true);
            }

            AudioManager.Instance.StopPlayingAll();

            if (win)
            {
                AudioManager.Instance.Play(winSound);
            }
            else
            {
                AudioManager.Instance.Play(loseSound);
            }
            AudioManager.Instance.Play(bgmSound);
        }

        public void End()
        {
            winText?.SetActive(false);
            loseText?.SetActive(false);
            doneText?.SetActive(true);
            StartCoroutine(IE_WaitForInputAndQuit());
        }

        private IEnumerator IE_WaitForInputAndQuit()
        {
            while (!input.AnyInput)
            {
                yield return null;
            }
            Application.Quit();
        }
    }
}