using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Complete
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NetworkObject))]
    public class MiniTankAI : NetworkBehaviour
    {
        [Header("Configuración de Seguimiento")]
        public float m_FollowDistance = 3f;         // Distancia ideal a la que se queda del dueño
        public float m_MaxEnclosureDistance = 8f;   // Distancia máxima permitida antes de ignorar enemigos y volver con el dueño

        [Header("Configuración de Combate")]
        public float m_DetectionRadius = 10f;       // Radio para detectar tanques enemigos
        public LayerMask m_TankMask;                // Capa de los tanques
        public GameObject m_BulletPrefab;           // Prefab de la bala del minitanque
        public Transform m_FireTransform;           // Punto de salida de la bala
        public float m_LaunchForce = 25f;           // Fuerza del disparo
        public float m_FireRate = 1.5f;             // Segundos entre disparos

        [Header("Efectos")]
        public GameObject m_ExplosionPrefab;        // Explosión al morir de un tiro

        // Variable de Red Sincronizada para que los clientes sepan quién es el dueño
        private NetworkVariable<ulong> m_OwnerClientId = new NetworkVariable<ulong>();

        private NavMeshAgent m_Agent;
        private Transform m_OwnerTransform;
        private Transform m_TargetEnemy;
        private float m_NextFireTime;
        private bool m_IsDead = false;

        private void Awake()
        {
            m_Agent = GetComponent<NavMeshAgent>();
        }

        // ====================================================================
        // INICIALIZACIÓN DE RED (Llamado por el Servidor al spawnearlo)
        // ====================================================================
        public void SetOwner(ulong ownerId, Transform ownerTransform)
        {
            if (!IsServer) return;
            m_OwnerClientId.Value = ownerId;
            m_OwnerTransform = ownerTransform;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer)
            {
                StartCoroutine(FindOwnerTransformOnClient());
            }
        }

        private IEnumerator FindOwnerTransformOnClient()
        {
            yield return new WaitForSeconds(0.5f);

            TankHealth[] tanques = FindObjectsOfType<TankHealth>();
            foreach (var tanque in tanques)
            {
                if (tanque.OwnerClientId == m_OwnerClientId.Value)
                {
                    m_OwnerTransform = tanque.transform;
                    break;
                }
            }
        }

        // ====================================================================
        // BUCLE DE INTELIGENCIA ARTIFICIAL (Solo se procesa en el Servidor)
        // ====================================================================
        private void Update()
        {
            if (!IsServer || m_IsDead || m_OwnerTransform == null) return;

            // 1. Calculamos la distancia actual al dueño
            float distanceToOwner = Vector3.Distance(transform.position, m_OwnerTransform.position);

            // 2. REGLA DE PRIORIDAD MÁXIMA: Si el dueño se ha ido muy lejos, ignoramos el combate
            if (distanceToOwner > m_MaxEnclosureDistance)
            {
                m_Agent.SetDestination(m_OwnerTransform.position);
                return; // Saltamos el resto del código para que no busque enemigos ni dispare
            }

            // 3. Si el dueño está a una distancia aceptable, procesamos el combate normal
            BuscarEnemigoCercano();

            if (m_TargetEnemy != null)
            {
                // ESTADO: Atacar enemigo (Se detiene y apunta)
                m_Agent.SetDestination(transform.position);
                RotarHacia(m_TargetEnemy.position);

                if (Time.time >= m_NextFireTime)
                {
                    Fire();
                }
            }
            else
            {
                // ESTADO: Seguir al dueño si no hay amenazas cerca
                if (distanceToOwner > m_FollowDistance)
                {
                    m_Agent.SetDestination(m_OwnerTransform.position);
                }
                else
                {
                    m_Agent.SetDestination(transform.position); // Se frena si está al lado
                }
            }
        }

        private void BuscarEnemigoCercano()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, m_DetectionRadius, m_TankMask);
            float closestDistance = Mathf.Infinity;
            Transform target = null;

            foreach (var col in colliders)
            {
                TankHealth enemyHealth = col.GetComponent<TankHealth>();

                if (enemyHealth != null && enemyHealth.OwnerClientId != m_OwnerClientId.Value)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        target = col.transform;
                    }
                }
            }

            m_TargetEnemy = target;
        }

        private void RotarHacia(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }

        private void Fire()
        {
            if (m_TargetEnemy == null) return;

            m_NextFireTime = Time.time + m_FireRate;

            GameObject bulletInstance = Instantiate(m_BulletPrefab, m_FireTransform.position, m_FireTransform.rotation);

            Collider bulletCollider = bulletInstance.GetComponent<Collider>();
            Collider miniTankCollider = GetComponent<Collider>();
            if (bulletCollider != null && miniTankCollider != null) Physics.IgnoreCollision(bulletCollider, miniTankCollider);

            if (bulletCollider != null && m_OwnerTransform != null)
            {
                Collider ownerCollider = m_OwnerTransform.GetComponent<Collider>() ?? m_OwnerTransform.GetComponentInChildren<Collider>();
                if (ownerCollider != null) Physics.IgnoreCollision(bulletCollider, ownerCollider);
            }

            Rigidbody rb = bulletInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;

                Vector3 targetPos = m_TargetEnemy.position;
                Vector3 firePos = m_FireTransform.position;
                Vector3 direction = targetPos - firePos;

                float distance = new Vector3(direction.x, 0, direction.z).magnitude;
                float yOffset = direction.y;
                float gravity = Mathf.Abs(Physics.gravity.y);
                float angleInRadians = 45f * Mathf.Deg2Rad;

                float velocidadNecesaria = Mathf.Sqrt((gravity * distance * distance) / (2 * (distance * Mathf.Tan(angleInRadians) - yOffset))) / Mathf.Cos(angleInRadians);

                if (!float.IsNaN(velocidadNecesaria) && !float.IsInfinity(velocidadNecesaria))
                {
                    Vector3 horizontalDirection = new Vector3(direction.x, 0, direction.z).normalized;
                    Vector3 launchVelocity = horizontalDirection * Mathf.Cos(angleInRadians) * velocidadNecesaria;
                    launchVelocity.y = Mathf.Sin(angleInRadians) * velocidadNecesaria;

                    rb.AddForce(launchVelocity, ForceMode.VelocityChange);
                }
                else
                {
                    Vector3 fireDirection = (direction.normalized + Vector3.up * 0.2f).normalized;
                    rb.AddForce(fireDirection * m_LaunchForce, ForceMode.VelocityChange);
                }
            }

            NetworkObject netObj = bulletInstance.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || m_IsDead) return;

            if (other.CompareTag("Bullet") || other.GetComponent<ShellExplosion>() != null)
            {
                Morir();
            }
        }

        private void Morir()
        {
            m_IsDead = true;
            PlayExplosionClientRpc(transform.position);
            GetComponent<NetworkObject>().Despawn();
        }

        [ClientRpc]
        private void PlayExplosionClientRpc(Vector3 position)
        {
            if (m_ExplosionPrefab != null)
            {
                GameObject exp = Instantiate(m_ExplosionPrefab, position, Quaternion.identity);
                Destroy(exp, 2f);
            }
        }
    }
}