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

public enum NonInteractivePageType
{
    None,       // No panel shown - just VO + optional animation, Next unlocks after both.
    InfoPanel,  // Small existing info panel.
    SceneIntro  // Big per-context panel (Ground/Flight/Advanced).
}

[CreateAssetMenu(fileName = "Scenario_", menuName = "ATC/Scenario Data")]
public class ScenarioData : ScriptableObject
{
    [Header("Identification")]
    public string scenarioId;
    public SceneContext context;

    [Header("Page Type")]
    public bool requiresAnswer = true;
    [Tooltip("Only used when Requires Answer is OFF. InfoPanel = small existing panel. SceneIntro = big per-context panel (Ground/Flight/Advanced).")]
    public NonInteractivePageType pageType = NonInteractivePageType.InfoPanel;

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

    [Header("Consequence Animation - Correct (required for now)")]
    [Tooltip("Trigger parameter name on the ScenarioManager's Animator Reference for a correct answer.")]
    public string correctAnimTrigger;
    [Tooltip("The same clip the trigger above leads to in the Animator Controller. Used only to auto-read the duration - assign it here too so the script doesn't need a manually-typed wait time.")]
    public AnimationClip correctAnimClip;

    [Header("Consequence Animation - Wrong (optional)")]
    [Tooltip("Leave empty if this scenario has no wrong-answer animation yet.")]
    public string wrongAnimTrigger;
    [Tooltip("The same clip wrongAnimTrigger leads to. Used only to auto-read the duration. Ignored if wrongAnimTrigger is empty.")]
    public AnimationClip wrongAnimClip;
}