using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ScenarioManager : MonoBehaviour
{
    [Header("Scenario Data")]
    [SerializeField] private List<ScenarioData> scenarios;

    [Header("Signal Controllers")]
    [SerializeField] private LightGunController lightGun;
    [SerializeField] private FlareController flareController;

    [Header("Instruction UI")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private TextMeshProUGUI instructionText;

    [Header("Question UI")]
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private TextMeshProUGUI questionText;

    [SerializeField] private Button optionAButton;
    [SerializeField] private TextMeshProUGUI optionAText;

    [SerializeField] private Button optionBButton;
    [SerializeField] private TextMeshProUGUI optionBText;

    [Header("Feedback UI (short auto title, e.g. Correct/Incorrect)")]
    [SerializeField] private TextMeshProUGUI feedbackTitleText;

    [Header("Per-Option Result Icons (empty Image next to each option)")]
    [SerializeField] private Image optionAResultIcon;
    [SerializeField] private Image optionBResultIcon;
    [SerializeField] private Sprite correctIconSprite;
    [SerializeField] private Sprite wrongIconSprite;

    [Header("Feedback Audio")]
    [SerializeField] private AudioSource feedbackAudioSource;
    [SerializeField] private AudioClip correctChime;
    [SerializeField] private AudioClip wrongChime;

    [Header("Explain Button (appears after answering)")]
    [SerializeField] private Button explainButton;

    [Header("Explanation Panel (a.k.a. Feedback Panel - same thing)")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TextMeshProUGUI feedbackDescriptionText;
    [SerializeField] private Button continueButton;

    [Header("Navigation")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    private int currentIndex;
    private bool[] completedSteps;
    private bool lastAnswerCorrect;

    private void Awake()
    {
        if (optionAButton != null)
            optionAButton.onClick.AddListener(() => SelectAnswer(0));

        if (optionBButton != null)
            optionBButton.onClick.AddListener(() => SelectAnswer(1));

        if (explainButton != null)
            explainButton.onClick.AddListener(OpenExplanation);

        if (continueButton != null)
            continueButton.onClick.AddListener(CloseExplanation);

        if (nextButton != null)
            nextButton.onClick.AddListener(GoNext);

        if (previousButton != null)
            previousButton.onClick.AddListener(GoPrevious);
    }

    private void Start()
    {
        completedSteps = new bool[scenarios.Count];

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        ShowScenario(0);
    }

    private void ShowScenario(int index)
    {
        if (index < 0 || index >= scenarios.Count)
            return;

        currentIndex = index;

        ScenarioData scenario = scenarios[index];

        // Always start a fresh scenario with the explanation panel closed
        // and the explain button hidden until a new answer is given.
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        if (explainButton != null)
            explainButton.gameObject.SetActive(false);

        if (feedbackTitleText != null)
            feedbackTitleText.text = string.Empty;

        if (optionAResultIcon != null)
            optionAResultIcon.gameObject.SetActive(false);

        if (optionBResultIcon != null)
            optionBResultIcon.gameObject.SetActive(false);

        if (previousButton != null)
            previousButton.interactable = currentIndex > 0;

        HandleSignal(scenario);

        // INTRO PAGE
        if (!scenario.requiresAnswer)
        {
            if (instructionPanel != null)
                instructionPanel.SetActive(true);

            if (questionPanel != null)
                questionPanel.SetActive(false);

            if (instructionText != null)
                instructionText.text = scenario.instructorIntroLine;

            completedSteps[index] = true;

            if (nextButton != null)
                nextButton.interactable = true;

            return;
        }

        // QUESTION PAGE
        if (instructionPanel != null)
            instructionPanel.SetActive(false);

        if (questionPanel != null)
            questionPanel.SetActive(true);

        if (questionText != null)
            questionText.text = scenario.questionText;

        if (optionAText != null)
            optionAText.text = scenario.optionA;

        if (optionBText != null)
            optionBText.text = scenario.optionB;

        bool alreadyCompleted = completedSteps[index];

        if (optionAButton != null)
            optionAButton.interactable = !alreadyCompleted;

        if (optionBButton != null)
            optionBButton.interactable = !alreadyCompleted;

        if (nextButton != null)
            nextButton.interactable = alreadyCompleted;

        // If the user already answered correctly before (e.g. came back via
        // Previous then Next), let them reopen the explain button too.
        if (alreadyCompleted && explainButton != null)
        {
            lastAnswerCorrect = true;
            explainButton.gameObject.SetActive(true);
        }
    }

    private void HandleSignal(ScenarioData scenario)
    {
        if (lightGun != null)
            lightGun.ClearSignal();

        switch (scenario.signalVisualType)
        {
            case SignalVisualType.None:
                break;

            case SignalVisualType.LightGun:

                if (lightGun != null)
                {
                    lightGun.SetSignal(
                        scenario.lightColor,
                        scenario.lightPattern);
                }

                break;

            case SignalVisualType.AlternatingRedGreen:

                if (lightGun != null)
                {
                    lightGun.PlayAlternatingSignal();
                }

                break;

            case SignalVisualType.Flare:

                if (flareController != null)
                {
                    flareController.PlayFlare();
                }

                break;
        }
    }

    private void SelectAnswer(int selectedAnswer)
    {
        ScenarioData scenario = scenarios[currentIndex];

        bool correct =
            selectedAnswer == scenario.correctOptionIndex;

        lastAnswerCorrect = correct;

        // Short, automatic feedback only - no explanation text yet.
        if (feedbackTitleText != null)
            feedbackTitleText.text = correct ? "Correct" : "Incorrect";

        if (explainButton != null)
            explainButton.gameObject.SetActive(true);

        // Clear the other option's icon first, so only the just-clicked
        // option ever shows a result icon at a time.
        Image otherIcon = (selectedAnswer == 0) ? optionBResultIcon : optionAResultIcon;
        if (otherIcon != null)
            otherIcon.gameObject.SetActive(false);

        // Show the correct/wrong icon next to the option that was clicked.
        Image clickedIcon = (selectedAnswer == 0) ? optionAResultIcon : optionBResultIcon;
        if (clickedIcon != null)
        {
            clickedIcon.sprite = correct ? correctIconSprite : wrongIconSprite;
            clickedIcon.gameObject.SetActive(true);
        }

        if (feedbackAudioSource != null)
        {
            AudioClip clip = correct ? correctChime : wrongChime;
            if (clip != null)
                feedbackAudioSource.PlayOneShot(clip);
        }

        if (correct)
        {
            completedSteps[currentIndex] = true;

            // LOCK ANSWERS FOREVER
            if (optionAButton != null)
                optionAButton.interactable = false;

            if (optionBButton != null)
                optionBButton.interactable = false;

            if (nextButton != null)
                nextButton.interactable = true;
        }
        else
        {
            // Buzz only on a wrong selection, per Mirdula's call.
            Handheld.Vibrate();
        }
        // Wrong answers: no lockout, user can simply try again.
        // explainButton stays available if they want the full explanation first.
    }

    private void OpenExplanation()
    {
        ScenarioData scenario = scenarios[currentIndex];

        if (feedbackDescriptionText != null)
        {
            feedbackDescriptionText.text = lastAnswerCorrect
                ? scenario.instructorCorrectLine
                : scenario.instructorWrongLine;
        }

        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);

        // Defensive: hide question panel underneath in case the
        // explanation panel material isn't fully opaque.
        if (questionPanel != null)
            questionPanel.SetActive(false);
    }

    private void CloseExplanation()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        if (questionPanel != null)
            questionPanel.SetActive(true);
    }

    private void GoNext()
    {
        if (!completedSteps[currentIndex])
            return;

        if (currentIndex < scenarios.Count - 1)
        {
            ShowScenario(currentIndex + 1);
        }
        else
        {
            Debug.Log("ATC Training Complete");
        }
    }

    private void GoPrevious()
    {
        if (currentIndex > 0)
        {
            ShowScenario(currentIndex - 1);
        }
    }
}