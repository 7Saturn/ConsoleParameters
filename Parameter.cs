using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public enum ParameterType {
    String,
    Integer,
    Uinteger,
    Boolean
}

public class Parameter {
    string parameterName; //Those must not contain the prefix --, - or /! Just like in the ParameterDefinition
    ParameterType type;
    uint numberOfValues;
    bool boolValue;
    string[] stringValues;
    uint[] uintValues;
    int[] intValues;
    bool isTainted = false;

    public Parameter(string newParameterName, // Only to be used to create boolean type parameters
                     bool newValue) {
        if (   newParameterName == null
            || newParameterName.Length < 1) {
            throw new ParameterNameRequiredException("For a parameter definition a parameter name of at least one character length is required!");
        }
        this.parameterName = newParameterName;
        this.type = ParameterType.Boolean;
        this.numberOfValues = 1;
        this.boolValue = newValue; //Funny thing is, Bools can never be tainted. Either they are present, or not. =)
    }

    // Das hier ist eigentlich Pferdescheiße! Die Exception kommt recht bald... Besser für die drei Typen selbst parsen

    public Parameter(ParameterDefinition pDef,
                     string[] values):this(pDef.getParameterName(),
                                           pDef.getType(),
                                           values) {
    }

    public Parameter(ParameterDefinition pDef,
                     string values):this(pDef.getParameterName(),
                                         pDef.getType(),
                                         valueSplit(values)) {
    }

    public Parameter(ParameterDefinition pDef) {// For parameters without values provided
        this.parameterName = pDef.getParameterName();
        this.type = pDef.getType();
        this.numberOfValues = 0;
        this.stringValues = null;
        this.uintValues = null;
        this.intValues = null;
        this.isTainted = true;
    }

    public Parameter(string newParameterName,
                     ParameterType newType,    // Will always be provided (cannot be defined as null by the caller), so no exception required!
                     string[] providedValues) {
        if (   newParameterName == null
            || newParameterName.Length < 1) {
            throw new ParameterNameRequiredException("For a parameter definition a parameter name of at least one character length is required!");
        }
        this.parameterName = newParameterName;
        this.type = newType;
        if (providedValues == null) {
            throw new ParameterValuesRequiredException("For a parameter a list of parameter values is required (at least an empty list)!");
        }

        this.numberOfValues = (uint) providedValues.Length;

        if (newType == ParameterType.String) {
            string[] tempStrings=new string[providedValues.Length];
            Array.Copy(providedValues, tempStrings, providedValues.Length);
            this.stringValues = tempStrings;
        }
        if (newType == ParameterType.Integer) {
            int[] intValues = new int[providedValues.Length];
            for (uint counter = 0; counter < providedValues.Length; counter++) {
                int tempValue;
                bool success = Int32.TryParse(providedValues[counter], out tempValue);
                if (!success) {
                    throw new ParameterTypeIntegerRequiredException("Parameter '" + newParameterName + "' could not be parsed as integer but is supposed to be one.");
                }
                intValues[counter] = tempValue;
            }
            this.intValues = intValues;
        }
        if (newType == ParameterType.Uinteger) {
            uint[] intValues = new uint[providedValues.Length];
            for (uint counter = 0; counter < providedValues.Length; counter++) {
                uint tempValue;
                bool success = UInt32.TryParse(providedValues[counter], out tempValue);
                if (!success) {
                    throw new ParameterTypeIntegerRequiredException("Parameter '" + newParameterName + "' could not be parsed as uinteger but is supposed to be one.");
                }
                intValues[counter] = tempValue;
            }
            this.uintValues = intValues;

        }
        if (newType == ParameterType.Boolean) {
            this.numberOfValues = 1;
            this.boolValue = true;
        }
        else {
            this.numberOfValues = (uint) providedValues.Length;
        }
    }

    public string getName() {
        return parameterName;
    }

    public ParameterType getType() {
        return type;
    }

    public uint getNumberOfValues() {
        return numberOfValues;
    }

    public bool getBoolValue() {
        if (type != ParameterType.Boolean) {
            throw new ParameterTypeWrongForGetting("Boolean parameter value requested but parameter is not of type Boolean.");
        }
        return boolValue;
    }

    public string[] getStringValues() {
        if (type != ParameterType.String) {
            throw new ParameterTypeWrongForGetting("String parameter values requested but parameter is not of type String.");
        }
        return stringValues;
    }

    public int[] getIntegerValues() {
        if (type != ParameterType.Integer) {
            throw new ParameterTypeWrongForGetting("Int parameter values requested but parameter is not of type Integer.");
        }
        return intValues;
    }

    public uint[] getUintegerValues() {
        if (type != ParameterType.Uinteger) {
            throw new ParameterTypeWrongForGetting("Uint parameter values requested but parameter is not of type Uinteger.");
        }
        return uintValues;
    }

    private static string[] valueSplit (string value) {
        List<string> list1 = new List<string>(Regex.Split(value, @"\s*,\s*"));
        List<string> list2 = new List<string>();
        foreach (string element in list1) {
            if (!element.Equals("")) list2.Add(element);
        }
        return list2.ToArray();
    }
}

public class ParameterValuesRequiredException : System.Exception {
    public ParameterValuesRequiredException() : base() { }
    public ParameterValuesRequiredException(string message) : base(message) { }
    public ParameterValuesRequiredException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterValuesRequiredException(System.Runtime.Serialization.SerializationInfo info,
                                               System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterNameRequiredException : System.Exception {
    public ParameterNameRequiredException() : base() { }
    public ParameterNameRequiredException(string message) : base(message) { }
    public ParameterNameRequiredException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterNameRequiredException(System.Runtime.Serialization.SerializationInfo info,
                                             System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterTypeIntegerRequiredException : System.Exception {
    public ParameterTypeIntegerRequiredException() : base() { }
    public ParameterTypeIntegerRequiredException(string message) : base(message) { }
    public ParameterTypeIntegerRequiredException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterTypeIntegerRequiredException(System.Runtime.Serialization.SerializationInfo info,
                                                    System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterTypeWrongForGetting : System.Exception {
    public ParameterTypeWrongForGetting() : base() { }
    public ParameterTypeWrongForGetting(string message) : base(message) { }
    public ParameterTypeWrongForGetting(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterTypeWrongForGetting(System.Runtime.Serialization.SerializationInfo info,
                                           System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
