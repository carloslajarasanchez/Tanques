using Unity.Netcode;
using UnityEngine;

public class TankAppearance : NetworkBehaviour
{
    // Variable que sincroniza el color (azul por defecto)
    private NetworkVariable<Color> netColor = new NetworkVariable<Color>(Color.blue);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Si soy el Host (Server + Client Id 0), azul. Si no, rojo.
            netColor.Value = OwnerClientId == 0 ? Color.blue : Color.red;
        }

        // Aplicamos el color inicial y nos suscribimos a cambios
        ApplyColor(netColor.Value);
        netColor.OnValueChanged += (oldCol, newCol) => ApplyColor(newCol);
    }

    private void ApplyColor(Color col)
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers) r.material.color = col;
    }
}