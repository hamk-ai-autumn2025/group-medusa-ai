using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class VolumeSlider : MonoBehaviour
    {
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string exposedParameter = "MasterVolume";
        private Slider slider;

        private const float minDb = -80f;
        private const float maxDb = 0f;

        private void Awake()
        {
            slider = GetComponent<Slider>();
            if (slider != null)
            {
                slider.onValueChanged.AddListener(OnSliderValueChanged);
                // Optionally initialize slider value from mixer
                float currentDb;
                if (audioMixer.GetFloat(exposedParameter, out currentDb))
                {
                    slider.value = DbToLinear(currentDb);
                }
            }
        }

        private void OnSliderValueChanged(float value)
        {
            float db = LinearToDb(value);
            audioMixer.SetFloat(exposedParameter, db);
        }

        // Converts slider value (0-1) to decibels
        private float LinearToDb(float linear)
        {
            if (linear <= 0.0001f)
                return minDb;
            return Mathf.Lerp(minDb, maxDb, Mathf.Log10(linear * 9 + 1));
        }

        // Converts decibels to slider value (0-1)
        private float DbToLinear(float db)
        {
            if (db <= minDb)
                return 0f;
            float t = (db - minDb) / (maxDb - minDb);
            return (Mathf.Pow(10, t) - 1) / 9;
        }
    }
}