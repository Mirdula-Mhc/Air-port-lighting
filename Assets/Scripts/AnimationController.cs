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
    private Coroutine currentAnimCoroutine;

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

    private void HandleScenarioLoaded(ScenarioData scenario, int index)
    {
        // Kill stale anim — prevents NotifyCorrectAnimComplete firing on wrong scenario
        if (currentAnimCoroutine != null)
        {
            StopCoroutine(currentAnimCoroutine);
            currentAnimCoroutine = null;
        }
        SnapAircraftIfContextChanged(scenario.context);
    }

    private void HandleCorrectAnswer(ScenarioData scenario, int selectedIndex)
    {
        int capturedIndex = scenarioManager.CurrentIndex; // capture NOW before any async gap
        currentAnimCoroutine = StartCoroutine(PlayAnimAndNotify(scenario, true, capturedIndex));
    }

    private void HandleWrongAnswer(ScenarioData scenario, int selectedIndex)
    {
        if (string.IsNullOrEmpty(scenario.wrongAnimTrigger)) return;
        int capturedIndex = scenarioManager.CurrentIndex;
        currentAnimCoroutine = StartCoroutine(PlayAnimAndNotify(scenario, false, capturedIndex));
    }

    private IEnumerator PlayAnimAndNotify(ScenarioData scenario, bool correct, int scenarioIndex)
    {
        string trigger = correct ? scenario.correctAnimTrigger : scenario.wrongAnimTrigger;
        AnimationClip clip = correct ? scenario.correctAnimClip : scenario.wrongAnimClip;

        if (animatorReference == null || string.IsNullOrEmpty(trigger))
        {
            if (correct) scenarioManager.NotifyCorrectAnimComplete(scenarioIndex);
            yield break;
        }

        animatorReference.SetTrigger(trigger);
        float duration = clip != null ? clip.length : 2f;
        yield return new WaitForSeconds(duration);

        if (correct)
            scenarioManager.NotifyCorrectAnimComplete(scenarioIndex);
    }

    private void SnapAircraftIfContextChanged(SceneContext newContext)
    {
        if (lastContext.HasValue && lastContext.Value == newContext) return;
        lastContext = newContext;
        if (animatorReference == null) return;

        string trigger = newContext switch
        {
            SceneContext.Flight => enterFlightTrigger,
            SceneContext.Advanced => enterAdvancedTrigger,
            _ => null
        };

        if (!string.IsNullOrEmpty(trigger))
            animatorReference.SetTrigger(trigger);
    }
}