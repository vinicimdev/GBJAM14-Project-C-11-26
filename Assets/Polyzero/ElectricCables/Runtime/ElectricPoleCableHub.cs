using UnityEngine;

namespace Game.Spline
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ElectricPoleCableHub : MonoBehaviour
    {
        [Header("Cable Sockets")]
        [SerializeField] private Transform[] sockets;

        [Header("Socket Debug")]
        [SerializeField] private bool showSocketGizmos = true;
        [SerializeField] private bool showSocketLabels = true;
        [SerializeField] [Min(0.01f)] private float socketGizmoSize = 0.12f;
        [SerializeField] private Color socketGizmoColor = new Color(0.15f, 0.75f, 1f, 1f);
        [SerializeField] private Color socketLineColor = new Color(0.15f, 0.75f, 1f, 0.55f);

        public int SocketCount => sockets != null ? sockets.Length : 0;
        public bool ShowSocketGizmos
        {
            get => showSocketGizmos;
            set => showSocketGizmos = value;
        }

        public bool ShowSocketLabels
        {
            get => showSocketLabels;
            set => showSocketLabels = value;
        }

        public Transform GetSocket(int index)
        {
            if (sockets == null || index < 0 || index >= sockets.Length)
            {
                return null;
            }

            return sockets[index];
        }

        public void SetSockets(Transform[] newSockets)
        {
            sockets = newSockets;
        }

        private void OnValidate()
        {
            socketGizmoSize = Mathf.Max(0.01f, socketGizmoSize);
        }

        private void OnDrawGizmos()
        {
            DrawSocketGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawSocketGizmos(true);
        }

        private void DrawSocketGizmos(bool selected)
        {
            if (!showSocketGizmos || sockets == null)
            {
                return;
            }

            Color previousColor = Gizmos.color;
            for (int i = 0; i < sockets.Length; i++)
            {
                Transform socket = sockets[i];
                if (socket == null)
                {
                    continue;
                }

                float size = selected ? socketGizmoSize * 1.35f : socketGizmoSize;
                Gizmos.color = socketGizmoColor;
                Gizmos.DrawSphere(socket.position, size);

                Gizmos.color = socketLineColor;
                Gizmos.DrawLine(transform.position, socket.position);

                DrawSocketDirection(socket, size * 2.25f);
            }

            Gizmos.color = previousColor;
        }

        private void DrawSocketDirection(Transform socket, float length)
        {
            Color previousColor = Gizmos.color;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(socket.position, socket.position + socket.up * length);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(socket.position, socket.position + socket.forward * length);
            Gizmos.color = previousColor;
        }
    }
}
