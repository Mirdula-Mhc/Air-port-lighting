using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;

public class VideoController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScenarioManager scenarioManager;

    [Header("Video UI")]
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Button startButton; // launch video button
    [SerializeField] private Button continueButton; // per-scenario continue button

    private bool isLaunchVideo = true; // true = startup video, false = per-scenario video

    private void Start()
    {
        if (startButton != null) startButton.gameObject.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (videoPanel != null) videoPanel.SetActive(true);

        startButton?.onClick.AddListener(OnStartPressed);
        continueButton?.onClick.AddListener(OnContinuePressed);

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoEnd;
            videoPlayer.Play();
        }
    }

    private void OnEnable()
    {
        if (scenarioManager != null)
            scenarioManager.OnVideoRequired += HandleVideoRequired;
    }

    private void OnDisable()
    {
        if (scenarioManager != null)
            scenarioManager.OnVideoRequired -= HandleVideoRequired;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoEnd;
    }

    // ?? Launch Video (startup) ????????????????????????????????????????

    private void OnVideoEnd(VideoPlayer vp)
    {
        if (isLaunchVideo)
        {
            if (startButton != null) startButton.gameObject.SetActive(true);
        }
        else
        {
            ScenarioData scenario = scenarioManager.GetScenario(scenarioManager.CurrentIndex);
            if (scenario.autoContinue)
            {
                OnContinuePressed();
                return;
            }
            if (scenario.showContinueButton && continueButton != null)
                continueButton.gameObject.SetActive(true);
        }
    }

    private void OnStartPressed()
    {
        if (videoPanel != null) videoPanel.SetActive(false);
        StartCoroutine(StartWithDelay());
    }

    private IEnumerator StartWithDelay()
    {
        yield return null; // one frame for all OnEnable subscriptions
        scenarioManager.ShowScenario(0);
    }

    // ?? Per-Scenario Video ????????????????????????????????????????????

    private void HandleVideoRequired(ScenarioData scenario, int index)
    {
        isLaunchVideo = false;

        if (videoPanel != null) videoPanel.SetActive(true);
        if (startButton != null) startButton.gameObject.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);

        if (videoPlayer != null && scenario.videoClip != null)
        {
            videoPlayer.clip = scenario.videoClip;
            videoPlayer.isLooping = false;
            videoPlayer.Play();
        }
    }

    private void OnContinuePressed()
    {
        if (videoPanel != null) videoPanel.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        scenarioManager.NotifyVideoComplete();
    }
}