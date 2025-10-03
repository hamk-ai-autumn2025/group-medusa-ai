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
        [SerializeField] private HudWindow talkWindow;

        public ActionWindow ActionWindow => actionWindow;
        public TargetWindow TargetWindow => targetWindow;
        public PartyWindow PartyMembers => partyMembers;
        public ActionPointBarWindow ActionPointBar => actionPointBar;
        public LabelWindow DescriptionWindow => descriptionWindow;
        public HudWindow TalkWindow => talkWindow;

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

            partyMembers?.Initialize(manager);
            actionPointBar?.Initialize(manager);
            actionWindow?.Initialize(manager);
            targetWindow?.Initialize(manager);
            talkWindow?.Initialize(manager);
            descriptionWindow?.Initialize(manager);

            actionWindow?.SetTargetWindow(targetWindow);
            partyMembers?.SetActionWindow(actionWindow);
            partyMembers?.SetActionPointBar(actionPointBar);
            descriptionWindow?.CloseWindow();
        }
    }
}