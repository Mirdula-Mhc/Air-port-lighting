using UnityEngine;
using System.Collections;

public class AircraftController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScenarioManager scenarioManager;

    [Header("Aircraft Root")]
    [SerializeField] private GameObject aircraftRoot;

    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 1f;

    private Coroutine currentMoveCoroutine;

    private void OnEnable()
    {
        scenarioManager.OnWrongAnswer += HandleWrongAnswer;
    }

    private void OnDisable()
    {
        scenarioManager.OnWrongAnswer -= HandleWrongAnswer;
    }

    private void HandleWrongAnswer(ScenarioData scenario, int selectedIndex)
    {
        if (scenario.aircraftAnchor == null) return;

        // Stop previous move before starting new one
        if (currentMoveCoroutine != null)
        {
            StopCoroutine(currentMoveCoroutine);
            currentMoveCoroutine = null;
        }

        currentMoveCoroutine = StartCoroutine(MoveToAnchor(scenario.aircraftAnchor));
    }

    private IEnumerator MoveToAnchor(Transform anchor)
    {
        if (aircraftRoot == null) yield break;

        Vector3 startPos = aircraftRoot.transform.position;
        Quaternion startRot = aircraftRoot.transform.rotation;
        Vector3 targetPos = anchor.position;
        Quaternion targetRot = anchor.rotation;

        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, t / moveDuration);
            aircraftRoot.transform.position = Vector3.Lerp(startPos, targetPos, progress);
            aircraftRoot.transform.rotation = Quaternion.Slerp(startRot, targetRot, progress);
            yield return null;
        }

        aircraftRoot.transform.position = targetPos;
        aircraftRoot.transform.rotation = targetRot;
        currentMoveCoroutine = null;
    }
}