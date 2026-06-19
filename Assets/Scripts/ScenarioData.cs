using UnityEngine;

public enum LightColor
{
    Red,
    Green,
    White
}

public enum LightPattern
{
    Steady,
    Flashing
}

public enum SceneContext
{
    Ground,
    Flight,
    Advanced
}

public enum SignalVisualType
{
    None,
    LightGun,
    AlternatingRedGreen,
    Flare
}

[CreateAssetMenu(fileName = "Scenario_", menuName = "ATC/Scenario Data")]
public class ScenarioData : ScriptableObject
{
    [Header("Identification")]
    public string scenarioId;
    public SceneContext context;

    [Header("Page Type")]
    public bool requiresAnswer = true;

    [Header("Signal")]
    public SignalVisualType signalVisualType = SignalVisualType.LightGun;
    public LightColor lightColor;
    public LightPattern lightPattern;

    [Header("Question")]
    [TextArea(2, 5)]
    public string questionText;

    [TextArea(2, 5)]
    public string optionA;

    [TextArea(2, 5)]
    public string optionB;

    public int correctOptionIndex;

    [Header("Instructor")]
    [TextArea(2, 5)]
    public string instructorIntroLine;

    [Header("Feedback")]
    [TextArea(2, 5)]
    public string instructorCorrectLine;

    [TextArea(2, 5)]
    public string instructorWrongLine;

    [Header("Audio (Optional)")]
    public AudioClip introVO;
    public AudioClip correctVO;
    public AudioClip wrongVO;
}