using UnityEngine;
using DG.Tweening; // Import DOTween
using System; // For Action

public class Passenger : MonoBehaviour
{
    [SerializeField] private Renderer passengerRenderer;
    [SerializeField] private Animator passengerAnimator; // Assign in Inspector
    [SerializeField] private float moveDuration = 1f; // Duration of the walk

    public PassengerColor CurrentColor { get; private set; }
    public bool IsMoving { get; private set; } = false;

    private static readonly int IsRunningAnimHash = Animator.StringToHash("isRunning"); // Changed to isRunning

    public void Initialize(PassengerColor color)
    {
        CurrentColor = color;
        ApplyColor();
        SetWalkingAnimation(false); // Start in idle
        IsMoving = false;
        transform.DOKill(); // Kill any previous tweens on reuse from pool
    }

    private void ApplyColor()
    {
        if (passengerRenderer == null)
        {
            Debug.LogError("PassengerRenderer not assigned on " + gameObject.name);
            return;
        }
        
        passengerRenderer.material.color = GetUnityColor(CurrentColor);
    }

    private Color GetUnityColor(PassengerColor colorEnum)
    {
        switch (colorEnum)
        {
            case PassengerColor.Red: return Color.red;
            case PassengerColor.Green: return Color.green;
            case PassengerColor.Blue: return Color.blue;
            case PassengerColor.Yellow: return Color.yellow;
            case PassengerColor.Purple: return new Color(0.5f, 0f, 0.5f);
            case PassengerColor.Orange: return new Color(1f, 0.5f, 0f);
            case PassengerColor.Black: return Color.black;
            default: return Color.white;
        }
    }

    public void Select()
    {
        // TODO: Implement visual feedback for selection (e.g., highlight, scale up)
        Debug.Log(gameObject.name + " selected.");
        // Example: transform.localScale = Vector3.one * 1.2f;
    }

    public void Deselect()
    {
        // TODO: Implement visual feedback for deselection (e.g., revert highlight, scale down)
        Debug.Log(gameObject.name + " deselected.");
        // Example: transform.localScale = Vector3.one;
    }

    // TODO: Add other passenger behaviors here later (movement, interaction etc.)

    public void MoveToPosition(Vector3 targetPosition, Action onComplete = null)
    {
        if (IsMoving) return; // Don't start a new move if already moving

        IsMoving = true;
        SetWalkingAnimation(true);

        // Optional: Make passenger look at the target position (or direction of movement)
        // Vector3 direction = (targetPosition - transform.position).normalized;
        // if (direction != Vector3.zero)
        // {
        //    transform.DOLookAt(targetPosition, 0.1f, AxisConstraint.Y); // Adjust axis constraint as needed
        // }

        transform.DOMove(targetPosition, moveDuration)
            .SetEase(Ease.Linear) // Or another ease you prefer
            .OnComplete(() =>
            {
                SetWalkingAnimation(false);
                IsMoving = false;
                onComplete?.Invoke();
            });
    }

    private void SetWalkingAnimation(bool isRunning) // Parameter name can remain isWalking or change to isRunning for clarity
    {
        if (passengerAnimator != null)
        {
            passengerAnimator.SetBool(IsRunningAnimHash, isRunning); // Use IsRunningAnimHash
        }
        else
        {
            // Debug.LogWarning("PassengerAnimator not assigned on " + gameObject.name);
        }
    }

    private void OnDisable()
    {
        // Ensure DOTween tweens are killed when the object is disabled/pooled
        // to prevent issues if it's re-enabled while a tween was "paused".
        transform.DOKill();
        IsMoving = false; // Reset state
    }
}
