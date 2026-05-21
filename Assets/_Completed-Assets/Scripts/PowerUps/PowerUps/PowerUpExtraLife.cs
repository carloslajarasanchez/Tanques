using UnityEngine;

namespace Complete
{
    public class PowerUpExtraLife : BasePowerUp
    {
        public override bool ApplyEffect(GameObject target)
        {
            TankHealth health = target.GetComponent<TankHealth>();

            if (health != null)
            {
                if (health.m_CurrentLives.Value >= 3) return false;

                health.m_CurrentLives.Value++;
                Debug.Log("[Power-up] Vida extra añadida.");
                return true;
            }
            return false;
        }
    }
}