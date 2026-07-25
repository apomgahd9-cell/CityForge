using System.Collections.Generic;

public class ValidationResult
{
    public List<ValidationMessage> Messages { get; private set; } = new List<ValidationMessage>();

    public bool HasErrors
    {
        get
        {
            foreach (var msg in Messages)
                if (msg.Severity == ValidationSeverity.Error)
                    return true;
            return false;
        }
    }

    public bool IsValid => !HasErrors;

    public void AddError(string source, string message)
    {
        Messages.Add(new ValidationMessage(ValidationSeverity.Error, source, message));
    }

    public void AddWarning(string source, string message)
    {
        Messages.Add(new ValidationMessage(ValidationSeverity.Warning, source, message));
    }

    public void AddInfo(string source, string message)
    {
        Messages.Add(new ValidationMessage(ValidationSeverity.Info, source, message));
    }

    public void Merge(ValidationResult other)
    {
        if (other != null)
            Messages.AddRange(other.Messages);
    }
}

public enum ValidationSeverity
{
    Info,
    Warning,
    Error
}

public class ValidationMessage
{
    public ValidationSeverity Severity { get; private set; }
    public string Source { get; private set; }
    public string Message { get; private set; }

    public ValidationMessage(ValidationSeverity severity, string source, string message)
    {
        Severity = severity;
        Source = source;
        Message = message;
    }

    public override string ToString()
    {
        return $"[{Severity}] {Source}: {Message}";
    }
}
