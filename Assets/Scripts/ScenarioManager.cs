using UnityEngine;
using System;
using System.Collections.Generic;

public class ScenarioManager : MonoBehaviour
{
    [Header("Scenario Data")]
    [SerializeField] private List<ScenarioData> scenarios;

    // Controllers subscribe to these
    public event Action<ScenarioData, int> OnScenarioLoaded;
    public event Action<ScenarioData, int> OnCorrectAnswer;
    public event Action<ScenarioData, int> OnWrongAnswer;
    public event Action<int> OnScenarioFirstCompleted; // fires ONLY on first completion, never on revisit
    public event Action OnAllScenariosComplete;
    public event Action OnIntroVOComplete;
    public event Action<ScenarioData, int> OnVideoRequired; // fires when scenario has a video

    private int currentIndex;
    private bool[] completedSteps;
    private bool[] audioPlayed;

    public int CurrentIndex => currentIndex;
    public bool IsCompleted(int index) => completedSteps != null && completedSteps[index];
    public bool AudioPlayed(int index) => audioPlayed != null && audioPlayed[index];
    public void MarkAudioPlayed(int index) => audioPlayed[index] = true;
    public ScenarioData GetScenario(int index) => scenarios[index];
    public int ScenarioCount => scenarios.Count;

    private void Start()
    {
        completedSteps = new bool[scenarios.Count];
        audioPlayed = new bool[scenarios.Count];
        // VideoController calls ShowScenario(0) after launch video ends
    }

    public void ShowScenario(int index)
    {
        if (index < 0 || index >= scenarios.Count) return;
        currentIndex = index;
        ScenarioData scenario = scenarios[index];

        // If scenario has a video, pause here and let VideoController handle it
        // VideoController calls NotifyVideoComplete() when done
        if (scenario.playVideo)
        {
            OnVideoRequired?.Invoke(scenario, index);
            return;
        }

        OnScenarioLoaded?.Invoke(scenario, index);
    }

    // Called by VideoController when video finishes and user continues
    public void NotifyVideoComplete()
    {
        OnScenarioLoaded?.Invoke(scenarios[currentIndex], currentIndex);
    }

    public void SelectAnswer(int selectedAnswer)
    {
        ScenarioData scenario = scenarios[currentIndex];
        bool correct = selectedAnswer == scenario.correctOptionIndex;

        if (correct)
            OnCorrectAnswer?.Invoke(scenario, selectedAnswer);
        else
            OnWrongAnswer?.Invoke(scenario, selectedAnswer);
    }

    public void NotifyIntroVOComplete()
    {
        OnIntroVOComplete?.Invoke();
    }

    // AnimationController passes back the index it started with
    // Stale coroutines that finish after page change are ignored
    public void NotifyCorrectAnimComplete(int scenarioIndex)
    {
        if (scenarioIndex != currentIndex)
        {
            Debug.Log($"[ScenarioManager] Ignoring stale anim complete for index {scenarioIndex}, current is {currentIndex}");
            return;
        }

        if (!completedSteps[scenarioIndex])
        {
            completedSteps[scenarioIndex] = true;
            OnScenarioFirstCompleted?.Invoke(scenarioIndex);
        }
    }

    public void GoNext()
    {
        ScenarioData current = scenarios[currentIndex];
        if (current.requiresAnswer && !completedSteps[currentIndex]) return;

        if (currentIndex < scenarios.Count - 1)
            ShowScenario(currentIndex + 1);
        else
            OnAllScenariosComplete?.Invoke();
    }

    public void GoPrevious()
    {
        if (currentIndex > 0)
            ShowScenario(currentIndex - 1);
    }
}