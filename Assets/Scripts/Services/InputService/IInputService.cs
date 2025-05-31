using System;
using UnityEngine;

namespace Services.InputService
{
    public interface IInputService : IDisposable
    {
        event Action<Vector3> OnTap;
        
        Vector3 ScreenToWorld(Vector2 screenPos);
    }
}