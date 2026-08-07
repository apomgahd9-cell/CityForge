using System.Collections.Generic;
using UnityEngine;

public class CitizenScheduleSystem : MonoBehaviour
{
    public static CitizenScheduleSystem Instance { get; private set; }

    private Dictionary<Citizen, int> workArrivalTick = new Dictionary<Citizen, int>();
    private SimulationClock clock;
    private int workDurationTicks = 192; // 8 أيام لعب (24 tick/يوم × 8)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        clock = FindObjectOfType<SimulationClock>();
        if (clock != null)
            clock.OnTick += OnSimulationTick;
    }

    private void OnDestroy()
    {
        if (clock != null)
            clock.OnTick -= OnSimulationTick;
    }

    public void RegisterAtWork(Citizen citizen)
    {
        if (citizen == null) return;
        workArrivalTick[citizen] = clock != null ? clock.CurrentTick : 0;
        Debug.Log($"{citizen.id} arrived at work at tick {workArrivalTick[citizen]}");
    }

    private void OnSimulationTick(int tick)
    {
        var toSendHome = new List<Citizen>();

        foreach (var kvp in workArrivalTick)
        {
            if (tick - kvp.Value >= workDurationTicks)
            {
                toSendHome.Add(kvp.Key);
            }
        }

        foreach (Citizen citizen in toSendHome)
        {
            workArrivalTick.Remove(citizen);
            SendHome(citizen);
        }
    }

    private void SendHome(Citizen citizen)
    {
        if (CitizenController.Instance == null) return;

        if (citizen.homeBuilding == null)
        {
            Debug.LogWarning($"Cannot send {citizen.id} home: home building is missing.");
            return;
        }

        CitizenState previousState = citizen.state;
        citizen.state = CitizenState.GoingHome;

        bool pathFound = CitizenController.Instance.RequestPath(citizen, citizen.homeBuilding.Position);

        if (pathFound)
        {
            Debug.Log($"{citizen.id} is going home");
        }
        else
        {
            citizen.state = previousState;
            Debug.LogWarning($"Failed to find home path for {citizen.id}");
        }
    }
}
