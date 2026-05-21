using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Complete
{
    public class PlayerUIManager : MonoBehaviour
    {
        [Header("Contenedores de la UI (Anclajes)")]
        public RectTransform m_LeftContainer;       // Objeto UI anclado arriba a la izquierda
        public RectTransform m_RightContainer;      // Objeto UI anclado arriba a la derecha

        [Header("Iconos de Vida del Host (Azul)")]
        public Image[] m_HostLifeImages;            // Arrastra aquí las 3 imágenes del tanque azul

        [Header("Iconos de Vida del Cliente (Rojo)")]
        public Image[] m_ClientLifeImages;          // Arrastra aquí las 3 imágenes del tanque rojo

        private bool m_LayoutConfigured = false;

        private void Start()
        {
            // Nos suscribimos al callback de conexión para ordenar la pantalla en cuanto entremos a la partida
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
            }
        }

        private void Update()
        {
            // Failsafe: Si por temas de carga el NetworkManager no estaba listo en el Start,
            // forzamos la ordenación en cuanto el cliente local esté activo en la red.
            if (!m_LayoutConfigured && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                ConfigureUILayout();
            }
        }

        private void OnPlayerConnected(ulong clientId)
        {
            ConfigureUILayout();
        }

        private void ConfigureUILayout()
        {
            if (NetworkManager.Singleton == null || m_LeftContainer == null || m_RightContainer == null) return;

            // ====================================================================
            // INVERSIÓN DINÁMICA DE LA UI SEGÚN LA PERSPECTIVA
            // ====================================================================
            if (NetworkManager.Singleton.IsServer)
            {
                // PERSPECTIVA DEL HOST:
                // El Host (Azul) se ve a sí mismo a la izquierda y al rival (Rojo) a la derecha.
                SetImagesToContainer(m_HostLifeImages, m_LeftContainer);
                SetImagesToContainer(m_ClientLifeImages, m_RightContainer);
            }
            else
            {
                // PERSPECTIVA DEL CLIENTE:
                // El Cliente (Rojo) se ve a sí mismo a la izquierda y al rival (Azul) a la derecha.
                SetImagesToContainer(m_ClientLifeImages, m_LeftContainer);
                SetImagesToContainer(m_HostLifeImages, m_RightContainer);
            }

            m_LayoutConfigured = true;
        }

        private void SetImagesToContainer(Image[] images, RectTransform container)
        {
            // Re-emparentamos las imágenes dentro del contenedor correspondiente del Canvas
            // para que adopten sus posiciones y alineaciones de forma automática.
            foreach (Image img in images)
            {
                if (img != null)
                {
                    img.transform.SetParent(container, false);
                }
            }
        }

        // ====================================================================
        // ACTUALIZACIÓN DE ICONOS (Llamados por la NetworkVariable de TankHealth)
        // ====================================================================

        public void UpdateHostLivesVisuals(int currentLives)
        {
            for (int i = 0; i < m_HostLifeImages.Length; i++)
            {
                if (m_HostLifeImages[i] != null)
                {
                    // Si el índice es menor que las vidas actuales se queda encendido, si no, se oculta.
                    m_HostLifeImages[i].enabled = (i < currentLives);
                }
            }
        }

        public void UpdateClientLivesVisuals(int currentLives)
        {
            for (int i = 0; i < m_ClientLifeImages.Length; i++)
            {
                if (m_ClientLifeImages[i] != null)
                {
                    m_ClientLifeImages[i].enabled = (i < currentLives);
                }
            }
        }
    }
}