using UnityEngine;

namespace Services.InputService
{
    public sealed class TouchInputService : IInputService
    {
        public event System.Action<Vector3> OnTap;

        public Vector3 ScreenToWorld(Vector2 screenPos) =>
            Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));

        public void Tick()
        {
            if (UnityEngine.Input.touchCount == 0) return;

            Touch t = UnityEngine.Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                var pos = ScreenToWorld(t.position);
                OnTap?.Invoke(pos);
            }
        }

        public void Dispose() { }
    }
}
