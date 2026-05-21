using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Complete
{
    [RequireComponent(typeof(BoxCollider))]
    public class PowerUpSpawnZone : NetworkBehaviour
    {
        [Header("Pool de Power-Ups")]
        public GameObject[] m_PowerUpPrefabs;

        [Header("Configuración del Spawn")]
        public float m_SpawnInterval = 10f;
        public int m_MaxItemsInZone = 5;
        public float m_ClearanceRadius = 1.5f;
        public LayerMask m_ObstacleMask;

        private BoxCollider m_ZoneCollider;
        private List<GameObject> m_SpawnedItems = new List<GameObject>();

        // Nueva lista para seguir el rastro de los minitanques creados por esta zona
        private List<GameObject> m_SpawnedMiniTanks = new List<GameObject>();

        private void Awake()
        {
            m_ZoneCollider = GetComponent<BoxCollider>();
            m_ZoneCollider.isTrigger = true;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                // Escuchamos el evento de revancha/reinicio si tu GameManager lo expone,
                // pero para asegurar al 100% sin depender de eventos, limpiamos al arrancar el objeto en red.
                LimpiarZona();
                StartCoroutine(SpawnRoutine());
            }
        }

        // ====================================================================
        // ¡LA SOLUCIÓN AL RESET!: Método público para destruir todo en la revancha
        // ====================================================================
        public void LimpiarZona()
        {
            if (!IsServer) return;

            // 1. Limpiar Power-ups del suelo
            foreach (GameObject item in m_SpawnedItems)
            {
                if (item != null)
                {
                    NetworkObject netObj = item.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned) netObj.Despawn();
                }
            }
            m_SpawnedItems.Clear();

            // 2. Limpiar Minitanques vivos
            foreach (GameObject mini in m_SpawnedMiniTanks)
            {
                if (mini != null)
                {
                    NetworkObject netObj = mini.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned) netObj.Despawn();
                }
            }
            m_SpawnedMiniTanks.Clear();
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(m_SpawnInterval);

                m_SpawnedItems.RemoveAll(item => item == null);
                m_SpawnedMiniTanks.RemoveAll(mini => mini == null); // Limpieza de nulos

                if (m_SpawnedItems.Count < m_MaxItemsInZone && m_PowerUpPrefabs.Length > 0)
                {
                    TrySpawnPowerUp();
                }
            }
        }

        private void TrySpawnPowerUp()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 randomPoint = GetRandomPointInBounds(m_ZoneCollider.bounds);

                if (!Physics.CheckSphere(randomPoint, m_ClearanceRadius, m_ObstacleMask))
                {
                    GameObject randomPrefab = m_PowerUpPrefabs[Random.Range(0, m_PowerUpPrefabs.Length)];

                    GameObject powerUpInstance = Instantiate(randomPrefab, randomPoint, Quaternion.identity);
                    m_SpawnedItems.Add(powerUpInstance);

                    // Vinculamos esta zona al Power-up si es de tipo minitanque para rastrear al "hijo"
                    PowerUpMiniTank compMini = powerUpInstance.GetComponent<PowerUpMiniTank>();
                    if (compMini != null)
                    {
                        compMini.m_SourceZone = this; // Le decimos al power-up quién es su zona creadora
                    }

                    NetworkObject netObj = powerUpInstance.GetComponent<NetworkObject>();
                    if (netObj != null) netObj.Spawn();

                    break;
                }
            }
        }

        // Método para que el PowerUpMiniTank registre el minitanque vivo en la zona
        public void RegistrarMinitanque(GameObject miniTank)
        {
            if (miniTank != null) m_SpawnedMiniTanks.Add(miniTank);
        }

        private Vector3 GetRandomPointInBounds(Bounds bounds)
        {
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        private void OnDrawGizmos()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawCube(transform.position, box.size);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, box.size);
        }
    }
}