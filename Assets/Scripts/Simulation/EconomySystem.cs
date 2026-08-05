using System.Collections.Generic;
using UnityEngine;

public class EconomySystem : MonoBehaviour, ISaveable
{
    public static EconomySystem Instance { get; private set; }

    private EconomyData economyData;
    public float CurrentFunds { get; private set; }
    private float outstandingLoan;

    private SimulationClock clock;
    private int lastBudgetMonth = -1;
    private int ticksPerGameDay = 24;
    private int gameDaysPerMonth = 30;

    public int LoadPriority => 200;

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
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.Register(this);

        economyData = JsonLoader.Load<EconomyData>("Data/Balance/economy");
        if (economyData == null)
        {
            Debug.LogError("economy.json not found. Economy system disabled.");
            enabled = false;
            return;
        }

        GameRulesData gameRules = JsonLoader.Load<GameRulesData>("Data/Core/GameRules");
        if (gameRules != null && gameRules.simulation != null)
        {
            ticksPerGameDay = gameRules.simulation.ticksPerGameDay;
            gameDaysPerMonth = gameRules.simulation.gameDaysPerMonth;
        }
        else
        {
            Debug.LogWarning("GameRules simulation data missing. Using default budget cycle.");
        }

        CurrentFunds = economyData.initialState.startingFunds;
        outstandingLoan = 0f;
        Debug.Log($"Economy Online. Starting Funds: {CurrentFunds}");

        clock = FindObjectOfType<SimulationClock>();
        if (clock != null)
            clock.OnTick += OnSimulationTick;
        else
            Debug.LogError("SimulationClock not found.");
    }

    private void OnDestroy()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.Unregister(this);

        if (clock != null)
            clock.OnTick -= OnSimulationTick;
    }

    private void OnSimulationTick(int tick)
    {
        int ticksPerMonth = ticksPerGameDay * gameDaysPerMonth;
        if (ticksPerMonth <= 0)
        {
            Debug.LogError("Invalid budget cycle configuration.");
            return;
        }

        int currentMonth = tick / ticksPerMonth;
        if (currentMonth != lastBudgetMonth)
        {
            lastBudgetMonth = currentMonth;
            CalculateMonthlyBudget();
        }
    }

    private void CalculateMonthlyBudget()
    {
        if (MetricsSystem.Instance == null) return;

        float revenue = CalculateRevenue();
        float expenses = CalculateExpenses();
        CurrentFunds += revenue - expenses;

        Debug.Log($"Month {lastBudgetMonth}: +{revenue:F1} -{expenses:F1} = Funds: {CurrentFunds:F1}");

        if (CurrentFunds <= economyData.financialState.bankruptcyThreshold)
            Debug.LogError("City is Bankrupt.");
    }

    private float CalculateRevenue()
    {
        float total = 0f;

        foreach (TaxSource source in economyData.revenue.taxIncome.sources.Values)
        {
            float metricVal = MetricsSystem.Instance.GetMetric(source.metric);
            float taxRate = GetCurrentTaxRate(source.taxSource);
            total += metricVal * source.weight * (taxRate / 100f);
        }

        // TODO: تفعيل رسوم الخدمات عند وجود نظام استهلاك
        return total;
    }

    private float CalculateExpenses()
    {
        float total = 0f;

        if (ServiceSystem.Instance != null)
            total += ServiceSystem.Instance.GetTotalUpkeep();
        else
            Debug.LogWarning("ServiceSystem not available for expense calculation.");

        if (RoadNetwork.Instance != null &&
            economyData.expenses != null &&
            economyData.expenses.roadMaintenance != null)
        {
            total += RoadNetwork.Instance.RoadCount *
                     economyData.expenses.roadMaintenance.costPerTile;
        }

        if (outstandingLoan > 0)
            total += outstandingLoan * economyData.loan.interestRate / 12f;

        return total;
    }

    private float GetCurrentTaxRate(string taxSource)
    {
        string[] parts = taxSource.Split('.');
        if (parts.Length == 2 && parts[0] == "taxPolicy")
        {
            if (economyData.taxPolicy.TryGetValue(parts[1], out TaxPolicyItem policy))
                return policy.defaultRate;
        }

        Debug.LogWarning($"Tax policy not found for: {taxSource}. Returning 0.");
        return 0f;
    }

    public bool CanAfford(float cost) => CurrentFunds >= cost;
    public void DeductFunds(float amount) => CurrentFunds -= amount;
    public void AddFunds(float amount) => CurrentFunds += amount;

    public float GetOutstandingLoan() => outstandingLoan;

    public void TakeLoan(float amount)
    {
        if (amount > economyData.loan.maxAmount - outstandingLoan)
        {
            Debug.LogWarning("Loan amount exceeds maximum.");
            return;
        }
        outstandingLoan += amount;
        CurrentFunds += amount;
    }

    public void RepayLoan(float amount)
    {
        amount = Mathf.Min(amount, outstandingLoan);
        outstandingLoan -= amount;
        CurrentFunds -= amount;
    }

    public void Save(SaveData data)
    {
        data.currentFunds = CurrentFunds;
        data.outstandingLoan = outstandingLoan;
    }

    public void Load(SaveData data)
    {
        CurrentFunds = data.currentFunds;
        outstandingLoan = data.outstandingLoan;
    }
}
