using UnityEngine;

namespace Services.InputService
{
    public static class InputServiceFactory
    {
        public static IInputService Create()
        {
#if UNITY_EDITOR
            return new MouseInputService();
#else
            return new TouchInputService();
#endif
        }
    }
}