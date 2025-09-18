using System.Collections.Generic;
using dev.susybaka.TurnBasedGame.Characters;

namespace dev.susybaka.TurnBasedGame.Combat
{
    public class TurnHandler
    {
        private List<Character> turnOrder;
        private int currentTurnIndex;

        public void StartBattle(List<Character> participants)
        {
            // Initialize the turn order based on participants' speed or other criteria
            // Set the currentTurnIndex to 0 to start from the first character
        }

        public void EndTurn()
        {
            // Perform necessary actions to end the current turn
            // This can include updating turn counters, checking victory/defeat conditions, etc.
        }

        public Character GetCurrentTurnCharacter()
        {
            // Return the character whose turn it currently is
            return null;
        }

        public void ProceedToNextTurn()
        {
            // Move to the next turn in the turn order
        }
    }
}