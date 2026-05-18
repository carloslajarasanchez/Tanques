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
        private NetworkVariable<Color> m_NetColor = new NetworkVariable<Color>(Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn()
        {
            m_OriginalPitch = m_MovementAudio.pitch;

            if (IsServer)
            {
                m_NetColor.Value = (OwnerClientId == 0) ? Color.blue : Color.red;
            }

            ApplyColor(m_NetColor.Value);
            m_NetColor.OnValueChanged += (oldV, newV) => ApplyColor(newV);

            // IMPORTANTE: Solo el dueño mueve su tanque
            // Si usas NetworkTransform normal, el cliente no tiene autoridad "física"
            // Para este tutorial, desactivamos kinematic en el dueño para que las teclas funcionen
            m_Rigidbody.isKinematic = !IsOwner;
        }

        private void ApplyColor(Color c)
        {
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].material.color = c;
        }

        private void Update()
        {
            if (!IsOwner) return;

            m_MovementInputValue = Input.GetAxis("Vertical");
            m_TurnInputValue = Input.GetAxis("Horizontal");
            EngineAudio();
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

        private void EngineAudio() { /* Tu lógica de audio original */ }
    }
}