using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkButtons : MonoBehaviour
{
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;

    private void Awake()
    {
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            HideButtons();
        });

        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            HideButtons();
        });
    }

    private void HideButtons()
    {
        hostBtn.gameObject.SetActive(false);
        clientBtn.gameObject.SetActive(false);
    }
}