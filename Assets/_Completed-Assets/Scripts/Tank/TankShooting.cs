using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Complete
{
    public class TankShooting : NetworkBehaviour
    {
        public int m_PlayerNumber = 1;
        public GameObject m_ShellPrefab;            // El Prefab de tu bala (asegúrate de que tenga el NetworkObject asignado)
        public Transform m_FireTransform;           // Punto de salida de la bala (en la punta del cañón)
        public Slider m_AimSlider;
        public AudioSource m_ShootingAudio;
        public AudioClip m_ChargingClip;
        public AudioClip m_FireClip;
        public float m_MinLaunchForce = 15f;
        public float m_MaxLaunchForce = 30f;
        public float m_MaxChargeTime = 0.75f;

        private string m_FireButton;
        private float m_CurrentLaunchForce;
        private float m_ChargeSpeed;
        private bool m_Fired;

        private void OnEnable()
        {
            m_CurrentLaunchForce = m_MinLaunchForce;
            if (m_AimSlider != null) m_AimSlider.value = m_MinLaunchForce;
        }

        private void Start()
        {
            m_FireButton = "Fire" + m_PlayerNumber;
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }

        private void Update()
        {
            // Solo el dueño local de este tanque procesa los botones de entrada de datos
            if (!IsOwner) return;

            if (m_AimSlider != null) m_AimSlider.value = m_MinLaunchForce;

            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire();
            }
            else if (Input.GetButtonDown(m_FireButton))
            {
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;

                if (m_ShootingAudio != null)
                {
                    m_ShootingAudio.clip = m_ChargingClip;
                    m_ShootingAudio.Play();
                }
            }
            else if (Input.GetButton(m_FireButton) && !m_Fired)
            {
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                if (m_AimSlider != null) m_AimSlider.value = m_CurrentLaunchForce;
            }
            else if (Input.GetButtonUp(m_FireButton) && !m_Fired)
            {
                Fire();
            }
        }

        private void Fire()
        {
            m_Fired = true;

            // Enviamos la petición de disparo junto con la fuerza cargada al servidor
            FireServerRpc(m_CurrentLaunchForce);

            m_CurrentLaunchForce = m_MinLaunchForce;
        }

        // ====================================================================
        // SERVERN RPC: El Servidor procesa la creación de la bala
        // ====================================================================
        [ServerRpc]
        private void FireServerRpc(float launchForce)
        {
            // Sistema de seguridad: si se te olvidó asignar algo en el inspector, te avisa en consola
            if (m_ShellPrefab == null)
            {
                Debug.LogError("[TankShooting] Error: No has asignado el Prefab de la bala en el Inspector del tanque.");
                return;
            }
            if (m_FireTransform == null)
            {
                Debug.LogError("[TankShooting] Error: No has asignado el punto de salida (FireTransform) en el Inspector del tanque.");
                return;
            }

            // 1. Instanciamos el objeto físico en el Servidor
            GameObject shellInstance = Instantiate(m_ShellPrefab, m_FireTransform.position, m_FireTransform.rotation);

            // 2. Le inyectamos la fuerza física inicial usando su Rigidbody normal con gravedad
            Rigidbody shellRigidbody = shellInstance.GetComponent<Rigidbody>();
            if (shellRigidbody != null)
            {
                shellRigidbody.isKinematic = false;
                shellRigidbody.velocity = launchForce * m_FireTransform.forward;
            }
            else
            {
                Debug.LogWarning("[TankShooting] Alerta: El Prefab de la bala no tiene un componente Rigidbody.");
            }

            // 3. Spawneamos el objeto en la red multijugador
            NetworkObject netObj = shellInstance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
            else
            {
                Debug.LogError("[TankShooting] ¡ERROR CRÍTICO! El Prefab de la bala NO TIENE un componente NetworkObject. Añádelo o no aparecerá en red.");
            }

            // 4. Mandamos una señal síncrona de audio a los clientes
            PlayFireAudioClientRpc();
        }

        [ClientRpc]
        private void PlayFireAudioClientRpc()
        {
            if (m_ShootingAudio != null)
            {
                m_ShootingAudio.clip = m_FireClip;
                m_ShootingAudio.Play();
            }
        }
    }
}