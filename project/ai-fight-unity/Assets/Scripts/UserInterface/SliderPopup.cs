using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class SliderPopup : HudPopup
    {
        private Slider slider;

        protected override void Awake()
        {
            base.Awake();
            slider = GetComponentInChildren<Slider>(true);
            slider.maxValue = 100;
            slider.value = slider.maxValue;
        }

        public void Show(int value, int maxValue)
        {
            slider.maxValue = maxValue;
            slider.value = value;
            base.Show();
        }

        public void Refresh(int value, int maxValue)
        {
            slider.maxValue = maxValue;
            slider.value = value;
            base.Refresh();
        }
    }
}