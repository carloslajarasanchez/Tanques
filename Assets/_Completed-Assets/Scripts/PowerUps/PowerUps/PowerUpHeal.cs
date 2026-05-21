using UnityEngine;

namespace Complete
{
    public class PowerUpHeal : BasePowerUp
    {
        [Header("Configuración de Cura")]
        public float m_HealAmount = 50f;

        public override bool ApplyEffect(GameObject target)
        {
            TankHealth health = target.GetComponent<TankHealth>();

            if (health != null)
            {
                if (health.m_CurrentHealth.Value >= health.m_StartingHealth) return false;

                float nuevaVida = health.m_CurrentHealth.Value + m_HealAmount;
                health.m_CurrentHealth.Value = Mathf.Min(nuevaVida, health.m_StartingHealth);

                Debug.Log("[Power-up] Tanque curado.");
                return true;
            }
            return false;
        }
    }
}