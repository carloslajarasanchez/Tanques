using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Complete
{
    public class TankLifeUI : MonoBehaviour
    {
        [Header("Sprites de los Tanques")]
        public Sprite m_BlueTankSprite;             // El dibujo del tanque Azul (Host)
        public Sprite m_RedTankSprite;              // El dibujo del tanque Rojo (Cliente)

        [Header("UI Izquierda (Siempre Vidas del Host)")]
        public Image[] m_LeftLifeImages;            // Las 3 imágenes fijas de la izquierda

        [Header("UI Derecha (Siempre Vidas del Cliente)")]
        public Image[] m_RightLifeImages;           // Las 3 imágenes fijas de la derecha

        private bool m_SpritesAsignados = false;

        private void Start()
        {
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
            // Asignamos los sprites fijos en cuanto la red esté lista
            if (!m_SpritesAsignados && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                AsignarSpritesFijos();
            }

            // Refrescamos el estado de las imágenes (encendido/apagado) leyendo las NetworkVariables de los tanques
            ActualizarVidasVisibles();
        }

        private void OnPlayerConnected(ulong clientId)
        {
            AsignarSpritesFijos();
        }

        private void AsignarSpritesFijos()
        {
            // FIJO: Izquierda siempre es Azul (Host), Derecha siempre es Rojo (Cliente)
            EstablecerSprites(m_LeftLifeImages, m_BlueTankSprite);
            EstablecerSprites(m_RightLifeImages, m_RedTankSprite);

            m_SpritesAsignados = true;
        }

        private void EstablecerSprites(Image[] grupoImagenes, Sprite spriteDestino)
        {
            foreach (Image img in grupoImagenes)
            {
                if (img != null && spriteDestino != null)
                {
                    img.sprite = spriteDestino;
                }
            }
        }

        private void ActualizarVidasVisibles()
        {
            // Buscamos los tanques en la escena para leer sus vidas sincronizadas
            TankHealth[] todosLosTanques = FindObjectsOfType<TankHealth>();

            foreach (TankHealth tanque in todosLosTanques)
            {
                // Host (ID 0) -> Va siempre a la izquierda
                if (tanque.OwnerClientId == 0)
                {
                    RefrescarIconos(m_LeftLifeImages, tanque.m_CurrentLives.Value);
                }
                // Cliente (Cualquier otro ID) -> Va siempre a la derecha
                else
                {
                    RefrescarIconos(m_RightLifeImages, tanque.m_CurrentLives.Value);
                }
            }
        }

        private void RefrescarIconos(Image[] imagenes, int vidasActuales)
        {
            for (int i = 0; i < imagenes.Length; i++)
            {
                if (imagenes[i] != null)
                {
                    // Si el índice es menor que las vidas actuales se muestra, si no se oculta
                    imagenes[i].enabled = (i < vidasActuales);
                }
            }
        }
    }
}