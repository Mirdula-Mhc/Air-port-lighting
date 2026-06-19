using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Drives the full scenario sequence: shows signal, waits for answer,
/// validates, plays consequence, handles Next/Previous navigation.
/// Hook UI buttons (OptionA, OptionB, Next, Previous) to the public methods below.
/// </summary>
public class ScenarioManager : MonoBehaviour
{
    [Header("Scenario Sequence")]
    [SerializeField] private List<ScenarioData> scenarios;

    [Header("Scene References")]
    [SerializeField] private LightGunController lightGun;
    [SerializeField] private Animator consequenceAnimator; // aircraft/scene animator
    [SerializeField] private AudioSource voiceSource;

    [Header("UI Events (hook in Inspector)")]
    public UnityEvent<string> OnOptionATextSet;
    public UnityEvent<string> OnOptionBTextSet;
    public UnityEvent OnAnswerCorrect;          // e.g. play green checkmark, enable Next
    public UnityEvent OnAnswerWrong;            // e.g. flash red icon
    public UnityEvent<string> OnInstructorLine; // subtitle/captions text
    public UnityEvent<bool> OnNextButtonInteractable;
    public UnityEvent<bool> OnPrevButtonInteractable;
    public UnityEvent<bool, bool> OnOptionButtonsInteractable; // (optionA, optionB)

    private int currentIndex = 0;
    private bool[] answeredCorrectly;
    private bool waitingForAnswer = false;

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
        lightGun.SetSignal(s.lightColor, s.lightPattern);

        // Set prompt text
        OnOptionATextSet?.Invoke(s.optionA);
        OnOptionBTextSet?.Invoke(s.optionB);

        // Intro VO/caption if present
        if (!string.IsNullOrEmpty(s.instructorIntroLine))
            OnInstructorLine?.Invoke(s.instructorIntroLine);

        if (voiceSource != null && s.introVO != null)
            voiceSource.PlayOneShot(s.introVO);

        bool alreadyCorrect = answeredCorrectly[index];

        // Buttons stay interactable even on review so user can re-tap,
        // but if already correct, Next is immediately available
        OnOptionButtonsInteractable?.Invoke(true, true);
        OnNextButtonInteractable?.Invoke(alreadyCorrect);
        OnPrevButtonInteractable?.Invoke(index > 0);

        waitingForAnswer = !alreadyCorrect;
    }

    /// <summary>Call from Option A button OnClick</summary>
    public void SelectOptionA() => HandleSelection(0);

    /// <summary>Call from Option B button OnClick</summary>
    public void SelectOptionB() => HandleSelection(1);

    private void HandleSelection(int selectedIndex)
    {
        if (!waitingForAnswer && answeredCorrectly[currentIndex])
        {
            // Already correct, just reviewing - ignore re-selection or allow silently
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
            OnAnswerWrong?.Invoke();
            // Buttons remain interactable - user must pick again
        }
    }

    private IEnumerator HandleCorrectAnswer(ScenarioData s)
    {
        OnAnswerCorrect?.Invoke();
        OnInstructorLine?.Invoke(s.instructorCorrectLine);

        if (voiceSource != null && s.correctVO != null)
            voiceSource.PlayOneShot(s.correctVO);

        if (!string.IsNullOrEmpty(s.consequenceAnimationTrigger) && consequenceAnimator != null)
        {
            consequenceAnimator.SetTrigger(s.consequenceAnimationTrigger);
            // Optional: wait for animation length here if you want Next gated on animation finishing.
            // yield return new WaitForSeconds(animationDuration);
        }

        yield return null;

        OnNextButtonInteractable?.Invoke(true);
    }

    /// <summary>Call from Next button OnClick</summary>
    public void GoNext()
    {
        if (!answeredCorrectly[currentIndex]) return; // safety guard
        if (currentIndex < scenarios.Count - 1)
            ShowScenario(currentIndex + 1);
        else
            OnSequenceComplete();
    }

    /// <summary>Call from Previous button OnClick</summary>
    public void GoPrevious()
    {
        if (currentIndex > 0)
            ShowScenario(currentIndex - 1);
    }

    private void OnSequenceComplete()
    {
        // Hook up your scene-transition logic here (e.g. Scene 2 -> Scene 3)
        Debug.Log("Scenario sequence complete: " + (scenarios.Count > 0 ? scenarios[0].context.ToString() : ""));
    }
}