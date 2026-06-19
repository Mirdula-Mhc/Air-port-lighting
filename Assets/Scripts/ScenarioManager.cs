using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Drives the full scenario sequence: shows signal, waits for answer,
/// validates, plays consequence, handles Next/Previous navigation.
/// All UI references are dragged directly into the Inspector slots below -
/// no event binding needed.
/// </summary>
public class ScenarioManager : MonoBehaviour
{
    [Header("Scenario Sequence")]
    [Tooltip("Drag your ScenarioData assets here in the order they should play")]
    [SerializeField] private List<ScenarioData> scenarios;

    [Header("Scene References")]
    [Tooltip("The GameObject with LightGunController on it")]
    [SerializeField] private LightGunController lightGun;
    [Tooltip("Optional - the Animator that plays consequence animations (aircraft movement etc)")]
    [SerializeField] private Animator consequenceAnimator;
    [Tooltip("Optional - an AudioSource to play instructor voice lines")]
    [SerializeField] private AudioSource voiceSource;

    [Header("UI - Option Buttons")]
    [Tooltip("The first choice button (e.g. left option)")]
    [SerializeField] private Button optionAButton;
    [Tooltip("Text inside Option A button")]
    [SerializeField] private TextMeshProUGUI optionAText;
    [Tooltip("The second choice button (e.g. right option)")]
    [SerializeField] private Button optionBButton;
    [Tooltip("Text inside Option B button")]
    [SerializeField] private TextMeshProUGUI optionBText;

    [Header("UI - Navigation Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    [Header("UI - Feedback")]
    [Tooltip("Subtitle/caption text showing instructor lines")]
    [SerializeField] private TextMeshProUGUI instructorText;
    [Tooltip("Optional - GameObject shown briefly when answer is wrong (e.g. a red X icon)")]
    [SerializeField] private GameObject wrongIcon;
    [Tooltip("Optional - GameObject shown briefly when answer is correct (e.g. a green check icon)")]
    [SerializeField] private GameObject correctIcon;
    [Tooltip("How long the wrong/correct icon stays visible, in seconds")]
    [SerializeField] private float feedbackIconDuration = 1.2f;

    private int currentIndex = 0;
    private bool[] answeredCorrectly;
    private bool waitingForAnswer = false;

    private void Awake()
    {
        // Wire button clicks once, here, so nothing needs to be set up in the Inspector's OnClick() lists
        if (optionAButton != null) optionAButton.onClick.AddListener(SelectOptionA);
        if (optionBButton != null) optionBButton.onClick.AddListener(SelectOptionB);
        if (nextButton != null) nextButton.onClick.AddListener(GoNext);
        if (previousButton != null) previousButton.onClick.AddListener(GoPrevious);

        if (wrongIcon != null) wrongIcon.SetActive(false);
        if (correctIcon != null) correctIcon.SetActive(false);
    }

    private void Start()
    {
        answeredCorrectly = new bool[scenarios.Count];
        ShowScenario(currentIndex);
    }

    private void ShowScenario(int index)
    {
        if (index < 0 || index >= scenarios.Count) return;

        currentIndex = index;
        ScenarioData s = scenarios[index];

        // Display the light signal
        if (lightGun != null)
            lightGun.SetSignal(s.lightColor, s.lightPattern);

        // Set prompt text directly
        if (optionAText != null) optionAText.text = s.optionA;
        if (optionBText != null) optionBText.text = s.optionB;

        // Intro VO/caption if present
        if (!string.IsNullOrEmpty(s.instructorIntroLine) && instructorText != null)
            instructorText.text = s.instructorIntroLine;

        if (voiceSource != null && s.introVO != null)
            voiceSource.PlayOneShot(s.introVO);

        bool alreadyCorrect = answeredCorrectly[index];

        SetOptionButtonsInteractable(true);
        if (nextButton != null) nextButton.interactable = alreadyCorrect;
        if (previousButton != null) previousButton.interactable = index > 0;

        waitingForAnswer = !alreadyCorrect;
    }

    public void SelectOptionA() => HandleSelection(0);
    public void SelectOptionB() => HandleSelection(1);

    private void HandleSelection(int selectedIndex)
    {
        if (!waitingForAnswer && answeredCorrectly[currentIndex])
        {
            // Already correct, just reviewing - ignore re-selection
            return;
        }

        ScenarioData s = scenarios[currentIndex];

        if (selectedIndex == s.correctOptionIndex)
        {
            answeredCorrectly[currentIndex] = true;
            waitingForAnswer = false;
            StartCoroutine(HandleCorrectAnswer(s));
        }
        else
        {
            StartCoroutine(ShowWrongFeedback());
        }
    }

    private IEnumerator ShowWrongFeedback()
    {
        if (wrongIcon != null)
        {
            wrongIcon.SetActive(true);
            yield return new WaitForSeconds(feedbackIconDuration);
            wrongIcon.SetActive(false);
        }
        // Buttons remain interactable the whole time - user must pick again
    }

    private IEnumerator HandleCorrectAnswer(ScenarioData s)
    {
        if (correctIcon != null)
        {
            correctIcon.SetActive(true);
        }

        if (instructorText != null)
            instructorText.text = s.instructorCorrectLine;

        if (voiceSource != null && s.correctVO != null)
            voiceSource.PlayOneShot(s.correctVO);

        if (!string.IsNullOrEmpty(s.consequenceAnimationTrigger) && consequenceAnimator != null)
        {
            consequenceAnimator.SetTrigger(s.consequenceAnimationTrigger);
        }

        if (correctIcon != null)
        {
            yield return new WaitForSeconds(feedbackIconDuration);
            correctIcon.SetActive(false);
        }
        else
        {
            yield return null;
        }

        if (nextButton != null) nextButton.interactable = true;
    }

    public void GoNext()
    {
        if (!answeredCorrectly[currentIndex]) return; // safety guard
        if (currentIndex < scenarios.Count - 1)
            ShowScenario(currentIndex + 1);
        else
            OnSequenceComplete();
    }

    public void GoPrevious()
    {
        if (currentIndex > 0)
            ShowScenario(currentIndex - 1);
    }

    private void SetOptionButtonsInteractable(bool value)
    {
        if (optionAButton != null) optionAButton.interactable = value;
        if (optionBButton != null) optionBButton.interactable = value;
    }

    private void OnSequenceComplete()
    {
        // Hook up your scene-transition logic here (e.g. Scene 2 -> Scene 3)
        Debug.Log("Scenario sequence complete: " + (scenarios.Count > 0 ? scenarios[0].context.ToString() : ""));
    }
}