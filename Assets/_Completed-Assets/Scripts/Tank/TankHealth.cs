using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Complete
{
    public class TankHealth : NetworkBehaviour
    {
        public float m_StartingHealth = 100f;
        public Slider m_Slider;
        public Image m_FillImage;
        public Color m_FullHealthColor = Color.green;
        public Color m_ZeroHealthColor = Color.red;
        public GameObject m_ExplosionPrefab;

        // VARIABLES DE RED
        public NetworkVariable<float> m_CurrentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> m_CurrentLives = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private bool m_Dead;
        private Rigidbody m_Rigidbody;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn()
        {
            m_CurrentHealth.OnValueChanged += OnHealthChanged;

            if (IsServer)
            {
                m_CurrentHealth.Value = m_StartingHealth;
                m_CurrentLives.Value = 3;
                m_Dead = false;
            }

            SetHealthUI(m_CurrentHealth.Value);
        }

        public override void OnNetworkDespawn()
        {
            m_CurrentHealth.OnValueChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float oldHealth, float newHealth)
        {
            SetHealthUI(newHealth);
        }

        public void TakeDamage(float amount)
        {
            if (!IsServer || m_Dead) return;

            m_CurrentHealth.Value -= amount;

            if (m_CurrentHealth.Value <= 0f)
            {
                OnDeath();
            }
        }

        private void SetHealthUI(float healthValue)
        {
            if (m_Slider != null) m_Slider.value = healthValue;
            if (m_FillImage != null) m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, healthValue / m_StartingHealth);
        }

        private void OnDeath()
        {
            m_Dead = true;

            PlayExplosionClientRpc(transform.position);

            m_CurrentLives.Value--;

            if (m_CurrentLives.Value > 0)
            {
                Respawn();
            }
            else
            {
                DisableTankClientRpc();

                if (GameUIManager.Instance != null)
                {
                    GameUIManager.Instance.CheckGameOver();
                }
            }
        }

        [ClientRpc]
        private void PlayExplosionClientRpc(Vector3 position)
        {
            if (m_ExplosionPrefab != null)
            {
                GameObject explosionInstance = Instantiate(m_ExplosionPrefab, position, Quaternion.identity);
                ParticleSystem particles = explosionInstance.GetComponent<ParticleSystem>();
                AudioSource audioSrc = explosionInstance.GetComponent<AudioSource>();

                if (particles != null)
                {
                    particles.Play();
                    Destroy(explosionInstance, particles.main.duration);
                }

                if (audioSrc != null) audioSrc.Play();
            }
        }

        // ==========================================
        // REAPARICIÓN EN LOS SPAWNPOINTS DE TU IMAGEN
        // ==========================================
        public void Respawn()
        {
            if (!IsServer) return;

            m_CurrentHealth.Value = m_StartingHealth;
            m_Dead = false;

            // Posiciones por defecto por si fallara la búsqueda (centro del mapa)
            Vector3 targetPosition = Vector3.zero;
            Quaternion targetRotation = Quaternion.identity;

            // Asignamos el punto según tu captura del PlayerSpawner:
            // Host (ID 0) va a "SpawnPoint1". Cliente (ID 1) va a "SpawnPoint2".
            string spawnName = (OwnerClientId == 0) ? "SpawnPoint1" : "SpawnPoint2";
            GameObject spawnPointObject = GameObject.Find(spawnName);

            if (spawnPointObject != null)
            {
                targetPosition = spawnPointObject.transform.position;
                targetRotation = spawnPointObject.transform.rotation;
            }
            else
            {
                // Plan B: Si no los encuentra sueltos, los busca dentro del padre "PlayerSpawner"
                GameObject spawner = GameObject.Find("PlayerSpawner");
                if (spawner != null)
                {
                    Transform fallbackSpawn = spawner.transform.Find(spawnName);
                    if (fallbackSpawn != null)
                    {
                        targetPosition = fallbackSpawn.position;
                        targetRotation = fallbackSpawn.rotation;
                    }
                }
                else
                {
                    Debug.LogError($"[TankHealth] ¡Error crítico! No se encuentra el objeto '{spawnName}' en la escena.");
                }
            }

            // Forzamos la detención de físicas en el Servidor antes de mover
            if (m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = true;
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }

            // Teletransportamos al tanque a su posición de inicio real
            transform.position = targetPosition;
            transform.rotation = targetRotation;

            if (m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = false;
            }

            // Mandamos las coordenadas exactas calculadas a las pantallas de los clientes
            ResetVisualsAndPhysicsClientRpc(targetPosition, targetRotation);
        }

        [ClientRpc]
        private void ResetVisualsAndPhysicsClientRpc(Vector3 targetPosition, Quaternion targetRotation)
        {
            if (m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = true;
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }

            // Forzamos el cambio de posición en el cliente para sincronizar el NetworkTransform
            transform.position = targetPosition;
            transform.rotation = targetRotation;

            // Activamos barras de vida, mallas de renderizado y el círculo verde del suelo
            SetTankActiveState(true);
            SetHealthUI(m_StartingHealth);

            if (m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = false;
            }
        }

        [ClientRpc]
        private void DisableTankClientRpc()
        {
            SetTankActiveState(false);
        }

        private void SetTankActiveState(bool active)
        {
            // Forzamos la activación incluyendo objetos desactivados (parámetro true)
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = active;
            foreach (Projector p in GetComponentsInChildren<Projector>(true)) p.enabled = active;
            foreach (Collider c in GetComponentsInChildren<Collider>(true)) c.enabled = active;

            TankMovement movement = GetComponent<TankMovement>();
            if (movement != null) movement.enabled = active;

            TankShooting shooting = GetComponent<TankShooting>();
            if (shooting != null) shooting.enabled = active;

            Canvas canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null) canvas.gameObject.SetActive(active);
        }
    }
}