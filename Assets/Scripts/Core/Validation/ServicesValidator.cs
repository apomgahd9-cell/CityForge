using System.Collections.Generic;

public static class ServicesValidator
{
    public static ValidationResult Validate(ServicesData data)
    {
        var result = new ValidationResult();

        if (data == null || data.services == null || data.services.Count == 0)
        {
            result.AddError("ServicesValidator", "Services data is null or empty.");
            return result;
        }

        var seenIds = new HashSet<string>();
        var validOperations = new HashSet<string> { "add", "multiply", "set" };
        var validCoverageTypes = new HashSet<string> { "radius", "network" };

        foreach (var kvp in data.services)
        {
            var def = kvp.Value;

            if (string.IsNullOrWhiteSpace(def.id))
            {
                result.AddError("ServicesValidator", "A service definition is missing its 'id'.");
                continue;
            }

            if (!seenIds.Add(def.id))
            {
                result.AddError("ServicesValidator", $"Duplicate service id: '{def.id}'.");
                continue;
            }

            if (def.effect == null || string.IsNullOrWhiteSpace(def.effect.metric))
                result.AddError("ServicesValidator", $"Service '{def.id}' is missing effect.metric.");

            if (def.effect != null && !validOperations.Contains(def.effect.operation))
                result.AddError("ServicesValidator", $"Service '{def.id}' has invalid operation: '{def.effect.operation}'.");

            if (!validCoverageTypes.Contains(def.coverageType))
                result.AddError("ServicesValidator", $"Service '{def.id}' has invalid coverageType: '{def.coverageType}'.");
        }

        return result;
    }
}
