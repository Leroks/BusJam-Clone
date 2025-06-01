using UnityEngine;

public class Passenger : MonoBehaviour
{
    [SerializeField] private Renderer passengerRenderer;

    public PassengerColor CurrentColor { get; private set; }

    public void Initialize(PassengerColor color)
    {
        CurrentColor = color;
        ApplyColor();
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
}
