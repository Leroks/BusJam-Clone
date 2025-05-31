using System;

namespace Services.Timer
{
    public interface ITimerService : IDisposable
    {
        event Action<float> OnTick;
        event Action OnFinished;

        void Start(float duration);
        void Stop();
        void Tick(float deltaTime);
        bool IsRunning { get; }
        float Remaining { get; }
    }
}
