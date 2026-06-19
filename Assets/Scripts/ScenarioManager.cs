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

    [Header("Feedback UI")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TextMeshProUGUI feedbackTitleText;
    [SerializeField] private TextMeshProUGUI feedbackDescriptionText;

    [Header("Navigation")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    private int currentIndex;
    private bool[] completedSteps;

    private void Awake()
    {
        if (optionAButton != null)
            optionAButton.onClick.AddListener(() => SelectAnswer(0));

        if (optionBButton != null)
            optionBButton.onClick.AddListener(() => SelectAnswer(1));

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

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        previousButton.interactable = currentIndex > 0;

        HandleSignal(scenario);

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

        if (nextButton != null)
            nextButton.interactable = completedSteps[index];
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

        bool correct = selectedAnswer == scenario.correctOptionIndex;

        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);

        if (correct)
        {
            if (feedbackTitleText != null)
                feedbackTitleText.text = "Correct";

            if (feedbackDescriptionText != null)
                feedbackDescriptionText.text = scenario.instructorCorrectLine;

            completedSteps[currentIndex] = true;

            if (nextButton != null)
                nextButton.interactable = true;
        }
        else
        {
            if (feedbackTitleText != null)
                feedbackTitleText.text = "Incorrect";

            if (feedbackDescriptionText != null)
                feedbackDescriptionText.text = scenario.instructorWrongLine;
        }
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