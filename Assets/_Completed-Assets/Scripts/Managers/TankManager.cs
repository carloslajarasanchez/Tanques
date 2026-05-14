using System;
using UnityEngine;

namespace Complete
{
    [Serializable]
    public class TankManager
    {
        // Simplificamos esta clase ya que Netcode gestiona la mayoría de las cosas
        [HideInInspector] public Color m_PlayerColor;
        [HideInInspector] public int m_PlayerNumber;
        [HideInInspector] public GameObject m_Instance;
        [HideInInspector] public int m_Wins;

        public void Setup()
        {
            // Ya no configuramos el color aquí, lo hace el script TankMovement por red
        }

        public void DisableControl()
        {
            // Buscamos los scripts en la instancia y los desactivamos
            m_Instance.GetComponent<TankMovement>().enabled = false;
            m_Instance.GetComponent<TankShooting>().enabled = false;
            m_Instance.GetComponentInChildren<Canvas>().gameObject.SetActive(false);
        }

        public void EnableControl()
        {
            // Solo habilitamos si somos el dueño (esto se refuerza en el OnNetworkSpawn del tanque)
            m_Instance.GetComponent<TankMovement>().enabled = true;
            m_Instance.GetComponent<TankShooting>().enabled = true;
            m_Instance.GetComponentInChildren<Canvas>().gameObject.SetActive(true);
        }

        public void Reset()
        {
            // El reset ahora es más simple, la posición la debería manejar un Spawner de red
            m_Instance.SetActive(false);
            m_Instance.SetActive(true);
        }
    }
}