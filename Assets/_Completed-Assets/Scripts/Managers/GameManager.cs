using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Complete
{
    public class GameManager : NetworkBehaviour
    {
        public int m_NumRoundsToWin = 5;
        public float m_StartDelay = 3f;
        public float m_EndDelay = 3f;
        public CameraControl m_CameraControl;
        public Text m_MessageText;

        private int m_RoundNumber;
        private WaitForSeconds m_StartWait;
        private WaitForSeconds m_EndWait;

        public override void OnNetworkSpawn()
        {
            m_StartWait = new WaitForSeconds(m_StartDelay);
            m_EndWait = new WaitForSeconds(m_EndDelay);

            if (IsServer)
            {
                StartCoroutine(GameLoop());
            }
        }

        private IEnumerator GameLoop()
        {
            yield return StartCoroutine(RoundStarting());
            yield return StartCoroutine(RoundPlaying());
            yield return StartCoroutine(RoundEnding());

            // Por ahora solo una ronda para probar, luego podemos repetir el loop
            m_MessageText.text = "FIN DE PRUEBA";
        }

        private IEnumerator RoundStarting()
        {
            // Notificamos a los clientes mediante un cambio de texto (Idealmente usarías una NetworkVariable)
            UpdateMessageClientRpc("RONDA " + m_RoundNumber);
            yield return m_StartWait;
        }

        private IEnumerator RoundPlaying()
        {
            UpdateMessageClientRpc("");
            // Aquí la lógica de esperar a que solo quede un tanque vivo
            while (!OneTankLeft())
            {
                yield return null;
            }
        }

        private IEnumerator RoundEnding()
        {
            UpdateMessageClientRpc("RONDA FINALIZADA");
            yield return m_EndWait;
        }

        [ClientRpc]
        private void UpdateMessageClientRpc(string message)
        {
            m_MessageText.text = message;
        }

        private bool OneTankLeft()
        {
            int numTanksLeft = 0;
            GameObject[] tanks = GameObject.FindGameObjectsWithTag("Player");
            foreach (var tank in tanks)
            {
                if (tank.activeSelf) numTanksLeft++;
            }
            return numTanksLeft <= 1;
        }
    }
}