using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScenarioManager scenarioManager;

    [Header("Instruction UI")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private TextMeshProUGUI instructionText;

    [Header("Scene Intro Panels")]
    [SerializeField] private GameObject groundIntroPanel;
    [SerializeField] private TextMeshProUGUI groundIntroText;
    [SerializeField] private GameObject flightIntroPanel;
    [SerializeField] private TextMeshProUGUI flightIntroText;
    [SerializeField] private GameObject advancedIntroPanel;
    [SerializeField] private TextMeshProUGUI advancedIntroText;

    [Header("Question UI")]
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Button optionAButton;
    [SerializeField] private TextMeshProUGUI optionAText;
    [SerializeField] private Button optionBButton;
    [SerializeField] private TextMeshProUGUI optionBText;

    [Header("Result Icons")]
    [SerializeField] private Image optionAResultIcon;
    [SerializeField] private Image optionBResultIcon;
    [SerializeField] private Sprite correctIconSprite;
    [SerializeField] private Sprite wrongIconSprite;

    [Header("Feedback Popup")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TextMeshProUGUI feedbackDescriptionText;
    [SerializeField] private float typewriterCharDelay = 0.03f;
    [SerializeField] private float popupHoldTime = 2f;

    [Header("Vignette")]
    [SerializeField] private CanvasGroup wrongVignette;
    [SerializeField] private float vignetteFadeInTime = 0.15f;
    [SerializeField] private float vignetteHoldTime = 0.2f;
    [SerializeField] private float vignetteFadeOutTime = 0.4f;

    [Header("Navigation")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    [Header("Completion Panel")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private TextMeshProUGUI completionTitleText;
    [SerializeField] private TextMeshProUGUI completionBodyText;
    [SerializeField] private Button completionNextButton;
    [SerializeField] private CompletionTextData completionTextData;

    private void Awake()
    {
        optionAButton?.onClick.AddListener(() => scenarioManager.SelectAnswer(0));
        optionBButton?.onClick.AddListener(() => scenarioManager.SelectAnswer(1));
        nextButton?.onClick.AddListener(scenarioManager.GoNext);
        previousButton?.onClick.AddListener(scenarioManager.GoPrevious);
        completionNextButton?.onClick.AddListener(scenarioManager.GoNext);
    }

    private void OnEnable()
    {
        scenarioManager.OnScenarioLoaded += HandleScenarioLoaded;
        scenarioManager.OnCorrectAnswer += HandleCorrectAnswer;
        scenarioManager.OnWrongAnswer += HandleWrongAnswer;
        scenarioManager.OnCorrectAnimComplete += HandleCorrectAnimComplete;
        scenarioManager.OnAllScenariosComplete += HandleAllComplete;
    }

    private void OnDisable()
    {
        scenarioManager.OnScenarioLoaded -= HandleScenarioLoaded;
        scenarioManager.OnCorrectAnswer -= HandleCorrectAnswer;
        scenarioManager.OnWrongAnswer -= HandleWrongAnswer;
        scenarioManager.OnCorrectAnimComplete -= HandleCorrectAnimComplete;
        scenarioManager.OnAllScenariosComplete -= HandleAllComplete;
    }

    // ?? Scenario Loaded ??????????????????????????????????????????????

    private void HandleScenarioLoaded(ScenarioData scenario, int index)
    {
        HideAllPanels();
        ClearResultIcons();

        completionPanel?.SetActive(false);
        nextButton?.gameObject.SetActive(true);

        if (previousButton != null)
            previousButton.interactable = index > 0;

        if (!scenario.requiresAnswer)
        {
            HandleNonInteractivePage(scenario, index);
            return;
        }

        ShowQuestionPage(scenario, index);
    }

    private void HandleNonInteractivePage(ScenarioData scenario, int index)
    {
        if (nextButton != null) nextButton.interactable = false;
        if (previousButton != null) previousButton.interactable = false;

        if (scenario.pageType == NonInteractivePageType.InfoPanel)
        {
            if (instructionPanel != null) instructionPanel.SetActive(true);
            if (instructionText != null) instructionText.text = scenario.instructorIntroLine;
            if (nextButton != null) nextButton.interactable = true;
            if (previousButton != null) previousButton.interactable = scenarioManager.CurrentIndex > 0;
            return;
        }

        GameObject panel = scenario.context switch
        {
            SceneContext.Ground => groundIntroPanel,
            SceneContext.Flight => flightIntroPanel,
            SceneContext.Advanced => advancedIntroPanel,
            _ => null
        };

        TextMeshProUGUI text = scenario.context switch
        {
            SceneContext.Ground => groundIntroText,
            SceneContext.Flight => flightIntroText,
            SceneContext.Advanced => advancedIntroText,
            _ => null
        };

        if (panel != null) panel.SetActive(true);
        if (text != null) text.text = scenario.instructorIntroLine;
    }

    private void ShowQuestionPage(ScenarioData scenario, int index)
    {
        if (questionPanel != null) questionPanel.SetActive(true);
        if (questionText != null) questionText.text = scenario.questionText;
        if (optionAText != null) optionAText.text = scenario.optionA;
        if (optionBText != null) optionBText.text = scenario.optionB;

        bool alreadyCompleted = scenarioManager.IsCompleted(index);

        if (optionAButton != null) optionAButton.interactable = !alreadyCompleted;
        if (optionBButton != null) optionBButton.interactable = !alreadyCompleted;
        if (nextButton != null) nextButton.interactable = alreadyCompleted;
    }

    // ?? Answer Handling ??????????????????????????????????????????????

    private void HandleCorrectAnswer(ScenarioData scenario, int selectedIndex)
    {
        ShowResultIcon(selectedIndex, true);
        SetAnswerButtonsInteractable(false);
        StartCoroutine(CorrectSequence(scenario));
    }

    private void HandleWrongAnswer(ScenarioData scenario, int selectedIndex)
    {
        ShowResultIcon(selectedIndex, false);
        StartCoroutine(FlashVignette());
        StartCoroutine(WrongSequence(scenario));
    }

    // ?? Correct Sequence ?????????????????????????????????????????????

    private IEnumerator CorrectSequence(ScenarioData scenario)
    {
        yield return StartCoroutine(ShowFeedbackPopup(scenario.instructorCorrectLine));
        if (questionPanel != null) questionPanel.SetActive(false);
        LockNav();
    }

    private void HandleCorrectAnimComplete()
    {
        UnlockNav();
        ShowCompletionPanel();
    }

    // ?? Wrong Sequence ???????????????????????????????????????????????

    private IEnumerator WrongSequence(ScenarioData scenario)
    {
        yield return StartCoroutine(ShowFeedbackPopup(scenario.instructorWrongLine));
        SetAnswerButtonsInteractable(true);
        ClearResultIcons();
    }

    // ?? Feedback Popup / Typewriter ??????????????????????????????????

    private IEnumerator ShowFeedbackPopup(string line)
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(true);
        if (questionPanel != null) questionPanel.SetActive(false);
        LockNav();

        yield return StartCoroutine(TypewriterReveal(feedbackDescriptionText, line));
        yield return new WaitForSeconds(popupHoldTime);

        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        if (questionPanel != null) questionPanel.SetActive(true);
        UnlockNav();
    }

    private IEnumerator TypewriterReveal(TextMeshProUGUI target, string fullText)
    {
        if (target == null) yield break;
        target.text = string.Empty;
        if (string.IsNullOrEmpty(fullText)) yield break;

        foreach (char c in fullText)
        {
            target.text += c;
            yield return new WaitForSeconds(typewriterCharDelay);
        }
    }

    // ?? Completion Panel ?????????????????????????????????????????????

    private void ShowCompletionPanel()
    {
        if (completionPanel != null) completionPanel.SetActive(true);
        if (completionTitleText != null) completionTitleText.text = "Mission Complete";
        if (completionBodyText != null)
            completionBodyText.text = completionTextData != null
                ? completionTextData.GetRandom()
                : string.Empty;

        if (questionPanel != null) questionPanel.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    private void HandleAllComplete()
    {
        Debug.Log("ATC Training Complete");
    }

    // ?? Vignette ?????????????????????????????????????????????????????

    private IEnumerator FlashVignette()
    {
        if (wrongVignette == null) yield break;

        float t = 0f;
        while (t < vignetteFadeInTime)
        {
            t += Time.deltaTime;
            wrongVignette.alpha = Mathf.Lerp(0f, 1f, t / vignetteFadeInTime);
            yield return null;
        }
        wrongVignette.alpha = 1f;

        yield return new WaitForSeconds(vignetteHoldTime);

        t = 0f;
        while (t < vignetteFadeOutTime)
        {
            t += Time.deltaTime;
            wrongVignette.alpha = Mathf.Lerp(1f, 0f, t / vignetteFadeOutTime);
            yield return null;
        }
        wrongVignette.alpha = 0f;
    }

    // ?? Helpers ??????????????????????????????????????????????????????

    private void HideAllPanels()
    {
        instructionPanel?.SetActive(false);
        groundIntroPanel?.SetActive(false);
        flightIntroPanel?.SetActive(false);
        advancedIntroPanel?.SetActive(false);
        questionPanel?.SetActive(false);
        feedbackPanel?.SetActive(false);
    }

    private void ClearResultIcons()
    {
        if (optionAResultIcon != null) optionAResultIcon.gameObject.SetActive(false);
        if (optionBResultIcon != null) optionBResultIcon.gameObject.SetActive(false);
    }

    private void ShowResultIcon(int selectedOption, bool correct)
    {
        Image icon = selectedOption == 0 ? optionAResultIcon : optionBResultIcon;
        Image other = selectedOption == 0 ? optionBResultIcon : optionAResultIcon;

        if (other != null) other.gameObject.SetActive(false);
        if (icon != null)
        {
            icon.sprite = correct ? correctIconSprite : wrongIconSprite;
            icon.gameObject.SetActive(true);
        }
    }

    private void SetAnswerButtonsInteractable(bool state)
    {
        if (optionAButton != null) optionAButton.interactable = state;
        if (optionBButton != null) optionBButton.interactable = state;
    }

    public void LockNav()
    {
        if (nextButton != null) nextButton.interactable = false;
        if (previousButton != null) previousButton.interactable = false;
    }

    public void UnlockNav()
    {
        if (nextButton != null) nextButton.interactable = scenarioManager.IsCompleted(scenarioManager.CurrentIndex);
        if (previousButton != null) previousButton.interactable = scenarioManager.CurrentIndex > 0;
    }
}