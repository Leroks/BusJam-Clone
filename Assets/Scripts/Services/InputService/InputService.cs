using System;
using UnityEngine;

public sealed class InputService : IDisposable
{
    public event Action<Vector3> OnTap;

        public Vector3 ScreenToWorld(Vector2 screenPos) =>
            Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));

        public void Tick()
        {
#if UNITY_EDITOR
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                var pos = ScreenToWorld(UnityEngine.Input.mousePosition);
                OnTap?.Invoke(pos);
            }
#else
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch t = UnityEngine.Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    var pos = ScreenToWorld(t.position);
                    OnTap?.Invoke(pos);
                }
            }
#endif
        }

    public void Dispose() { }
}
