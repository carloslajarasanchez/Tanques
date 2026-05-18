using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Complete
{
    public class TankShooting : NetworkBehaviour
    {
        public Rigidbody m_Shell;
        public Transform m_FireTransform;
        public Slider m_AimSlider;
        public AudioSource m_ShootingAudio;
        public AudioClip m_ChargingClip;
        public AudioClip m_FireClip;
        public float m_MinLaunchForce = 15f;
        public float m_MaxLaunchForce = 30f;
        public float m_MaxChargeTime = 0.75f;

        private string m_FireButton = "Fire1";
        private float m_CurrentLaunchForce;
        private float m_ChargeSpeed;
        private bool m_Fired;

        private void OnEnable()
        {
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_AimSlider.value = m_MinLaunchForce;
        }

        private void Start()
        {
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }

        private void Update()
        {
            if (!IsOwner) return;

            m_AimSlider.value = m_MinLaunchForce;

            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                m_CurrentLaunchForce = m_MaxLaunchForce;
                FireServerRpc(m_CurrentLaunchForce);
            }
            else if (Input.GetButtonDown(m_FireButton))
            {
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;
                m_ShootingAudio.clip = m_ChargingClip;
                m_ShootingAudio.Play();
            }
            else if (Input.GetButton(m_FireButton) && !m_Fired)
            {
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                m_AimSlider.value = m_CurrentLaunchForce;
            }
            else if (Input.GetButtonUp(m_FireButton) && !m_Fired)
            {
                FireServerRpc(m_CurrentLaunchForce);
            }
        }

        [ServerRpc]
        private void FireServerRpc(float launchForce)
        {
            if (m_Shell == null) { Debug.LogError("Falta el Prefab de la bala en TankShooting"); return; }

            m_Fired = true;
            Rigidbody shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation);
            shellInstance.velocity = launchForce * m_FireTransform.forward;

            // IMPORTANTE: La bala debe tener NetworkObject y estar en la lista de NetworkPrefabs
            if (shellInstance.GetComponent<NetworkObject>() != null)
                shellInstance.GetComponent<NetworkObject>().Spawn();

            FireClientRpc();
        }

        [ClientRpc]
        private void FireClientRpc()
        {
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();
            m_CurrentLaunchForce = m_MinLaunchForce;
        }
    }
}