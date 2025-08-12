// You can create a ScriptableObject for this to make tuning easy in the editor.
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "New Flight Profile", menuName = "Gliding System/Create Flight Profile")]
public class FlightProfile : ScriptableObject
{
    public string profileName; // "Parachute", "Glider", etc.

    // --- Control Feel ---
    public float pitchSpeed = 5f;
    public float rollSpeed = 5f;
    public float yawFromRoll = 1.5f;
    public float maxPitchAngle = 45f;
    public float minPitchAngle = 30f; // Used for pulling up

    // --- Core Physics ---
    public float baseDrag = 0.1f;         // General air resistance
    public float verticalDragBonus = 0.5f; // Extra drag that only affects vertical speed
    public float liftCoefficient = 15f;   // How effectively pitch generates upward lift
    public float forwardThrustFactor = 2f;  // How effectively falling converts to forward speed
    public float terminalVelocity = -50f; // Maximum fall speed
    public float maxSpeed = 70f;          // Maximum overall speed

    // --- Glider-Specific ---
    public bool canStall = false;
    public float stallAngle = 25f;  // Pitch angle above which you stall
    public float stallSpeed = 10f;  // Speed below which you stall
}