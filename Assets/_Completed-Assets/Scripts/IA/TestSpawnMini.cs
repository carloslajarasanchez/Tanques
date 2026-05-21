using UnityEngine;
using Unity.Netcode;

namespace Complete
{
    public class TestSpawnMini : NetworkBehaviour
    {
        [Header("Prefab del Minitanque de Red")]
        public GameObject m_MiniTankPrefab; // Arrastra aquí el prefab del minitanque configurado

        private void Update()
        {
            // Solo dejamos que el Host/Servidor procese el botón de creación
            if (!IsServer) return;

            // Si el Host pulsa la tecla M
            if (Input.GetKeyDown(KeyCode.M))
            {
                // Buscamos el tanque del Host (que siempre tiene el OwnerClientId en 0)
                TankHealth hostTank = null;
                TankHealth[] tanquesEnEscena = FindObjectsOfType<TankHealth>();

                foreach (TankHealth tanque in tanquesEnEscena)
                {
                    if (tanque.OwnerClientId == 0)
                    {
                        hostTank = tanque;
                        break;
                    }
                }

                // Si encontramos al Host, le hacemos aparecer su minitanque aliado
                if (hostTank != null && m_MiniTankPrefab != null)
                {
                    // Calculamos una posición a su lado izquierdo para que no aparezca incrustado
                    Vector3 spawnPosition = hostTank.transform.position + (hostTank.transform.right * -2f) + Vector3.up * 0.5f;

                    // 1. Instanciamos físicamente en el Servidor
                    GameObject miniInstance = Instantiate(m_MiniTankPrefab, spawnPosition, Quaternion.identity);

                    // 2. Le damos vida en la red Netcode para que aparezca en el cliente
                    NetworkObject netObj = miniInstance.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        netObj.Spawn();
                    }

                    // 3. Le pasamos el ID del dueño y su Transform para que la IA empiece a seguirle
                    MiniTankAI aiComponent = miniInstance.GetComponent<MiniTankAI>();
                    if (aiComponent != null)
                    {
                        aiComponent.SetOwner(0, hostTank.transform);
                    }

                    Debug.Log("[Test Red] Minitanque aliado invocado con éxito para el Host.");
                }
            }
        }
    }
}