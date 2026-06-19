using UnityEngine;

public enum LightColor { Red, Green, White }
public enum LightPattern { Steady, Flashing }
public enum SceneContext { Ground, Flight, Advanced }

[CreateAssetMenu(fileName = "Scenario_", menuName = "ATC/Scenario Data")]
public class ScenarioData : ScriptableObject
{
    [Header("Identification")]
    public string scenarioId;          // e.g. "Ground_A_SteadyRed"
    public SceneContext context;

    [Header("Light Signal")]
    public LightColor lightColor;
    public LightPattern lightPattern;

    [Header("Prompt Options")]
    [TextArea] public string optionA;
    [TextArea] public string optionB;
    public int correctOptionIndex;     // 0 = A, 1 = B

    [Header("Feedback")]
    [TextArea] public string instructorCorrectLine;
    [TextArea] public string instructorIntroLine; // optional, played when scenario starts

    [Header("Consequence Animation (optional)")]
    public string consequenceAnimationTrigger; // Animator trigger name, leave blank if none

    [Header("Audio (optional)")]
    public AudioClip introVO;
    public AudioClip correctVO;
}