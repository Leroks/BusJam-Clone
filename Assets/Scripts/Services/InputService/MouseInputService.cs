using UnityEngine;

namespace Services.InputService
{
    public sealed class MouseInputService : IInputService
    {
        public event System.Action<Vector3> OnTap;

        public Vector3 ScreenToWorld(Vector2 screenPos) =>
            Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));

        public void Tick()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                var pos = ScreenToWorld(UnityEngine.Input.mousePosition);
                OnTap?.Invoke(pos);
            }
        }

        public void Dispose() { }
    }
}