using Unity.Netcode;
using UnityEngine;

namespace Complete
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collider))]
    public abstract class BasePowerUp : NetworkBehaviour, IPowerUpEffect
    {
        [Header("Configuración Visual Base")]
        public float m_RotationSpeed = 50f;
        public float m_FloatSpeed = 0.5f;
        public float m_FloatAmplitude = 0.2f;

        private Vector3 m_StartPos;

        private void Awake()
        {
            // Forzamos que el Collider sea de tipo Trigger de forma automática
            GetComponent<Collider>().isTrigger = true;
        }

        private void Start()
        {
            m_StartPos = transform.position;
        }

        private void Update()
        {
            // Movimiento visual síncrono (se ejecuta localmente en cada pantalla)
            transform.Rotate(Vector3.up, m_RotationSpeed * Time.deltaTime, Space.World);
            Vector3 tempPos = m_StartPos;
            tempPos.y += Mathf.Sin(Time.time * Mathf.PI * m_FloatSpeed) * m_FloatAmplitude;
            transform.position = tempPos;
        }

        private void OnTriggerEnter(Collider other)
        {
            // FILTRO DE RED: El servidor es el único que decide si se recoge el objeto
            if (!IsServer) return;

            if (other.CompareTag("Player") || other.GetComponent<TankHealth>() != null)
            {
                // Llamamos al método abstracto que heredarán los hijos
                bool success = ApplyEffect(other.gameObject);

                if (success)
                {
                    // Despegar limpiamente de la red si el efecto fue aceptado por el tanque
                    GetComponent<NetworkObject>().Despawn();
                }
            }
        }

        // Al ser abstracto, cada hijo tendrá que rellenar este método obligatoriamente
        public abstract bool ApplyEffect(GameObject target);
    }
}