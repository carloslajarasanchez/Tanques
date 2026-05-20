using Unity.Netcode;
using UnityEngine;

namespace Complete
{
    public class ShellExplosion : NetworkBehaviour
    {
        public LayerMask m_TankMask;
        public ParticleSystem m_ExplosionParticles;
        public AudioSource m_ExplosionAudio;
        public float m_MaxDamage = 100f;
        public float m_ExplosionForce = 1000f;
        public float m_MaxLifeTime = 2f;
        public float m_ExplosionRadius = 5f;

        public override void OnNetworkSpawn()
        {
            // Solo el servidor controla el tiempo de vida de la bala
            // Si no choca con nada, se destruye tras m_MaxLifeTime
            if (IsServer)
            {
                Invoke(nameof(DestroyShell), m_MaxLifeTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // ¡CRÍTICO! Solo el servidor procesa las colisiones y el daño
            if (!IsServer) return;

            Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

            for (int i = 0; i < colliders.Length; i++)
            {
                Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();
                if (!targetRigidbody) continue;

                targetRigidbody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);
                TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();

                if (!targetHealth) continue;

                float damage = CalculateDamage(targetRigidbody.position);
                targetHealth.TakeDamage(damage);
            }

            // Avisamos a todos los clientes (y al propio host) que muestren la explosión
            ExplodeClientRpc();

            // Destruimos la bala de forma segura en la red
            DestroyShell();
        }

        [ClientRpc]
        private void ExplodeClientRpc()
        {
            // Desvinculamos las partículas de la bala para que no se borren junto con ella
            m_ExplosionParticles.transform.parent = null;
            m_ExplosionParticles.Play();
            m_ExplosionAudio.Play();

            // Destruimos el objeto de las partículas cuando termine su animación localmente
            ParticleSystem.MainModule mainModule = m_ExplosionParticles.main;
            Destroy(m_ExplosionParticles.gameObject, mainModule.duration);
        }

        private void DestroyShell()
        {
            // Cancelamos el Invoke por si chocó antes de agotar su tiempo de vida
            CancelInvoke(nameof(DestroyShell));

            // Despawneamos el objeto de la red (el 'true' indica que también destruye el GameObject)
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }

        private float CalculateDamage(Vector3 targetPosition)
        {
            Vector3 explosionToTarget = targetPosition - transform.position;
            float explosionDistance = explosionToTarget.magnitude;
            float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;
            float damage = relativeDistance * m_MaxDamage;
            return Mathf.Max(0f, damage);
        }
    }
}