using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Characters
{
    public class Character : MonoBehaviour
    {
        public CharacterData characterData;

        [Header("Character")]
        public int level = 1;
        public int health = 1;
        public int mana = 1;
        public int attackPower = 1;
        public int defense = 1;
        public bool isAlive = true;
        public bool isFighting = false;

        public void Attack(Character target)
        {
            // Perform attack logic here
        }

        public void ModifyHealth(int damage)
        {
            // Apply damage calculation and update health

            health -= damage;

            if (health <= 0)
            {
                isAlive = false;
                health = 0;
            }
        }

        public void UseItem(int item)
        {
            // Implement item usage logic
        }
    }
}