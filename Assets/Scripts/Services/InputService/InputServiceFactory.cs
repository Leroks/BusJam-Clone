using UnityEngine;

namespace Services.InputService
{
    public static class InputServiceFactory
    {
        public static IInputService Create()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return new MouseInputService();
#else
            return new TouchInputService();
#endif
        }
    }
}