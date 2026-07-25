public static class EconomyValidator
{
    public static ValidationResult Validate(EconomyData data)
    {
        var result = new ValidationResult();

        if (data == null)
        {
            result.AddError("EconomyValidator", "Economy data is null.");
            return result;
        }

        if (data.initialState == null || data.initialState.startingFunds < 0)
            result.AddError("EconomyValidator", "Starting funds is missing or negative.");

        if (data.taxPolicy != null)
        {
            foreach (var kvp in data.taxPolicy)
            {
                var policy = kvp.Value;
                if (policy.defaultRate < policy.minRate || policy.defaultRate > policy.maxRate)
                    result.AddError("EconomyValidator", $"Tax policy '{kvp.Key}' default rate out of bounds.");
                if (policy.minRate < 0 || policy.maxRate > 100)
                    result.AddError("EconomyValidator", $"Tax policy '{kvp.Key}' min/max rate invalid.");
            }
        }

        if (data.loan != null)
        {
            if (data.loan.interestRate < 0 || data.loan.interestRate > 1)
                result.AddWarning("EconomyValidator", $"Loan interest rate {data.loan.interestRate} unusual.");
            if (data.loan.maxAmount < 0)
                result.AddError("EconomyValidator", "Loan max amount is negative.");
        }

        if (data.financialState != null && data.financialState.bankruptcyThreshold > 0)
            result.AddWarning("EconomyValidator", "Bankruptcy threshold is positive (should be negative).");

        return result;
    }
}
