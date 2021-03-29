public class ParameterDefinition {
    string parameterName; //No extension here! No --, - or /! Same goes for Parameter class
    bool isRequired;
    uint minValues;
    uint maxValues;
    bool noSplit; //If set, the comma-separated list is /not/ split. Imagine being provided a file name for opening and it actually /does/ contain a »,«. Or a double with thousand-separator »,«
    ParameterType type;
    string helpText; // A short description text for generated help text.
    public ParameterDefinition(string newParameterName,
                               ParameterType newType,
                               bool newIsRequired = false,
                               uint newMinValues = 0,
                               uint newMaxValues = 0,
                               bool newSplit = false,
                               string newHelpText = null) {
        if (   newParameterName == null
            || newParameterName.Length < 1) {
            throw new ParameterDefinitionNameRequiredException("For a parameter definition a parameter name of at least one character length is required!");
        }
        if (   newIsRequired
            && newType == ParameterType.Boolean) {
            throw new ParameterDefinitionRequiredException(newParameterName + ": Parameters of type Boolean may never be required Parameters!"); // That's the trick... Their presence in the provided parameters marks them as true, their absence marks them as false.
        }

        if (   newMinValues > 0
            && newMaxValues > 0
            && newMinValues > newMaxValues) {
            throw new ParameterDefinitionLimitsFaultyException(newParameterName + ": Max values must be at least as high as min values."); // If someone is really funny...
        }

        if (   newSplit
            && newMinValues > 1) {
            throw new ParameterDefinitionNoSplitMinNumberWrongException("ParameterDefinition for " + newParameterName + ": When making a parameter not to be split, the max number of values is 1. Minvalues > 1 makes no sense, as it will never result in untainted parameters."); // Not immediately obvious.
        }

        if (   newSplit
            && newType == ParameterType.Boolean) {
            throw new ParameterDefinitionNoSplitException("ParameterDefinition for " + newParameterName + ": When making a parameter not to be split, the type Boolean is not allowed."); // For Numbers that might make sense, as 1,000,000.5 may be interpreted as one million and a half. But what is there to be split for Bools?
        }

        this.parameterName = newParameterName;
        this.type = newType;
        this.isRequired = newIsRequired;
        this.minValues = newMinValues;
        this.maxValues = newMaxValues;
        this.noSplit = newSplit;
        this.helpText = newHelpText;
    }

    public string getParameterName() {
        return parameterName;
    }

    public ParameterType getType() {
        return type;
    }

    public bool getIsRequired() {
        return isRequired;
    }

    public uint getMinValues() {
        return minValues;
    }

    public uint getMaxValues() {
        return maxValues;
    }

    public bool getNoSplit() {
        return noSplit;
    }

    public string getHelpText() {
        return helpText;
    }
}

public class ParameterDefinitionNameRequiredException : System.Exception {
    public ParameterDefinitionNameRequiredException() : base() { }
    public ParameterDefinitionNameRequiredException(string message) : base(message) { }
    public ParameterDefinitionNameRequiredException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterDefinitionNameRequiredException(System.Runtime.Serialization.SerializationInfo info,
                                                       System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterDefinitionRequiredException : System.Exception {
    public ParameterDefinitionRequiredException() : base() { }
    public ParameterDefinitionRequiredException(string message) : base(message) { }
    public ParameterDefinitionRequiredException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterDefinitionRequiredException(System.Runtime.Serialization.SerializationInfo info,
                                                   System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterDefinitionLimitsFaultyException : System.Exception {
    public ParameterDefinitionLimitsFaultyException() : base() { }
    public ParameterDefinitionLimitsFaultyException(string message) : base(message) { }
    public ParameterDefinitionLimitsFaultyException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterDefinitionLimitsFaultyException(System.Runtime.Serialization.SerializationInfo info,
                                                       System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterDefinitionNoSplitMinNumberWrongException : System.Exception {
    public ParameterDefinitionNoSplitMinNumberWrongException() : base() { }
    public ParameterDefinitionNoSplitMinNumberWrongException(string message) : base(message) { }
    public ParameterDefinitionNoSplitMinNumberWrongException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterDefinitionNoSplitMinNumberWrongException(System.Runtime.Serialization.SerializationInfo info,
                                                                System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterDefinitionNoSplitException : System.Exception {
    public ParameterDefinitionNoSplitException() : base() { }
    public ParameterDefinitionNoSplitException(string message) : base(message) { }
    public ParameterDefinitionNoSplitException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterDefinitionNoSplitException(System.Runtime.Serialization.SerializationInfo info,
                                                  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
