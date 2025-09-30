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
        [SerializeField] private HudWindow ultimateBar;
        [SerializeField] private ActionWindow actionWindow;
        [SerializeField] private TargetWindow targetWindow;
        [SerializeField] private HudWindow talkWindow;

        public ActionWindow ActionWindow => actionWindow;
        public TargetWindow TargetWindow => targetWindow;
        public PartyWindow PartyMembers => partyMembers;
        public HudWindow UltimateBar => ultimateBar;
        public HudWindow TalkWindow => talkWindow;

        public void OpenPartyWindow(Party party)
        {
            partyMembers?.OpenForPlanning(party);
        }

        public override void Initialize(GameManager manager)
        {
            if (initialized)
                return;

            base.Initialize(manager);

            partyMembers?.Initialize(manager);
            ultimateBar?.Initialize(manager);
            actionWindow?.Initialize(manager);
            targetWindow?.Initialize(manager);
            talkWindow?.Initialize(manager);

            actionWindow?.SetTargetWindow(targetWindow);
            partyMembers?.SetActionWindow(actionWindow);
        }
    }
}