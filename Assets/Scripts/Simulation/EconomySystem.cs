using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// يدير ميزانية المدينة: الإيرادات (الضرائب)، المصاريف (الخدمات)، والقروض.
/// يعتمد على EconomyData من economy.json. يُحدّث الميزانية مرة شهرياً.
/// </summary>
public class EconomySystem : MonoBehaviour
{
    public static EconomySystem Instance { get; private set; }

    [SerializeField] private EconomyData economyData;
    public float CurrentFunds { get; private set; }

    private SimulationClock clock;
    private int lastBudgetMonth = -1;

    private int ticksPerGameDay = 24;
    private int gameDaysPerMonth = 30;

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
        economyData = JsonLoader.Load<EconomyData>("Data/Balance/economy");
        if (economyData == null)
        {
            Debug.LogError("economy.json not found! Economy system disabled.");
            enabled = false;
            return;
        }

        var gameRules = JsonLoader.Load<GameRulesData>("Data/Core/GameRules");
        if (gameRules != null && gameRules.simulation != null)
        {
            ticksPerGameDay = gameRules.simulation.ticksPerGameDay;
            gameDaysPerMonth = gameRules.simulation.gameDaysPerMonth;
        }
        else
        {
            Debug.LogWarning("GameRules simulation data missing. Using default values for budget cycle.");
        }

        CurrentFunds = economyData.initialState.startingFunds;
        Debug.Log($"Economy Online. Starting Funds: {CurrentFunds}");

        clock = FindObjectOfType<SimulationClock>();
        if (clock != null)
        {
            clock.OnTick += OnSimulationTick;
        }
        else
        {
            Debug.LogError("SimulationClock not found!");
        }
    }

    private void OnDestroy()
    {
        if (clock != null)
            clock.OnTick -= OnSimulationTick;
    }

    private void OnSimulationTick(int tick)
    {
        int ticksPerMonth = ticksPerGameDay * gameDaysPerMonth;

        if (ticksPerMonth <= 0)
        {
            Debug.LogError("Invalid budget cycle configuration in GameRules. ticksPerGameDay and gameDaysPerMonth must be positive.");
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

        CurrentFunds += (revenue - expenses);
        Debug.Log($"💰 Month {lastBudgetMonth}: +{revenue:F1} -{expenses:F1} = Funds: {CurrentFunds:F1}");

        if (CurrentFunds <= economyData.financialState.bankruptcyThreshold)
        {
            Debug.LogError("💸 City is Bankrupt!");
        }
    }

    private float CalculateRevenue()
    {
        float total = 0;

        foreach (var source in economyData.revenue.taxIncome.sources.Values)
        {
            float metricVal = MetricsSystem.Instance.GetMetric(source.metric);
            float taxRate = GetCurrentTaxRate(source.taxSource);
            total += metricVal * source.weight * (taxRate / 100f);
        }

        if (economyData.revenue.serviceFees != null && economyData.revenue.serviceFees.enabled)
        {
            if (economyData.revenue.serviceFees.water != null)
                total += MetricsSystem.Instance.GetMetric(economyData.revenue.serviceFees.water.metric) * economyData.revenue.serviceFees.water.weight;
            if (economyData.revenue.serviceFees.electricity != null)
                total += MetricsSystem.Instance.GetMetric(economyData.revenue.serviceFees.electricity.metric) * economyData.revenue.serviceFees.electricity.weight;
        }

        return total;
    }

    private float CalculateExpenses()
    {
        float total = 0;

        // 1. مصاريف الخدمات المبنية على ServiceSystem (المصدر الحي الوحيد)
        if (ServiceSystem.Instance != null)
        {
            total += ServiceSystem.Instance.GetTotalUpkeep();
        }
        else
        {
            Debug.LogWarning("ServiceSystem not available for expense calculation.");
        }

        // 2. بنود مصاريف أخرى من economy.json (إن وجدت مستقبلاً)
        // TODO: يمكن إضافة مصاريف إدارية أو عامة غير مرتبطة بالخدمات هنا
        // مثال: total += economyData.expenses.administrationBaseCost;

        // 3. صيانة الطرق (جاهز للربط مستقبلاً)
        // TODO: ربط صيانة الطرق بـ RoadSystem لحساب عدد البلاطات
        // total += roadTileCount * economyData.expenses.roadMaintenance.costPerTile;

        return total;
    }

    private float GetCurrentTaxRate(string taxSource)
    {
        string[] parts = taxSource.Split('.');
        if (parts.Length == 2 && parts[0] == "taxPolicy")
        {
            if (economyData.taxPolicy.TryGetValue(parts[1], out var policy))
                return policy.defaultRate;
        }
        
        Debug.LogWarning($"Tax policy not found for source: {taxSource}. Returning 0.");
        return 0f;
    }

    public bool CanAfford(float cost) => CurrentFunds >= cost;
    public void DeductFunds(float amount) => CurrentFunds -= amount;
    public void AddFunds(float amount) => CurrentFunds += amount;
}
