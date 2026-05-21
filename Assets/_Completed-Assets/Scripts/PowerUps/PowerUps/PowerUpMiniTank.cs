using Unity.Netcode;
using UnityEngine;

namespace Complete
{
    public class PowerUpMiniTank : BasePowerUp
    {
        [Header("Configuración del Minitanque")]
        public GameObject m_MiniTankPrefab;

        [HideInInspector]
        public PowerUpSpawnZone m_SourceZone; // Referencia oculta a la zona de la que nació

        public override bool ApplyEffect(GameObject target)
        {
            NetworkObject playerNetObj = target.GetComponent<NetworkObject>();
            TankHealth playerHealth = target.GetComponent<TankHealth>();

            if (playerNetObj != null && playerHealth != null && m_MiniTankPrefab != null)
            {
                ulong ownerId = playerNetObj.OwnerClientId;
                Vector3 spawnPosition = target.transform.position + (target.transform.right * -2f) + Vector3.up * 0.5f;

                GameObject miniInstance = Instantiate(m_MiniTankPrefab, spawnPosition, Quaternion.identity);

                // ¡NUEVO!: Registramos el minitanque en la zona de origen para poder borrarlo en la revancha
                if (m_SourceZone != null)
                {
                    m_SourceZone.RegistrarMinitanque(miniInstance);
                }

                NetworkObject miniNetObj = miniInstance.GetComponent<NetworkObject>();
                if (miniNetObj != null) miniNetObj.Spawn();

                MiniTankAI aiComponent = miniInstance.GetComponent<MiniTankAI>();
                if (aiComponent != null)
                {
                    aiComponent.SetOwner(ownerId, target.transform);
                }

                Color colorObjetivo = (ownerId == 0) ? Color.blue : Color.red;
                CambiarColorMinitanqueClientRpc(miniNetObj, colorObjetivo);

                Debug.Log($"[Power-up] Minitanque creado en red y enlazado a la zona de spawn.");
                return true;
            }

            return false;
        }

        [ClientRpc]
        private void CambiarColorMinitanqueClientRpc(NetworkObjectReference miniTankRef, Color colorAsignado)
        {
            if (miniTankRef.TryGet(out NetworkObject miniTankNetObj))
            {
                Renderer[] renderers = miniTankNetObj.GetComponentsInChildren<Renderer>();
                foreach (Renderer rend in renderers)
                {
                    if (rend != null) rend.material.color = colorAsignado;
                }
            }
        }
    }
}