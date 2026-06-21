using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ScenarioManager : MonoBehaviour
{
    public enum FeedbackMode
    {
        // Mode B (old): short icon only, explanation is manual/optional via
        // the Explain button, animation is mandatory and runs right away.
        ManualExplainButton,

        // Mode A (new): explanation popup opens automatically with VO +
        // typewriter text, holds, auto-closes - THEN the mandatory
        // animation plays. No Explain button involved.
        AutoPopupTypewriter
    }

    [Header("Feedback Mode Toggle")]
    [Tooltip("Switch between the manual Explain-button flow (B) and the automatic popup/typewriter flow (A) without touching any wiring.")]
    [SerializeField] private FeedbackMode feedbackMode = FeedbackMode.ManualExplainButton;

    [Header("Scenario Data")]
    [SerializeField] private List<ScenarioData> scenarios;

    [Header("Signal Controllers")]
    [SerializeField] private LightGunController lightGun;
    [SerializeField] private FlareController flareController;

    [Header("Consequence Animation")]
    [Tooltip("The aircraft/scene Animator that all scenario animation triggers fire on. Lives here (not on ScenarioData) because it's a scene reference, not an asset reference.")]
    [SerializeField] private Animator animatorReference;

    [Header("Scene Context Transition (Animator-driven)")]
    // Direct Transform.position changes get overwritten every frame by the
    // Animator's own position curves, so the aircraft move into a new
    // context must be done as an animation trigger instead - same
    // mechanism as the consequence animations.
    [Tooltip("Trigger fired once when the first Flight-context scenario loads. Leave empty if not needed.")]
    [SerializeField] private string enterFlightTrigger;
    [Tooltip("Trigger fired once when the first Advanced-context scenario loads. Leave empty if not needed.")]
    [SerializeField] private string enterAdvancedTrigger;

    private SceneContext? lastContext;

    [Header("Instruction UI")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private TextMeshProUGUI instructionText;

    [Header("Scene Intro Panels (VO-only, no question, Next locked until VO ends)")]
    [Tooltip("Shown based on the scenario's existing Context dropdown (Ground/Flight/Advanced) - no new field needed.")]
    [SerializeField] private GameObject groundIntroPanel;
    [SerializeField] private TextMeshProUGUI groundIntroText;
    [SerializeField] private GameObject flightIntroPanel;
    [SerializeField] private TextMeshProUGUI flightIntroText;
    [SerializeField] private GameObject advancedIntroPanel;
    [SerializeField] private TextMeshProUGUI advancedIntroText;
    [SerializeField] private AudioSource sceneIntroAudioSource;

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

    [Header("Voice Over Audio (plays parallel to animation)")]
    [SerializeField] private AudioSource voAudioSource;

    [Header("Post-Answer Timing")]
    [Tooltip("Seconds to let the result icon sit before hiding the UI and playing the animation. (Mode B only)")]
    [SerializeField] private float resultIconHoldTime = 2.5f;

    [Header("Wrong Answer Vignette")]
    [Tooltip("Full-screen red Image (CanvasGroup or Image with alpha 0 by default) that flashes in/out on a wrong answer.")]
    [SerializeField] private CanvasGroup wrongVignette;
    [SerializeField] private float vignetteFadeInTime = 0.15f;
    [SerializeField] private float vignetteHoldTime = 0.2f;
    [SerializeField] private float vignetteFadeOutTime = 0.4f;

    [Header("Auto Popup / Typewriter Timing (Mode A only)")]
    [Tooltip("Seconds between each typed character.")]
    [SerializeField] private float typewriterCharDelay = 0.03f;
    [Tooltip("Seconds to hold the fully-typed explanation on screen before it auto-closes.")]
    [SerializeField] private float popupHoldTime = 2f;

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

        SnapAircraftIfContextChanged(scenario.context);

        HandleSignal(scenario);

        // NON-INTERACTIVE PAGE (no question)
        if (!scenario.requiresAnswer)
        {
            // Hide everything from both non-interactive sub-types first.
            if (instructionPanel != null) instructionPanel.SetActive(false);
            if (groundIntroPanel != null) groundIntroPanel.SetActive(false);
            if (flightIntroPanel != null) flightIntroPanel.SetActive(false);
            if (advancedIntroPanel != null) advancedIntroPanel.SetActive(false);
            if (questionPanel != null) questionPanel.SetActive(false);

            completedSteps[index] = true;

            if (scenario.pageType == NonInteractivePageType.InfoPanel)
            {
                // Small existing info panel - same as before, Next unlocks immediately.
                if (instructionPanel != null)
                    instructionPanel.SetActive(true);

                if (instructionText != null)
                    instructionText.text = scenario.instructorIntroLine;

                if (nextButton != null)
                    nextButton.interactable = true;

                return;
            }

            // SceneIntro - big per-context panel, Next gated until VO ends.
            GameObject activePanel = scenario.context switch
            {
                SceneContext.Ground => groundIntroPanel,
                SceneContext.Flight => flightIntroPanel,
                SceneContext.Advanced => advancedIntroPanel,
                _ => null
            };

            TextMeshProUGUI activeText = scenario.context switch
            {
                SceneContext.Ground => groundIntroText,
                SceneContext.Flight => flightIntroText,
                SceneContext.Advanced => advancedIntroText,
                _ => null
            };

            if (activePanel != null)
            {
                activePanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[ScenarioManager] No scene intro panel assigned for SceneContext.{scenario.context} - nothing will show on screen.");
            }

            if (activeText != null)
                activeText.text = scenario.instructorIntroLine;

            // Next stays disabled until the VO finishes playing.
            if (nextButton != null)
                nextButton.interactable = false;

            StartCoroutine(GateNextUntilIntroVOEnds(scenario));

            return;
        }

        // QUESTION PAGE
        if (instructionPanel != null)
            instructionPanel.SetActive(false);

        if (groundIntroPanel != null) groundIntroPanel.SetActive(false);
        if (flightIntroPanel != null) flightIntroPanel.SetActive(false);
        if (advancedIntroPanel != null) advancedIntroPanel.SetActive(false);

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
        // Only applies in Mode B - Mode A has no manual explain button.
        if (alreadyCompleted && feedbackMode == FeedbackMode.ManualExplainButton && explainButton != null)
        {
            lastAnswerCorrect = true;
            explainButton.gameObject.SetActive(true);
        }
    }

    private void SnapAircraftIfContextChanged(SceneContext newContext)
    {
        // Only fire when actually crossing into a new context - not on
        // every scenario within the same context (e.g. Ground A -> Ground B).
        if (lastContext.HasValue && lastContext.Value == newContext)
            return;

        lastContext = newContext;

        if (animatorReference == null)
        {
            Debug.LogWarning("[ScenarioManager] animatorReference is not assigned - cannot fire the scene transition trigger.");
            return;
        }

        string trigger = newContext switch
        {
            SceneContext.Flight => enterFlightTrigger,
            SceneContext.Advanced => enterAdvancedTrigger,
            _ => null // Ground is the starting context - nothing to transition into.
        };

        if (string.IsNullOrEmpty(trigger))
        {
            // Fine for Ground (no trigger needed), but worth flagging for
            // Flight/Advanced if someone forgot to assign one.
            if (newContext != SceneContext.Ground)
                Debug.LogWarning($"[ScenarioManager] No transition trigger assigned for entering {newContext} - aircraft position will not change.");
            return;
        }

        Debug.Log($"[ScenarioManager] Firing scene transition trigger '{trigger}' for entering {newContext}.");
        animatorReference.SetTrigger(trigger);
    }

    private System.Collections.IEnumerator GateNextUntilIntroVOEnds(ScenarioData scenario)
    {
        if (sceneIntroAudioSource != null && scenario.introVO != null)
        {
            sceneIntroAudioSource.PlayOneShot(scenario.introVO);
            Debug.Log($"[ScenarioManager] Scene intro VO playing ({scenario.introVO.length:0.00}s) - Next locked until it ends.");
            yield return new WaitForSeconds(scenario.introVO.length);
        }
        else
        {
            // No VO assigned - don't lock Next forever, just fall back to
            // a short minimum read time so the page isn't instantly skippable.
            Debug.LogWarning("[ScenarioManager] No intro VO assigned for this scene-intro page - using a 2s fallback before unlocking Next.");
            yield return new WaitForSeconds(2f);
        }

        if (nextButton != null)
            nextButton.interactable = true;
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

        // Explain button only exists in Mode B - Mode A pops up automatically.
        if (feedbackMode == FeedbackMode.ManualExplainButton && explainButton != null)
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
            if (optionAButton != null)
                optionAButton.interactable = false;

            if (optionBButton != null)
                optionBButton.interactable = false;

            if (feedbackMode == FeedbackMode.AutoPopupTypewriter)
                StartCoroutine(CorrectSequence_AutoPopup(scenario));
            else
                StartCoroutine(PlayResultSequence(scenario, correct: true));
        }
        else
        {
            // Buzz only on a wrong selection, per Mirdula's call.
            Handheld.Vibrate();

            StartCoroutine(FlashWrongVignette());

            if (feedbackMode == FeedbackMode.AutoPopupTypewriter)
            {
                StartCoroutine(WrongSequence_AutoPopup(scenario));
            }
            else
            {
                // Mode B: wrong-answer animation is optional. Only run the
                // full lock/hide/restore sequence if this scenario has one.
                if (!string.IsNullOrEmpty(scenario.wrongAnimTrigger))
                {
                    if (optionAButton != null)
                        optionAButton.interactable = false;

                    if (optionBButton != null)
                        optionBButton.interactable = false;

                    StartCoroutine(PlayResultSequence(scenario, correct: false));
                }
                // Otherwise: no lockout, user can simply try again immediately.
            }
        }
    }

    private System.Collections.IEnumerator FlashWrongVignette()
    {
        if (wrongVignette == null)
            yield break;

        // Fade in
        float t = 0f;
        while (t < vignetteFadeInTime)
        {
            t += Time.deltaTime;
            wrongVignette.alpha = Mathf.Lerp(0f, 1f, t / vignetteFadeInTime);
            yield return null;
        }
        wrongVignette.alpha = 1f;

        yield return new WaitForSeconds(vignetteHoldTime);

        // Fade out
        t = 0f;
        while (t < vignetteFadeOutTime)
        {
            t += Time.deltaTime;
            wrongVignette.alpha = Mathf.Lerp(1f, 0f, t / vignetteFadeOutTime);
            yield return null;
        }
        wrongVignette.alpha = 0f;
    }

    // ===================== MODE A: Auto Popup / Typewriter =====================

    private System.Collections.IEnumerator CorrectSequence_AutoPopup(ScenarioData scenario)
    {
        // 1) Explanation popup with parallel VO + typewriter text, auto-closes.
        yield return StartCoroutine(AutoExplainSequence(scenario, correct: true));

        // 2) Mandatory animation. Next stays disabled until this completes.
        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (previousButton != null)
            previousButton.interactable = false;

        if (nextButton != null)
            nextButton.interactable = false;

        // VO already played during the explanation popup above, so skip it here.
        yield return StartCoroutine(TriggerAnimationAndWait(scenario, correct: true, playVO: false));

        if (questionPanel != null)
            questionPanel.SetActive(true);

        if (previousButton != null)
            previousButton.interactable = currentIndex > 0;

        completedSteps[currentIndex] = true;

        if (nextButton != null)
            nextButton.interactable = true;
    }

    private System.Collections.IEnumerator WrongSequence_AutoPopup(ScenarioData scenario)
    {
        if (optionAButton != null)
            optionAButton.interactable = false;

        if (optionBButton != null)
            optionBButton.interactable = false;

        yield return StartCoroutine(AutoExplainSequence(scenario, correct: false));

        // Wrong answer: no mandatory animation, just let them retry.
        if (optionAButton != null)
            optionAButton.interactable = true;

        if (optionBButton != null)
            optionBButton.interactable = true;
    }

    private System.Collections.IEnumerator AutoExplainSequence(ScenarioData scenario, bool correct)
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);

        // No manual Continue button in Mode A - the popup closes itself.
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (questionPanel != null)
            questionPanel.SetActive(false);

        string line = correct ? scenario.instructorCorrectLine : scenario.instructorWrongLine;
        AudioClip vo = correct ? scenario.correctVO : scenario.wrongVO;

        // VO and typewriter start together, so the text reveals while the
        // line is being spoken.
        if (voAudioSource != null && vo != null)
            voAudioSource.PlayOneShot(vo);

        // Sync the typewriter speed to the VO clip's actual length so the
        // text finishes typing roughly when the audio finishes, instead of
        // using the fixed typewriterCharDelay (which was outpacing audio).
        float charDelay = typewriterCharDelay;
        if (vo != null && !string.IsNullOrEmpty(line))
            charDelay = vo.length / line.Length;

        yield return StartCoroutine(TypewriterReveal(feedbackDescriptionText, line, charDelay));

        yield return new WaitForSeconds(popupHoldTime);

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        if (questionPanel != null)
            questionPanel.SetActive(true);
    }

    private System.Collections.IEnumerator TypewriterReveal(TextMeshProUGUI target, string fullText, float delay)
    {
        if (target == null)
            yield break;

        target.text = string.Empty;

        if (string.IsNullOrEmpty(fullText))
            yield break;

        foreach (char c in fullText)
        {
            target.text += c;
            yield return new WaitForSeconds(delay);
        }
    }

    // Shared by both modes: actually fires the Animator trigger (if any)
    // and waits the configured duration. Used directly by Mode B's
    // PlayResultSequence, and by Mode A's CorrectSequence_AutoPopup.
    private System.Collections.IEnumerator TriggerAnimationAndWait(ScenarioData scenario, bool correct, bool playVO)
    {
        string trigger = correct ? scenario.correctAnimTrigger : scenario.wrongAnimTrigger;
        AnimationClip clip = correct ? scenario.correctAnimClip : scenario.wrongAnimClip;
        AudioClip vo = correct ? scenario.correctVO : scenario.wrongVO;

        bool hasAnimation = animatorReference != null && !string.IsNullOrEmpty(trigger);

        // Auto-deduct duration from the assigned clip. Falls back to a
        // small default only if no clip was assigned, so we never wait 0s.
        float duration = (clip != null) ? clip.length : 2f;

        if (hasAnimation)
        {
            Debug.Log($"[ScenarioManager] Animation STARTED: trigger='{trigger}' duration={duration:0.00}s on {animatorReference.name}");
            animatorReference.SetTrigger(trigger);

            if (clip == null)
                Debug.LogWarning($"[ScenarioManager] No AnimationClip assigned for trigger '{trigger}' - falling back to a {duration}s default wait. Assign the clip on the ScenarioData asset to auto-deduct the real duration.");
        }
        else
        {
            Debug.Log("[ScenarioManager] No animation assigned for this result - skipping trigger.");
        }

        if (playVO && voAudioSource != null && vo != null)
            voAudioSource.PlayOneShot(vo);

        yield return new WaitForSeconds(duration);

        if (hasAnimation)
            Debug.Log($"[ScenarioManager] Animation FINISHED: trigger='{trigger}'");
    }

    // ===================== MODE B: Manual Explain Button =====================

    private System.Collections.IEnumerator PlayResultSequence(ScenarioData scenario, bool correct)
    {
        // 1) Let the result icon sit on screen for a moment first.
        yield return new WaitForSeconds(resultIconHoldTime);

        // 2) Hide the question UI and lock both nav buttons so the user
        // can't skip ahead or back out mid-animation.
        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (previousButton != null)
            previousButton.interactable = false;

        if (nextButton != null)
            nextButton.interactable = false;

        // Force-close the explanation panel if it's open, and lock the
        // Explain button so the user can't pop it open mid-animation.
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        if (explainButton != null)
            explainButton.interactable = false;

        // 3) Fire the animation trigger (if assigned) and VO together.
        yield return StartCoroutine(TriggerAnimationAndWait(scenario, correct, playVO: true));

        // 4) Restore UI and re-enable navigation.
        if (questionPanel != null)
            questionPanel.SetActive(true);

        if (previousButton != null)
            previousButton.interactable = currentIndex > 0;

        if (explainButton != null)
            explainButton.interactable = true;

        if (correct)
        {
            completedSteps[currentIndex] = true;

            if (nextButton != null)
                nextButton.interactable = true;
        }
        else
        {
            // Wrong answer animation finished - let them try again.
            if (optionAButton != null)
                optionAButton.interactable = true;

            if (optionBButton != null)
                optionBButton.interactable = true;
        }
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

        // Mode B uses the manual Continue button - make sure it's visible
        // (Mode A hides it).
        if (continueButton != null)
            continueButton.gameObject.SetActive(true);

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