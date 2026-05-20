using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode.Samples
{
    [AddComponentMenu("Netcode/Client Network Transform")]
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false; // Esto le da permiso al Cliente para mover su propio tanque
        }
    }
}