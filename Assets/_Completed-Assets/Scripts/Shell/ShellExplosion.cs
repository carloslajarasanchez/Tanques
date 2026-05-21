using Unity.Netcode;
using UnityEngine;

namespace Complete
{
    public class ShellExplosion : MonoBehaviour
    {
        public LayerMask m_TankMask;                        // Capa (Layer) donde están los tanques
        public ParticleSystem m_ExplosionParticles;         // Partículas de la explosión
        public AudioSource m_ExplosionAudio;                // Sonido de la explosión
        public float m_MaxDamage = 100f;                    // Daño máximo en el centro del impacto
        public float m_ExplosionForce = 0f;                 // Forzado a 0 para que no empuje los tanques al explotar
        public float m_MaxLifeTime = 2f;                    // Tiempo límite antes de auto-destruirse
        public float m_ExplosionRadius = 5f;                // Radio de la explosión

        private bool m_HasExploded = false;                 // Evita que las partículas se dupliquen

        private void Start()
        {
            // Solo el servidor gestiona el tiempo de vida físico del objeto en red
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                Destroy(gameObject, m_MaxLifeTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // ====================================================================
            // FILTRO DE RED CRÍTICO: Solo el Servidor procesa la lógica del impacto
            // ====================================================================
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            // El servidor busca qué tanques están dentro del radio de daño
            Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

            for (int i = 0; i < colliders.Length; i++)
            {
                Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();

                // Si no hay Rigidbody, no es un tanque válido
                if (targetRigidbody == null) continue;

                // Buscamos el componente de salud del tanque afectado
                TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();

                if (targetHealth == null) continue;

                // Calculamos el daño exacto basado en la distancia a la explosión
                float damage = CalculateDamage(targetRigidbody.position);

                // El servidor aplica el daño a la NetworkVariable de TankHealth
                targetHealth.TakeDamage(damage);
            }

            // El servidor destruye el objeto. Al hacerlo, se invocará OnDestroy() 
            // tanto en el Servidor como en todos los Clientes conectados de forma síncrona.
            Destroy(gameObject);
        }

        // ====================================================================
        // ESTA ES LA CLAVE: Se ejecuta en TODOS los clientes cuando el objeto muere
        // ====================================================================
        private void OnDestroy()
        {
            // Seguridad para evitar que se ejecute dos veces si se llamara manualmente
            if (m_HasExploded) return;
            m_HasExploded = true;

            // Cada pantalla (Host y Clientes) reproduce sus propios efectos locales de forma síncrona
            if (m_ExplosionParticles != null)
            {
                // Desacoplamos las partículas para que no mueran junto con la bala
                m_ExplosionParticles.transform.parent = null;

                // Activamos los efectos visuales y sonoros
                m_ExplosionParticles.Play();

                if (m_ExplosionAudio != null)
                {
                    m_ExplosionAudio.Play();
                }

                // Destruimos el contenedor de las partículas de forma local tras expirar su duración
                Destroy(m_ExplosionParticles.gameObject, m_ExplosionParticles.main.duration);
            }
        }

        private float CalculateDamage(Vector3 targetPosition)
        {
            Vector3 explosionToTarget = targetPosition - transform.position;
            float explosionDistance = explosionToTarget.magnitude;
            float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;
            float damage = relativeDistance * m_MaxDamage;
            damage = Mathf.Max(0f, damage);
            return damage;
        }
    }
}