public class ParameterDefinition {
    string parameterName; //No extension here! No --, - or /! Same goes for Parameter class
    bool isRequired;
    uint minValues; //This...
    uint maxValues; //...and this are currently not used. But they ought to limit what the user can provide, at some point. This requires enhancing the Parameter Class with corresponding Checks
    ParameterType type;
    public ParameterDefinition(string newParameterName,
                               ParameterType newType,
                               bool newIsRequired = false,
                               uint newMinValues = 0,
                               uint newMaxValues = 999) {
        if (   newParameterName == null
            || newParameterName.Length < 1) {
            throw new ParameterDefinitionNameRequiredException("For a parameter definition a parameter name of at least one character length is required!");
        }
        if (   newIsRequired
            && newType == ParameterType.Boolean) {
            throw new ParameterDefinitionRequiredException("Parameters of type Boolean may never be required Parameters!"); // That's the trick... Their presence in the provided parameters marks them as true, their absence marks them as false.
        }
        this.parameterName = newParameterName;
        this.type = newType;
        this.isRequired = newIsRequired;
        this.minValues = newMinValues;
        this.maxValues = newMaxValues;
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
}

public class ParameterDefinitionNameRequiredException : System.Exception
{
    public ParameterDefinitionNameRequiredException() : base() { }
    public ParameterDefinitionNameRequiredException(string message) : base(message) { }
    public ParameterDefinitionNameRequiredException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterDefinitionNameRequiredException(System.Runtime.Serialization.SerializationInfo info,
                                                       System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterDefinitionRequiredException : System.Exception
{
    public ParameterDefinitionRequiredException() : base() { }
    public ParameterDefinitionRequiredException(string message) : base(message) { }
    public ParameterDefinitionRequiredException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterDefinitionRequiredException(System.Runtime.Serialization.SerializationInfo info,
                                                   System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
