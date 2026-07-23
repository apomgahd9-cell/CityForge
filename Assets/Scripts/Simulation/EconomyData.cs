using System;
using System.Collections.Generic;

[Serializable]
public class EconomyData
{
    public InitialState initialState;
    public BudgetCycle budgetCycle;
    public Dictionary<string, TaxPolicyItem> taxPolicy;
    public Revenue revenue;
    public Expenses expenses;
    public Loan loan;
    public FinancialState financialState;
}

[Serializable]
public class InitialState
{
    public float startingFunds;
}

[Serializable]
public class BudgetCycle
{
    public string period; // "monthly"
    public string description;
}

[Serializable]
public class TaxPolicyItem
{
    public float defaultRate;
    public float minRate;
    public float maxRate;
    public float rateStep;
}

[Serializable]
public class Revenue
{
    public TaxIncome taxIncome;
    public ServiceFees serviceFees;
}

[Serializable]
public class TaxIncome
{
    public Dictionary<string, TaxSource> sources;
}

[Serializable]
public class TaxSource
{
    public string function;
    public string metric;
    public float weight;
    public string taxSource;
}

[Serializable]
public class ServiceFees
{
    public bool enabled;
    public ServiceFee water;
    public ServiceFee electricity;
}

[Serializable]
public class ServiceFee
{
    public string function;
    public string metric;
    public float weight;
}

[Serializable]
public class Expenses
{
    public Dictionary<string, ServiceCost> serviceMaintenance;
    public RoadMaintenance roadMaintenance;
}

[Serializable]
public class ServiceCost
{
    public float baseCost;
    public Scaling scaling;
}

[Serializable]
public class Scaling
{
    public string metric;
    public float weight;
}

[Serializable]
public class RoadMaintenance
{
    public float costPerTile;
}

[Serializable]
public class Loan
{
    public float maxAmount;
    public float interestRate;
    public float minPayment;
}

[Serializable]
public class FinancialState
{
    public float bankruptcyThreshold;
}
