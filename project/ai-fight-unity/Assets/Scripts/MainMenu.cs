using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.Shared.Attributes;
using dev.susybaka.Shared.Audio;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class MainMenu : MonoBehaviour
    {
        [SoundName] public string bgm = "<None>";
        [SoundName] public string nextBgm = "<None>";
        [SoundName] public string hoverSound = "<None>";
        [SoundName] public string clickSound = "<None>";

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1f);
            AudioManager.Instance.StopPlayingAll();
            AudioManager.Instance.Play(bgm);
        }

        public void StartGame()
        {
            Debug.Log("Start Game button clicked.");
            AudioManager.Instance.StopPlayingAll();
            AudioManager.Instance.Play(nextBgm);
            UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        }

        public void QuitGame()
        {
            Debug.Log("Quit Game button clicked.");
            Application.Quit();
        }

        public void Hover()
        {
            AudioManager.Instance.Play(hoverSound);
        }

        public void Click()
        {
            AudioManager.Instance.Play(clickSound);
        }
    }
}