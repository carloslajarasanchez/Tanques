using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("UI Buttons")]
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private Button startButton; // Solo visible para el Host

    private void Awake()
    {
        // Al empezar, el botón de "Empezar Partida" está oculto
        startButton.gameObject.SetActive(false);

        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            OnConnectionSuccess();
        });

        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            OnConnectionSuccess();
        });

        startButton.onClick.AddListener(() => {
            // Solo el Servidor/Host puede cambiar de escena para todos
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene("_Complete-Game", LoadSceneMode.Single);
            }
        });
    }

    private void OnConnectionSuccess()
    {
        hostBtn.gameObject.SetActive(false);
        clientBtn.gameObject.SetActive(false);

        // Si soy el Host, muestro el botón para iniciar la partida
        if (NetworkManager.Singleton.IsHost)
        {
            startButton.gameObject.SetActive(true);
        }
    }
}