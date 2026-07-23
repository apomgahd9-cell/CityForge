using System;
using UnityEngine;

public class SimulationClock : MonoBehaviour
{
    [Header("Simulation Settings")]
    [SerializeField] private float tickIntervalSeconds = 1.0f;

    public int CurrentTick { get; private set; } = 0;

    public bool IsPaused { get; set; } = false;

    public event Action<int> OnTick;

    private float timer;

    private void Start()
    {
        timer = tickIntervalSeconds;
    }

    private void Update()
    {
        if (IsPaused)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer += tickIntervalSeconds;

            CurrentTick++;

            Debug.Log($"Simulation Tick: {CurrentTick}");

            OnTick?.Invoke(CurrentTick);
        }
    }

    public void SetTickInterval(float seconds)
    {
        tickIntervalSeconds = Mathf.Max(0.01f, seconds);
    }
}
