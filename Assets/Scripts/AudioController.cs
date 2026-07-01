using UnityEngine;
using System.Collections;

public class AudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScenarioManager scenarioManager;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource feedbackAudioSource;
    [SerializeField] private AudioSource introAudioSource;

    [Header("Feedback Clips")]
    [SerializeField] private AudioClip correctChime;
    [SerializeField] private AudioClip wrongChime;

    private Coroutine currentVOCoroutine;

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
        // Stop stale VO coroutine
        if (currentVOCoroutine != null)
        {
            StopCoroutine(currentVOCoroutine);
            currentVOCoroutine = null;
        }

        // Stop any playing intro audio
        if (introAudioSource != null) introAudioSource.Stop();

        if (scenario.requiresAnswer) return;

        // Revisit — notify immediately after one frame so UIController has time to lock nav
        if (scenarioManager.AudioPlayed(index))
        {
            currentVOCoroutine = StartCoroutine(NotifyAfterFrame());
            return;
        }

        currentVOCoroutine = StartCoroutine(PlayIntroVO(scenario, index));
    }

    private IEnumerator NotifyAfterFrame()
    {
        yield return null; // one frame so UIController.HandleNonInteractivePage finishes locking first
        scenarioManager.NotifyIntroVOComplete();
    }

    private IEnumerator PlayIntroVO(ScenarioData scenario, int index)
    {
        if (introAudioSource != null && scenario.introVO != null)
        {
            introAudioSource.PlayOneShot(scenario.introVO);
            yield return new WaitForSeconds(scenario.introVO.length);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        scenarioManager.MarkAudioPlayed(index);
        scenarioManager.NotifyIntroVOComplete();
    }

    private void HandleCorrectAnswer(ScenarioData scenario, int selectedIndex)
    {
        // Stop intro VO immediately
        if (introAudioSource != null) introAudioSource.Stop();
        // Only chime — VO is played by UIController inside the feedback popup
        if (feedbackAudioSource != null && correctChime != null)
            feedbackAudioSource.PlayOneShot(correctChime);
    }

    private void HandleWrongAnswer(ScenarioData scenario, int selectedIndex)
    {
        // Stop intro VO immediately
        if (introAudioSource != null) introAudioSource.Stop();
        // Only chime — VO is played by UIController inside the feedback popup
        if (feedbackAudioSource != null && wrongChime != null)
            feedbackAudioSource.PlayOneShot(wrongChime);
    }
}