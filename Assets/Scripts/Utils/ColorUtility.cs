using UnityEngine;
public static class ColorUtility
{
    public static Color GetUnityColor(PassengerColor colorEnum)
    {
        switch (colorEnum)
        {
            case PassengerColor.Red: return Color.red;
            case PassengerColor.Green: return Color.green;
            case PassengerColor.Blue: return Color.blue;
            case PassengerColor.Yellow: return Color.yellow;
            case PassengerColor.Purple: return new Color(0.5f, 0f, 0.5f);
            case PassengerColor.Orange: return new Color(1f, 0.5f, 0f);
            case PassengerColor.Black: return Color.grey;
            default: return Color.white;
        }
    }
}
