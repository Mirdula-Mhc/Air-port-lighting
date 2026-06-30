using UnityEngine;
using System;
using System.Collections.Generic;

public class ScenarioManager : MonoBehaviour
{
    [Header("Scenario Data")]
    [SerializeField] private List<ScenarioData> scenarios;

    // Events — all controllers listen to these
    public event Action<ScenarioData, int> OnScenarioLoaded;   // scenario, index
    public event Action<ScenarioData, int> OnCorrectAnswer;  // scenario, selectedIndex
    public event Action<ScenarioData, int> OnWrongAnswer;    // scenario, selectedIndex
    public event Action OnCorrectAnimComplete;
    public event Action OnAllScenariosComplete;

    private int currentIndex;
    private bool[] completedSteps;
    private bool[] audioPlayed;

    // Read-only access for controllers that need to query state
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
        ShowScenario(0);
    }

    public void ShowScenario(int index)
    {
        if (index < 0 || index >= scenarios.Count) return;
        currentIndex = index;
        OnScenarioLoaded?.Invoke(scenarios[index], index);
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

    public void NotifyCorrectAnimComplete()
    {
        completedSteps[currentIndex] = true;
        OnCorrectAnimComplete?.Invoke();
    }

    public void GoNext()
    {
        if (!completedSteps[currentIndex]) return;

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