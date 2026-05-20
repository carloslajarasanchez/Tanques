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
        private Vector3 m_SpawnPosition;
        private Quaternion m_SpawnRotation;
        private bool m_SpawnPointSaved = false; // Nos asegura guardar el spawn correcto

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

        private void Update()
        {
            // SOLUCIÓN SPRAWN: Esperamos a que el tanque se mueva de la posición cero (0,0,0) 
            // para registrar su verdadero punto de partida asignado por el GameManager.
            if (!m_SpawnPointSaved && transform.position != Vector3.zero)
            {
                m_SpawnPosition = transform.position;
                m_SpawnRotation = transform.rotation;
                m_SpawnPointSaved = true;
            }
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
            if (!IsServer) return;
            if (m_Dead) return;

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

        // Ahora es pública para que la llame el GameUIManager
        public void Respawn()
        {
            m_CurrentHealth.Value = m_StartingHealth;
            m_Dead = false;

            // SOLUCIÓN BARRA DE VIDA: Forzamos la reactivación total de componentes y UI en los clientes
            // ANTES de recolocar el tanque, asegurando que el Canvas vuelva a existir.
            TeleportAndResetClientRpc(m_SpawnPosition, m_SpawnRotation);
        }

        [ClientRpc]
        private void TeleportAndResetClientRpc(Vector3 position, Quaternion rotation)
        {
            // 1. Primero encendemos todo el tanque y su interfaz
            SetTankActiveState(true);

            // 2. Colocamos el tanque en su base real guardada
            transform.position = position;
            transform.rotation = rotation;

            // 3. Reseteamos físicas e inercias
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 4. Forzamos la actualización visual de la barra de vida a 100
            SetHealthUI(m_StartingHealth);
        }

        [ClientRpc]
        private void DisableTankClientRpc()
        {
            SetTankActiveState(false);
        }

        private void SetTankActiveState(bool active)
        {
            foreach (var r in GetComponentsInChildren<MeshRenderer>()) r.enabled = active;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = active;

            TankMovement movement = GetComponent<TankMovement>();
            TankShooting shooting = GetComponent<TankShooting>();
            if (movement != null) movement.enabled = active;
            if (shooting != null) shooting.enabled = active;

            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null) canvas.gameObject.SetActive(active);
        }
    }
}