using Unity.Netcode;
using UnityEngine;

namespace Complete
{
    public class TankMovement : NetworkBehaviour
    {
        public float m_Speed = 12f;
        public float m_TurnSpeed = 180f;
        public AudioSource m_MovementAudio;
        public AudioClip m_EngineIdling;
        public AudioClip m_EngineDriving;
        public float m_PitchRange = 0.2f;

        private Rigidbody m_Rigidbody;
        private float m_MovementInputValue;
        private float m_TurnInputValue;
        private float m_OriginalPitch;

        // Variable de red para sincronizar el color
        private NetworkVariable<Color> m_NetColor = new NetworkVariable<Color>();

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn()
        {
            m_OriginalPitch = m_MovementAudio.pitch;

            if (IsServer)
            {
                // Host (ID 0) Azul, Clientes Rojo
                m_NetColor.Value = OwnerClientId == 0 ? Color.blue : Color.red;
            }

            // Aplicar color y suscribirse a cambios
            ApplyColor(m_NetColor.Value);
            m_NetColor.OnValueChanged += (oldVal, newVal) => ApplyColor(newVal);

            if (IsOwner)
            {
                m_Rigidbody.isKinematic = false;
            }
            else
            {
                m_Rigidbody.isKinematic = true;
                enabled = false; // Desactiva el Update para no-dueños
            }
        }

        private void ApplyColor(Color color)
        {
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers) r.material.color = color;
        }

        private void Update()
        {
            if (!IsOwner) return;

            m_MovementInputValue = Input.GetAxis("Vertical");
            m_TurnInputValue = Input.GetAxis("Horizontal");

            EngineAudio();
        }

        private void EngineAudio()
        {
            if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f)
            {
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else
            {
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;
            Move();
            Turn();
        }

        private void Move()
        {
            Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;
            m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
        }

        private void Turn()
        {
            float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }
    }
}