using UnityEngine;
using DG.Tweening;
using System;

public class Passenger : MonoBehaviour
{
    [SerializeField] private Renderer passengerRenderer;
    [SerializeField] private Animator passengerAnimator;
    [SerializeField] private float moveDuration = 1f;

    public PassengerColor CurrentColor { get; private set; }
    public bool IsMoving { get; private set; } = false;

    private static readonly int IsRunningAnimHash = Animator.StringToHash("isRunning");

    public void Initialize(PassengerColor color)
    {
        CurrentColor = color;
        ApplyColor();
        SetWalkingAnimation(false);
        IsMoving = false;
        transform.DOKill();
    }

    private void ApplyColor()
    {
        passengerRenderer.material.color = ColorUtility.GetUnityColor(CurrentColor);
    }

    public void Select()
    {
        // TODO: visual feedback for selection
        // Example: transform.localScale = Vector3.one * 1.2f;
    }

    public void Deselect()
    {
        // TODO: visual feedback for deselection
        // Example: transform.localScale = Vector3.one;
    }
    
    public void MoveToPosition(Vector3 targetPosition, Action onComplete = null)
    {
        if (IsMoving) return;

        IsMoving = true;
        SetWalkingAnimation(true);

        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.DOLookAt(targetPosition, 0.1f, AxisConstraint.Y);
        }

        transform.DOMoveZ(targetPosition.z, moveDuration);
        transform.DOMoveX(targetPosition.x, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                SetWalkingAnimation(false);
                IsMoving = false;
                onComplete?.Invoke();
            });
    }

    private void SetWalkingAnimation(bool isRunning)
    {
        passengerAnimator.SetBool(IsRunningAnimHash, isRunning);
    }

    private void OnDisable()
    {
        transform.DOKill();
        IsMoving = false;
    }
}
