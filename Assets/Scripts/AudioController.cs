using UnityEngine;
using System.Collections;

public class AudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScenarioManager scenarioManager;
    [SerializeField] private UIController uiController;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource voAudioSource;
    [SerializeField] private AudioSource feedbackAudioSource;
    [SerializeField] private AudioSource introAudioSource;

    [Header("Feedback Clips")]
    [SerializeField] private AudioClip correctChime;
    [SerializeField] private AudioClip wrongChime;

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
        if (scenario.requiresAnswer) return;
        if (scenarioManager.AudioPlayed(index))
        {
            uiController.UnlockNav();
            return;
        }

        StartCoroutine(PlayIntroVOAndUnlock(scenario, index));
    }

    private IEnumerator PlayIntroVOAndUnlock(ScenarioData scenario, int index)
    {
        scenarioManager.MarkAudioPlayed(index);
        uiController.LockNav();

        if (introAudioSource != null && scenario.introVO != null)
        {
            introAudioSource.PlayOneShot(scenario.introVO);
            yield return new WaitForSeconds(scenario.introVO.length);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        uiController.UnlockNav();
    }

    private void HandleCorrectAnswer(ScenarioData scenario, int selectedIndex)
    {
        if (feedbackAudioSource != null && correctChime != null)
            feedbackAudioSource.PlayOneShot(correctChime);

        if (voAudioSource != null && scenario.correctVO != null)
            voAudioSource.PlayOneShot(scenario.correctVO);
    }

    private void HandleWrongAnswer(ScenarioData scenario, int selectedIndex)
    {
        if (feedbackAudioSource != null && wrongChime != null)
            feedbackAudioSource.PlayOneShot(wrongChime);

        if (voAudioSource != null && scenario.wrongVO != null)
            voAudioSource.PlayOneShot(scenario.wrongVO);
    }
}