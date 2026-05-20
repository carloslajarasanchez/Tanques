using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace Complete
{
    public class GameUIManager : NetworkBehaviour
    {
        public static GameUIManager Instance;

        [Header("UI Elements")]
        public GameObject m_EndGamePanel;           // El contenedor principal
        public TextMeshProUGUI m_MessageText;       // Texto del ganador
        public Button m_RematchButton;              // Botón de Revancha
        public Button m_LobbyButton;                // Botón de salir al Lobby
        public TextMeshProUGUI m_RematchStatusText; // Texto de "El otro jugador quiere revancha"

        [Header("Scene Names")]
        public string m_LobbySceneName = "Lobby";   // Pon aquí el nombre exacto de tu escena del menú/lobby

        // VARIABLES DE RED: Registran si cada jugador quiere revancha (Indexado por su ClientId)
        private NetworkVariable<bool> m_HostWantsRematch = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> m_ClientWantsRematch = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            // Aseguramos que todo empiece limpio y oculto
            if (m_EndGamePanel != null) m_EndGamePanel.SetActive(false);
            if (m_RematchStatusText != null) m_RematchStatusText.text = "";

            // Escuchar cambios en las votaciones de revancha para actualizar los textos locales
            m_HostWantsRematch.OnValueChanged += OnRematchVotesChanged;
            m_ClientWantsRematch.OnValueChanged += OnRematchVotesChanged;

            // Asignar funciones a los botones localmente
            if (m_RematchButton != null) m_RematchButton.onClick.AddListener(PressRematchButton);
            if (m_LobbyButton != null) m_LobbyButton.onClick.AddListener(PressLobbyButton);
        }

        public override void OnNetworkDespawn()
        {
            m_HostWantsRematch.OnValueChanged -= OnRematchVotesChanged;
            m_ClientWantsRematch.OnValueChanged -= OnRematchVotesChanged;
        }

        public void CheckGameOver()
        {
            if (!IsServer) return;

            // Resetear votos de revancha de la partida anterior
            m_HostWantsRematch.Value = false;
            m_ClientWantsRematch.Value = false;

            TankHealth[] allTanks = FindObjectsByType<TankHealth>(FindObjectsSortMode.None);
            TankHealth winner = null;
            int tanksAlive = 0;

            foreach (TankHealth tank in allTanks)
            {
                if (tank.m_CurrentLives.Value > 0)
                {
                    tanksAlive++;
                    winner = tank;
                }
            }

            if (tanksAlive <= 1)
            {
                string message = "¡EMPATE!";
                if (winner != null)
                {
                    message = $"¡EL TANQUE {winner.NetworkObjectId} GANA LA PARTIDA!";
                }

                ShowEndGameMenuClientRpc(message);
            }
        }

        [ClientRpc]
        private void ShowEndGameMenuClientRpc(string winMessage)
        {
            if (m_MessageText != null) m_MessageText.text = winMessage;
            if (m_RematchStatusText != null) m_RematchStatusText.text = "";
            if (m_RematchButton != null) m_RematchButton.interactable = true;

            if (m_EndGamePanel != null) m_EndGamePanel.SetActive(true);
        }

        // ==========================================
        // LÓGICA DEL BOTÓN DE REVANCHA
        // ==========================================

        private void PressRematchButton()
        {
            // Desactivamos nuestro botón para no spamearlo
            m_RematchButton.interactable = false;

            // Avisamos al servidor de que este cliente quiere revancha
            SendRematchVoteServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendRematchVoteServerRpc(ulong clientId)
        {
            // Si el que pulsa es el Host (Id 0) o el Cliente (Id 1...)
            if (clientId == NetworkManager.ServerClientId)
            {
                m_HostWantsRematch.Value = true;
            }
            else
            {
                m_ClientWantsRematch.Value = true;
            }

            // Comprobar si AMBOS han aceptado la revancha
            if (m_HostWantsRematch.Value && m_ClientWantsRematch.Value)
            {
                RestartGame();
            }
        }

        private void OnRematchVotesChanged(bool oldValue, bool newValue)
        {
            // Esta función se ejecuta en todas las pantallas cuando alguien vota
            ulong localId = NetworkManager.Singleton.LocalClientId;

            if (localId == NetworkManager.ServerClientId) // Soy el Host
            {
                // Si el cliente votó que sí, al Host le sale el aviso
                if (m_ClientWantsRematch.Value)
                {
                    m_RematchStatusText.text = "El otro jugador quiere echar la revancha";
                }
            }
            else // Soy el Cliente
            {
                // Si el host votó que sí, al Cliente le sale el aviso
                if (m_HostWantsRematch.Value)
                {
                    m_RematchStatusText.text = "El otro jugador quiere echar la revancha";
                }
            }
        }

        private void RestartGame()
        {
            if (!IsServer) return;

            // Buscamos los tanques y les restauramos las 3 vidas y la salud completa
            TankHealth[] allTanks = FindObjectsByType<TankHealth>(FindObjectsSortMode.None);
            foreach (TankHealth tank in allTanks)
            {
                tank.m_CurrentLives.Value = 3;
                tank.m_CurrentHealth.Value = tank.m_StartingHealth;

                // Forzamos al script del tanque a revivirlo y mandarlo a su spawn original
                // Usamos un truco invocando un método público de reset que implementaremos ahora
                tank.Invoke("Respawn", 0f);
            }

            // Ocultamos el panel en todos los clientes
            HideEndGameMenuClientRpc();
        }

        [ClientRpc]
        private void HideEndGameMenuClientRpc()
        {
            if (m_EndGamePanel != null) m_EndGamePanel.SetActive(false);
        }

        // ==========================================
        // LÓGICA DEL BOTÓN VOLVER AL LOBBY
        // ==========================================

        private void PressLobbyButton()
        {
            // Desconectamos limpiamente de la red según si somos Host o Cliente
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.Shutdown();
                // Al apagar el servidor, cargamos la escena del Lobby de vuelta
                SceneManager.LoadScene(m_LobbySceneName);
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
                // Si eres cliente, te desonectas y vuelves a tu menú local
                SceneManager.LoadScene(m_LobbySceneName);
            }
        }
    }
}