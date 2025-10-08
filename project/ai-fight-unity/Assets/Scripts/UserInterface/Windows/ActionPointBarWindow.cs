using UnityEngine;
using UnityEngine.UI;
using TMPro;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.Shared.UI;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class ActionPointBarWindow : HudWindow
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI label;

        private Party party;

        protected override void Awake()
        {
            base.Awake();

            if (label == null || slider == null)
                return;

            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = 0;
            label.text = "AP\n0";
        }

        public void SetParty(Party party)
        {
            this.party = party;
        }

        public override void OpenWindow()
        {
            base.OpenWindow();
            UpdateBar();
        }

        public override void CloseWindow()
        {
            UpdateBar();
            base.CloseWindow();
        }

        public void UpdateBar()
        {
            if (label == null || slider == null || party == null)
                return;

            slider.maxValue = party.MaxPoints;
            slider.value = party.Points;
            label.text = $"AP\n{party.Points}";
        }
    }
}