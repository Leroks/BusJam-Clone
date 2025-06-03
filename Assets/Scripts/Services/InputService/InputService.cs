using System;
using UnityEngine;

public sealed class InputService : IDisposable
{
    public event Action<Vector3> OnTapWorld;
    public event Action<Vector2> OnTapScreen;
    public event Action<Passenger> OnPassengerTap;
    public event Action<Bus> OnBusTap;
    public event Action OnEmptySpaceTap;

    private Vector3 ScreenToWorldNearPlane(Vector2 screenPos)
    {
        if (Camera.main == null) return Vector3.zero;
        return Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));
    }

    public void Tick()
    {
        Vector2 inputPosition = Vector2.zero;
        bool inputDown = false;
        
        if (Input.GetMouseButtonDown(0))
        {
            inputPosition = Input.mousePosition;
            inputDown = true;
        }

        if (inputDown)
        {
            OnTapScreen?.Invoke(inputPosition);
            OnTapWorld?.Invoke(ScreenToWorldNearPlane(inputPosition));

                Ray ray = Camera.main.ScreenPointToRay(inputPosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    Passenger passenger = hit.collider.GetComponentInParent<Passenger>();
                    if (passenger != null)
                    {
                        OnPassengerTap?.Invoke(passenger);
                        return;
                    }
                    
                    Bus bus = hit.collider.GetComponentInParent<Bus>();
                    if (bus != null)
                    {
                        OnBusTap?.Invoke(bus);
                    return;
                }
                
                OnEmptySpaceTap?.Invoke();
            }
            else
            {
                OnEmptySpaceTap?.Invoke();
            }
        }
    }

    public void Dispose()
    {
        OnTapWorld = null;
        OnTapScreen = null;
        OnPassengerTap = null;
        OnBusTap = null;
        OnEmptySpaceTap = null;
    }
}
