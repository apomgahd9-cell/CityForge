using UnityEngine;

public static class DataValidator
{
    public static ValidationResult ValidateAll(
        ServicesData services,
        EconomyData economy,
        GrowthProfileWrapper profiles,
        GrowthModelWrapper models)
    {
        var result = new ValidationResult();

        result.Merge(ServicesValidator.Validate(services));
        result.Merge(EconomyValidator.Validate(economy));
        result.Merge(GrowthValidator.Validate(profiles, models, services));

        foreach (var msg in result.Messages)
        {
            switch (msg.Severity)
            {
                case ValidationSeverity.Error:
                    Debug.LogError(msg.ToString());
                    break;
                case ValidationSeverity.Warning:
                    Debug.LogWarning(msg.ToString());
                    break;
                default:
                    Debug.Log(msg.ToString());
                    break;
            }
        }

        if (result.IsValid)
            Debug.Log("✅ All data validation passed.");
        else
            Debug.LogError("❌ Data validation completed with errors.");

        return result;
    }
}
