using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        // Solo el servidor decide dónde poner a cada uno
        if (IsServer)
        {
            int index = 0;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                // Obtenemos el objeto del jugador (el tanque)
                GameObject playerObj = client.PlayerObject.gameObject;

                // Lo movemos a un punto de spawn
                Transform spawnPoint = spawnPoints[index % spawnPoints.Length];
                playerObj.transform.position = spawnPoint.position;
                playerObj.transform.rotation = spawnPoint.rotation;

                index++;
            }
        }
    }
}