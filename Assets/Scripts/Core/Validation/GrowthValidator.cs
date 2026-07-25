using System.Collections.Generic;

public static class GrowthValidator
{
    public static ValidationResult Validate(GrowthProfileWrapper profiles, GrowthModelWrapper models, ServicesData services)
    {
        var result = new ValidationResult();

        if (profiles == null || profiles.profiles == null)
            result.AddError("GrowthValidator", "Growth profiles data is null.");
        if (models == null || models.models == null)
            result.AddError("GrowthValidator", "Growth models data is null.");
        if (result.HasErrors)
            return result;

        HashSet<string> validServiceIds = services?.services != null
            ? new HashSet<string>(services.services.Keys)
            : null;

        foreach (var profile in profiles.profiles.Values)
        {
            if (profile.stages == null) continue;

            foreach (var stage in profile.stages)
            {
                ValidateRules(stage.rules, validServiceIds, result);
                ValidateRules(stage.declineRules, validServiceIds, result);

                if (!string.IsNullOrEmpty(stage.growthModel) && !models.models.ContainsKey(stage.growthModel))
                    result.AddError("GrowthValidator", $"Growth model '{stage.growthModel}' not found.");
                if (!string.IsNullOrEmpty(stage.declineModel) && !models.models.ContainsKey(stage.declineModel))
                    result.AddError("GrowthValidator", $"Decline model '{stage.declineModel}' not found.");
            }
        }

        return result;
    }

    private static void ValidateRules(List<GrowthRule> rules, HashSet<string> validServiceIds, ValidationResult result)
    {
        if (rules == null) return;

        foreach (var rule in rules)
        {
            switch (rule.type)
            {
                case "service_available":
                    if (string.IsNullOrWhiteSpace(rule.serviceId))
                        result.AddError("GrowthValidator", "Rule 'service_available' missing serviceId.");
                    else if (validServiceIds != null && !validServiceIds.Contains(rule.serviceId))
                        result.AddError("GrowthValidator", $"Service '{rule.serviceId}' not found in services.json.");
                    break;

                case "metric_min":
                case "metric_max":
                    if (string.IsNullOrWhiteSpace(rule.metric))
                        result.AddError("GrowthValidator", $"Rule '{rule.type}' missing metric.");
                    break;
            }
        }
    }
}
