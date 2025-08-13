using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlightProfile))]
public class FlightProfileEditor : Editor
{
    SerializedProperty profileName;
    SerializedProperty pitchSpeed;
    SerializedProperty rollSpeed;
    SerializedProperty yawFromRoll;
    SerializedProperty maxPitchAngle;
    SerializedProperty minPitchAngle;
    SerializedProperty baseDrag;
    SerializedProperty verticalDragBonus;
    SerializedProperty liftCoefficient;
    SerializedProperty forwardThrustFactor;
    SerializedProperty terminalVelocity;
    SerializedProperty maxSpeed;
    SerializedProperty canStall;
    SerializedProperty stallAngle;
    SerializedProperty stallSpeed;

    private void OnEnable()
    {
        profileName = serializedObject.FindProperty("profileName");
        pitchSpeed = serializedObject.FindProperty("pitchSpeed");
        rollSpeed = serializedObject.FindProperty("rollSpeed");
        yawFromRoll = serializedObject.FindProperty("yawFromRoll");
        maxPitchAngle = serializedObject.FindProperty("maxPitchAngle");
        minPitchAngle = serializedObject.FindProperty("minPitchAngle");
        baseDrag = serializedObject.FindProperty("baseDrag");
        verticalDragBonus = serializedObject.FindProperty("verticalDragBonus");
        liftCoefficient = serializedObject.FindProperty("liftCoefficient");
        forwardThrustFactor = serializedObject.FindProperty("forwardThrustFactor");
        terminalVelocity = serializedObject.FindProperty("terminalVelocity");
        maxSpeed = serializedObject.FindProperty("maxSpeed");
        canStall = serializedObject.FindProperty("canStall");
        stallAngle = serializedObject.FindProperty("stallAngle");
        stallSpeed = serializedObject.FindProperty("stallSpeed");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(profileName);
        EditorGUILayout.PropertyField(pitchSpeed);
        EditorGUILayout.PropertyField(rollSpeed);
        EditorGUILayout.PropertyField(yawFromRoll);
        EditorGUILayout.PropertyField(maxPitchAngle);
        EditorGUILayout.PropertyField(minPitchAngle);
        EditorGUILayout.PropertyField(baseDrag);
        EditorGUILayout.PropertyField(verticalDragBonus);
        EditorGUILayout.PropertyField(liftCoefficient);
        EditorGUILayout.PropertyField(forwardThrustFactor);
        EditorGUILayout.PropertyField(terminalVelocity);
        EditorGUILayout.PropertyField(maxSpeed);
        EditorGUILayout.PropertyField(canStall);

        if (canStall.boolValue)
        {
            EditorGUILayout.PropertyField(stallAngle);
            EditorGUILayout.PropertyField(stallSpeed);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
