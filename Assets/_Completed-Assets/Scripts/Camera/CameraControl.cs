using UnityEngine;
using System.Collections.Generic;

namespace Complete
{
    public class CameraControl : MonoBehaviour
    {
        public float m_DampTime = 0.2f;
        public float m_ScreenEdgeBuffer = 4f;
        public float m_MinSize = 6.5f;

        private Camera m_Camera;
        private float m_ZoomSpeed;
        private Vector3 m_MoveVelocity;
        private Vector3 m_DesiredPosition;
        private List<Transform> m_Targets = new List<Transform>();

        private void Awake()
        {
            m_Camera = GetComponentInChildren<Camera>();
        }

        private void FixedUpdate()
        {
            FindTargets(); // Busca tanques en la escena
            if (m_Targets.Count == 0) return;

            Move();
            Zoom();
        }

        private void FindTargets()
        {
            m_Targets.Clear();
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players)
            {
                if (player.activeSelf) m_Targets.Add(player.transform);
            }
        }

        private void Move()
        {
            FindAveragePosition();
            transform.position = Vector3.SmoothDamp(transform.position, m_DesiredPosition, ref m_MoveVelocity, m_DampTime);
        }

        private void FindAveragePosition()
        {
            Vector3 averagePos = new Vector3();
            int numTargets = 0;

            for (int i = 0; i < m_Targets.Count; i++)
            {
                averagePos += m_Targets[i].position;
                numTargets++;
            }

            if (numTargets > 0) averagePos /= numTargets;
            averagePos.y = transform.position.y;
            m_DesiredPosition = averagePos;
        }

        private void Zoom()
        {
            float requiredSize = FindRequiredSize();
            m_Camera.orthographicSize = Mathf.SmoothDamp(m_Camera.orthographicSize, requiredSize, ref m_ZoomSpeed, m_DampTime);
        }

        private float FindRequiredSize()
        {
            Vector3 desiredLocalPos = transform.InverseTransformPoint(m_DesiredPosition);
            float size = 0f;

            for (int i = 0; i < m_Targets.Count; i++)
            {
                Vector3 targetLocalPos = transform.InverseTransformPoint(m_Targets[i].position);
                Vector3 desiredPosToTarget = targetLocalPos - desiredLocalPos;
                size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.y));
                size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.x) / m_Camera.aspect);
            }

            size += m_ScreenEdgeBuffer;
            return Mathf.Max(size, m_MinSize);
        }
    }
}