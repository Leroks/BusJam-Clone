using System;

public class TimerService : IDisposable
{
    public event Action<float> OnTick;
        public event Action OnFinished;

        public bool IsRunning { get; private set; }
        public float Remaining { get; private set; }

        public void Start(float duration)
        {
            Remaining = duration;
            IsRunning = true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;

            Remaining -= deltaTime;
            if (Remaining <= 0f)
            {
                Remaining = 0f;
                OnTick?.Invoke(Remaining);
                OnFinished?.Invoke();
                IsRunning = false;
            }
            else
            {
                OnTick?.Invoke(Remaining);
            }
        }

        public void Dispose()
        {
            OnFinished = null;
            OnTick = null;
        }
}