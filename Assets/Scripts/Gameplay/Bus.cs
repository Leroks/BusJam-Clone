using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using DG.Tweening;

public class Bus : MonoBehaviour
{
    [SerializeField] private Renderer busRenderer;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float arrivalOffset = 5f;
    [SerializeField] private float departureOffset = 5f;

    public event Action<Bus> OnBusReadyToDepart;
    public event Action<Bus> OnBusArrivedAtStop;
    public event Action<Bus> OnDepartureComplete;

    public PassengerColor BusColor { get; private set; }
    public int Capacity { get; private set; }
    public int CurrentPassengerCount { get; private set; }

    private List<Passenger> _passengersOnBoard;
    private Vector3 _busStopPosition;

    public bool IsFull => CurrentPassengerCount >= Capacity;

    public void Initialize(BusData data, Vector3 actualStopPosition)
    {
        BusColor = data.color;
        Capacity = data.capacity;
        _busStopPosition = actualStopPosition;

        transform.position = _busStopPosition - Vector3.right * arrivalOffset;

        CurrentPassengerCount = 0;
        _passengersOnBoard = new List<Passenger>(Capacity);

        ApplyBusColor();
    }

    public void StartArrivalSequence()
    {
        StartCoroutine(ArriveAtStopRoutine());
    }

    private IEnumerator ArriveAtStopRoutine()
    {
        Vector3 initialPosition = transform.position;
        float journeyLength = Vector3.Distance(initialPosition, _busStopPosition);
        float startTime = Time.time;

        if (journeyLength > 0)
        {
            float distCovered = (Time.time - startTime) * moveSpeed;
            float fractionOfJourney = distCovered / journeyLength;

            while (fractionOfJourney < 1)
            {
                distCovered = (Time.time - startTime) * moveSpeed;
                fractionOfJourney = distCovered / journeyLength;
                transform.position = Vector3.Lerp(initialPosition, _busStopPosition, fractionOfJourney);
                yield return null;
            }
        }
        transform.position = _busStopPosition;
        
        transform.DOMoveY(transform.position.y + 0.005f, 0.2f).SetEase(Ease.InOutQuad).SetLoops(-1, LoopType.Yoyo);
        OnBusArrivedAtStop?.Invoke(this);
    }
    
    private void ApplyBusColor()
    {
        busRenderer.material.color = ColorUtility.GetUnityColor(BusColor);
    }

    public bool CanBoard(Passenger passenger)
    {
        if (IsFull) return false;
        return passenger.CurrentColor == BusColor;
    }

    public bool AddPassenger(Passenger passenger)
    {
        if (!CanBoard(passenger)) return false;

        CurrentPassengerCount++;
        _passengersOnBoard.Add(passenger);
        
        if (IsFull)
        {
            HandleBusFull();
        }
        return true;
    }

    private void HandleBusFull()
    {
        Debug.Log($"{BusColor} bus ({gameObject.name}) is full and ready to depart");
        OnBusReadyToDepart?.Invoke(this);
    }

    public void StartDeparture()
    {
        StartCoroutine(DepartRoutine());
    }

    private IEnumerator DepartRoutine()
    {
        Debug.Log($"Bus {gameObject.name} starting departure routine.");
        DOTween.Kill(transform, true);

        Vector3 initialPosition = transform.position;
        Vector3 targetPosition = initialPosition + Vector3.right * departureOffset;
        float journeyLength = Vector3.Distance(initialPosition, targetPosition);
        float startTime = Time.time;

        if (journeyLength > 0)
        {
            float distCovered = (Time.time - startTime) * moveSpeed;
            float fractionOfJourney = distCovered / journeyLength;

            while (fractionOfJourney < 1)
            {
                distCovered = (Time.time - startTime) * moveSpeed;
                fractionOfJourney = distCovered / journeyLength;
                transform.position = Vector3.Lerp(initialPosition, targetPosition, fractionOfJourney);
                yield return null;
            }
        }
        
        transform.position = targetPosition;
        
        OnDepartureComplete?.Invoke(this);
        
        gameObject.SetActive(false); 
    }

}
