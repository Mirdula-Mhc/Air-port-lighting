using UnityEngine;
using System.Collections;

public class AnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScenarioManager scenarioManager;
    [SerializeField] private Animator animatorReference;

    [Header("Scene Context Transitions")]
    [SerializeField] private string enterFlightTrigger;
    [SerializeField] private string enterAdvancedTrigger;

    private SceneContext? lastContext;

    private Coroutine animationRoutine;

    private void OnEnable()
    {
        scenarioManager.OnScenarioLoaded += HandleScenarioLoaded;
        scenarioManager.OnCorrectAnswer += HandleCorrectAnswer;
        scenarioManager.OnWrongAnswer += HandleWrongAnswer;
    }

    private void OnDisable()
    {
        scenarioManager.OnScenarioLoaded -= HandleScenarioLoaded;
        scenarioManager.OnCorrectAnswer -= HandleCorrectAnswer;
        scenarioManager.OnWrongAnswer -= HandleWrongAnswer;
    }

    // ------------------------------------------------------------------------

    private void HandleScenarioLoaded(ScenarioData scenario, int index)
    {
        SnapContextTransition(scenario.context);

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
    }

    // ------------------------------------------------------------------------

    private void HandleCorrectAnswer(ScenarioData scenario, int selectedIndex)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(
            PlayAnimation(
                scenario,
                true,
                scenarioManager.CurrentIndex));
    }

    private void HandleWrongAnswer(ScenarioData scenario, int selectedIndex)
    {
        if (string.IsNullOrEmpty(scenario.wrongAnimTrigger))
            return;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(
            PlayAnimation(
                scenario,
                false,
                scenarioManager.CurrentIndex));
    }

    // ------------------------------------------------------------------------

    private IEnumerator PlayAnimation(
        ScenarioData scenario,
        bool correct,
        int scenarioIndex)
    {
        if (animatorReference == null)
        {
            if (correct)
                scenarioManager.NotifyCorrectAnimComplete(scenarioIndex);

            yield break;
        }

        string trigger = correct
            ? scenario.correctAnimTrigger
            : scenario.wrongAnimTrigger;

        AnimationClip clip = correct
            ? scenario.correctAnimClip
            : scenario.wrongAnimClip;

        if (!string.IsNullOrEmpty(trigger))
            animatorReference.SetTrigger(trigger);

        float waitTime = 0.5f;

        if (clip != null)
            waitTime = clip.length;

        yield return new WaitForSeconds(waitTime);

        animationRoutine = null;

        // Only correct answers complete the scenario.
        if (correct)
            scenarioManager.NotifyCorrectAnimComplete(scenarioIndex);
    }

    // ------------------------------------------------------------------------

    private void SnapContextTransition(SceneContext context)
    {
        if (lastContext.HasValue &&
            lastContext.Value == context)
            return;

        lastContext = context;

        if (animatorReference == null)
            return;

        switch (context)
        {
            case SceneContext.Flight:

                if (!string.IsNullOrEmpty(enterFlightTrigger))
                    animatorReference.SetTrigger(enterFlightTrigger);

                break;

            case SceneContext.Advanced:

                if (!string.IsNullOrEmpty(enterAdvancedTrigger))
                    animatorReference.SetTrigger(enterAdvancedTrigger);

                break;
        }
    }
}