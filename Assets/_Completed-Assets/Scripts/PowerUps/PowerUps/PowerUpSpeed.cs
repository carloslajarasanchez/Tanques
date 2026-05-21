using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Complete
{
    public class PowerUpSpeed : BasePowerUp
    {
        [Header("Configuración de Velocidad")]
        public float m_SpeedMultiplier = 1.5f;
        public float m_Duration = 5f;

        public override bool ApplyEffect(GameObject target)
        {
            // El servidor obtiene el NetworkObject del tanque para saber quién es el dueño
            NetworkObject netObj = target.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                // Filtro de seguridad: Evitamos acumular bufos de velocidad si ya tiene uno activo localmente
                // Para comprobarlo en red de forma sencilla, miramos si el componente ya existe en el target en el servidor
                if (target.GetComponent<TemporarySpeedBoost>() != null) return false;

                // 1. Añadimos el componente en el Servidor (para que conste que ya lo tiene y no agarre otro)
                TemporarySpeedBoost serverBoost = target.AddComponent<TemporarySpeedBoost>();
                serverBoost.Initialize(target.GetComponent<TankMovement>(), m_SpeedMultiplier, m_Duration);

                // 2. ¡La Clave!: Si el dueño no es el servidor (es decir, es un cliente remoto),
                // le enviamos un ClientRpc directo a su máquina para que aplique la velocidad en su simulación local.
                if (!netObj.IsOwner)
                {
                    ClientRpcParams clienteDestino = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { netObj.OwnerClientId } }
                    };
                    ApplySpeedBoostClientRpc(m_SpeedMultiplier, m_Duration, clienteDestino);
                }

                Debug.Log($"[Power-up] Velocidad procesada en el servidor para el jugador {netObj.OwnerClientId}");
                return true;
            }
            return false;
        }

        // ====================================================================
        // CLIENT RPC TARGETED: Solo se ejecuta en el PC del jugador que recogió el Power-up
        // ====================================================================
        [ClientRpc]
        private void ApplySpeedBoostClientRpc(float multiplier, float duration, ClientRpcParams rpcParams = default)
        {
            // Buscamos nuestro propio tanque local (el que tiene el script TankMovement donde IsOwner es true)
            TankMovement[] todosLosMovimientos = FindObjectsOfType<TankMovement>();

            foreach (TankMovement movement in todosLosMovimientos)
            {
                // Solo le aplicamos el boost a nuestro propio tanque
                if (movement.IsOwner)
                {
                    // Si por algún motivo ya lo tuviera (anti-lag), salimos
                    if (movement.gameObject.GetComponent<TemporarySpeedBoost>() != null) return;

                    TemporarySpeedBoost clientBoost = movement.gameObject.AddComponent<TemporarySpeedBoost>();
                    clientBoost.Initialize(movement, multiplier, duration);
                    break;
                }
            }
        }
    }

    // ====================================================================
    // COMPONENTE AUXILIAR TEMPORAL (Se queda igual, pero ahora se ejecuta en el PC correcto)
    // ====================================================================
    public class TemporarySpeedBoost : MonoBehaviour
    {
        private TankMovement m_Movement;
        private float m_OriginalSpeed;

        public void Initialize(TankMovement movement, float multiplier, float duration)
        {
            if (movement == null) return;

            m_Movement = movement;
            m_OriginalSpeed = m_Movement.m_Speed;
            m_Movement.m_Speed *= multiplier;

            StartCoroutine(ResetSpeedAfterTime(duration));
        }

        private IEnumerator ResetSpeedAfterTime(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (m_Movement != null)
            {
                m_Movement.m_Speed = m_OriginalSpeed;
            }
            Destroy(this);
        }
    }
}