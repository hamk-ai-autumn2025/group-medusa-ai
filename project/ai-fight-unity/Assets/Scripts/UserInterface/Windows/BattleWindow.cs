using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.Shared.UI;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class BattleWindow : GameStateWindow
    {
        [Header("Battle Window")]
        [SerializeField] private PartyWindow partyMembers;
        [SerializeField] private ActionPointBarWindow actionPointBar;
        [SerializeField] private ActionWindow actionWindow;
        [SerializeField] private TargetWindow targetWindow;
        [SerializeField] private LabelWindow descriptionWindow;
        [SerializeField] private LabelWindow talkWindow;
        private CaptureTextInput talkCapture;
        [SerializeField] private PopupWindow popupWindow;
        [SerializeField] private SpeechWindow speechWindow;

        public ActionWindow ActionWindow => actionWindow;
        public TargetWindow TargetWindow => targetWindow;
        public PartyWindow PartyMembers => partyMembers;
        public ActionPointBarWindow ActionPointBar => actionPointBar;
        public LabelWindow DescriptionWindow => descriptionWindow;
        public LabelWindow TalkWindow => talkWindow;
        public CaptureTextInput TalkCapture => talkCapture;
        public PopupWindow PopupWindow => popupWindow;
        public SpeechWindow SpeechWindow => speechWindow;

        public void OpenPartyWindow(Party party)
        {
            actionPointBar?.SetParty(party);
            partyMembers?.OpenForPlanning(party);
        }

        public override void Initialize(GameManager manager)
        {
            if (initialized)
                return;

            base.Initialize(manager);
            talkCapture = talkWindow?.GetComponent<CaptureTextInput>();

            partyMembers?.Initialize(manager);
            actionPointBar?.Initialize(manager);
            actionWindow?.Initialize(manager);
            targetWindow?.Initialize(manager);
            talkWindow?.Initialize(manager);
            descriptionWindow?.Initialize(manager);
            popupWindow?.Initialize(manager);
            speechWindow?.Initialize(manager);

            actionWindow?.SetTargetWindow(targetWindow);
            partyMembers?.SetActionWindow(actionWindow);
            partyMembers?.SetActionPointBar(actionPointBar);
            descriptionWindow?.CloseWindow();
        }
    }
}