using System;
using UnityEngine;

namespace Services.InputService
{
    public interface IInputService : IDisposable
    {
        /// <summary> Fires each time the player taps/clicks the screen. </summary>
        event Action<Vector3> OnTap;   // world-space position

        /// <summary> Convert screen point to world pos using main camera. </summary>
        Vector3 ScreenToWorld(Vector2 screenPos);
    }
}