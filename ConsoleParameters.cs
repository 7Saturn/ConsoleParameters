using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices; //Caller-Info

public static class ConsoleParameters {
    private static string ownFileName = Environment.GetCommandLineArgs()[0].Replace(Directory.GetCurrentDirectory(), ".");
    private static string parameterPrefix;
    private static bool wasInitializedFlag = false;

    private static List<Parameter> listOfParameters = new List<Parameter>(); //This is where the actual data will be put, that could be derived from the provided parameters. If something smells fishy, this should indicate it:
    private static bool isTainted = false; //If something goes wrong during parsing, this flag can be queried very easily (instead of having to recheck all those lists shown above).

    private static List<ParameterDefinition> listOfParameterDefinitions = new List<ParameterDefinition>(); //If the programmer provided a faulty definition for initialization, he's not supposed to just catch that exception , but do it properly instead. So if he survives the checks, this list will only contain proper definitions.
    private static List<ParameterDefinition> doubledParameterDefinitions = new List<ParameterDefinition>(); // Entered by the programmer into the definition at least twice

    //The parameter names here will have to include the prefix (--, - or /), in contrast to ParameterDefinitions and Parameters.
    private static List<string> doubledParameterNamesInDefinition = new List<string>(); //The parameters provided by the Programmer at least twice
    private static List<string> allowedParameterNames = new List<string>(); //The parameters allowed according to the parameter definition provided

    private static List<string> allProvidedParameterNames = new List<string>(); // All parameters provided by the user, but not values (e. g. »--test« but not »test«)
    private static List<string> allowedProvidedParameterNames = new List<string>(); // All parameters provided by the user, but not values (e. g. »--test« but not »test«), that are acutally allowed by the definition
    private static List<string> doubledParameterNames = new List<string>(); //The parameters provided by the user at least twice
    private static List<string> missingParameterNames = new List<string>(); // All parameters present in the definition /not/ provided by the user
    private static List<string> unknownParameterNames = new List<string>(); // All parameters provided by the user /not/ present in the definition, aka unknown parameters
    private static List<string> missingButRequiredParameterNames = new List<string>(); //All parameters omitted by the user but required of him
    private static List<string> residualArgs; //All the rest or args after removing all parameters and their values. This allowes things like 7za a archivename.7z -mx0, getting »a« as command, »archivename.7z« as archive name and still leave -mx0 as a parameter name that can be present or not.

    public static void InitializeParameters(string newParameterPrefix,
                                            ParameterDefinition[] newParameterDefinitions,
                                            string[] args) {

        //This must be allowed only once
        if (wasInitializedFlag) throw new ParameterAlreadyInitializedException();

        //Does the prefix check out?
        if (   newParameterPrefix == null
            || !(   newParameterPrefix.Equals("-")
                 || newParameterPrefix.Equals("--")
                 || newParameterPrefix.Equals("/"))) {
            throw new ParameterPrefixFaultyException("Parameter initialisation failed. The parameter prefix must be '-', '--' or '/' (e. g. --parametername means '--'). You used: '" + newParameterPrefix + "'.");
        }
        ConsoleParameters.parameterPrefix = newParameterPrefix;

        //Some general checks, just to make sure, no bullshit was defined by the programmer.

        //Do the definitions check out?
        if (   newParameterDefinitions == null
            || newParameterDefinitions.Length < 1) {
            throw new ParameterPrefixFaultyException("Parameter initialisation failed. The parameter definition array must provide at least one parameter definition. If you only want the provided words from the console, don't use this class, take args directly.");
        }

        foreach (ParameterDefinition pDef in newParameterDefinitions) {
            if (pDef == null) {
                throw new ParameterDefinitionNullException("A parameter definition provided is null.");
            }
            string newParameterName = withPrefix(pDef.getParameterName());
            if (allowedParameterNames.Contains(newParameterName)) {
                if (!doubledParameterNamesInDefinition.Contains(newParameterName)) {
                    doubledParameterNamesInDefinition.Add(newParameterName);
                    doubledParameterDefinitions.Add(pDef);
                }
            }
            else {
                listOfParameterDefinitions.Add(pDef);
                allowedParameterNames.Add(newParameterName);
            }
        }
        if (doubledParameterDefinitions.Count > 0) {
            string parameterNames = null;
            foreach (string parameterName in doubledParameterNamesInDefinition) {
                if (parameterNames == null) {
                    parameterNames = parameterName;
                }
                else {
                    parameterNames += "\n" + parameterName;
                }
            }
            throw new ParameterNameDoubledException("Parameter initialisation failed. The following parameter names are present at least twice in the parameter definition:\n" + parameterNames);
        }

        //And of course the actual arguments have to be OK, too.
        if (args == null) {
            throw new ParameterArgsMissingException("Parameter initialization failed. The parameter string array must provide at least an empty string array (usually that is 'args' provided to Main).");
        }

        //So from here on, all parameter definition values are OK. Let's see, what the user provided on the console...

        residualArgs = new List<string>();

        List<ParameterDefinition> boolParameterDefinitions     = new List<ParameterDefinition>(); // Only Bool Parameters
        List<ParameterDefinition> nonBoolParameterDefinitions  = new List<ParameterDefinition>(); // Those require values, in contrast to the bools, which are the value by themselves.
        List<ParameterDefinition> stringParameterDefinitions   = new List<ParameterDefinition>(); // These will return a list of strings
        List<ParameterDefinition> integerParameterDefinitions  = new List<ParameterDefinition>(); // These will return a list of integer values
        List<ParameterDefinition> uintegerParameterDefinitions = new List<ParameterDefinition>(); // These will return a list of unsigned integer values
        List<ParameterDefinition> requiredParameterDefinitions = new List<ParameterDefinition>(); // These will have to be provided by the user

        foreach (ParameterDefinition pDef in newParameterDefinitions) {
            if (pDef.getType() == ParameterType.Boolean) boolParameterDefinitions.Add(pDef);
            if (pDef.getType() == ParameterType.String) stringParameterDefinitions.Add(pDef);
            if (pDef.getType() == ParameterType.Integer) integerParameterDefinitions.Add(pDef);
            if (pDef.getType() == ParameterType.Uinteger) uintegerParameterDefinitions.Add(pDef);
            if (pDef.getType() != ParameterType.Boolean) nonBoolParameterDefinitions.Add(pDef);
            if (pDef.getIsRequired()) requiredParameterDefinitions.Add(pDef);
        }
        // Are there required parameters missing?
        foreach (ParameterDefinition missingCandidate in requiredParameterDefinitions) {
            string fullName = withPrefix(missingCandidate.getParameterName());

            if (!Array.Exists(args, element => element == fullName)) {
                missingButRequiredParameterNames.Add(fullName);
            }
        }

        if (missingButRequiredParameterNames.Count > 0) ConsoleParameters.isTainted = true; // If so, that's bad!

        //What Parameters are missing in general?
        foreach (ParameterDefinition pDef in newParameterDefinitions) {
            string fullName = withPrefix(pDef.getParameterName());
            if (!Array.Exists(args, element => element == fullName)) {
                missingParameterNames.Add(fullName);
            }
        }

        // No parameters entered twice by the user? En passant we also collect all values, that look like parameters.
        foreach (string parameterName in args) {
            if (hasPrefix(parameterName)) {// So is actually a parameter name
                if (allProvidedParameterNames.Contains(parameterName)) {
                    if (!doubledParameterNames.Contains(parameterName)) {
                        doubledParameterNames.Add(parameterName);
                    }
                }
                else {
                    allProvidedParameterNames.Add(parameterName);
                }
            }
        }

        if (doubledParameterNames.Count > 0) ConsoleParameters.isTainted = true; //If so, that's bad, too!

        foreach (string parameterName in allProvidedParameterNames) {
            if (!allowedParameterNames.Contains(parameterName)) { //That's not part of the definition!
                unknownParameterNames.Add(parameterName);
            }
            else {
                allowedProvidedParameterNames.Add(parameterName);
            }
        }
        if (unknownParameterNames.Count > 0) ConsoleParameters.isTainted = true; //And that's bad.



        /*
          Checks to be implemented:
          * Checking for missing Parameter values
          * Removing all strings that are part of a parameter name and value, returning the residual strings as well somehow
          * Checking for parameter contents.
            * Bools are always fine, check for all of them, present = true, not present = false
            * all the rest will have to be analyzed. Format: --parameter "Values,comma,separated", "\s*,\s*" as separator. No multiple strings for one parameter during entry! If the user wants to provide multiple values, he shall take commas and if neccessary, inch-characters.
          * Create Parameter report (missing, faulty). The parameter itself should know what's wrong with it.
            * Wrong number of arguments, doubled,
        */

        foreach (ParameterDefinition boolParameterDefinition in boolParameterDefinitions) {
            string parameterName = withPrefix(boolParameterDefinition.getParameterName());
            Parameter newBool;
            if (allowedProvidedParameterNames.Contains(parameterName)) {
                newBool = new Parameter(boolParameterDefinition.getParameterName(), true); //Present, so flag will be set.
            }
            else {
                newBool = new Parameter(boolParameterDefinition.getParameterName(), false); //Not present, so flag unset.
            }
            listOfParameters.Add(newBool);
        }
        foreach (ParameterDefinition pDef in newParameterDefinitions) {
            string fullParamName = newParameterPrefix + pDef.getParameterName();
            Parameter newParameter = null;
            if (pDef.getType() == ParameterType.Boolean) {
                newParameter = new Parameter(pDef.getParameterName(),
                                             Array.Exists(args, element => element == fullParamName));
            }
            if (pDef.getType() == ParameterType.String) {
                newParameter = new Parameter(pDef.getParameterName(),
                                             ParameterType.String,
                                             args);
            }
            /*            if (pDef.getType() == ParameterType.Integer) {
                newParameter = new Parameter();
            }
            if (pDef.getType() == ParameterType.Uinteger) {
                newParameter = new Parameter();
            }*/
            if (newParameter != null) listOfParameters.Add(newParameter);
        }
        ConsoleParameters.wasInitializedFlag = true;
    }

    public static string GetApplicationFileName() {
        ensureInitializationDone();
        return ownFileName;
    }

    public static string getStartCommand() {
        ensureInitializationDone();
        string currentSystemType = System.Environment.OSVersion.Platform.ToString();
        if (currentSystemType.Equals("Unix")) {
            return "mono " + ownFileName;
        } else if (currentSystemType.Equals("Win32NT")) {
            return ownFileName;
        } else {
            Console.WriteLine("System: '{0}'", currentSystemType);
            return ownFileName;
        }
    }

    public static List<string> getMissingParameterNames() {
        ensureInitializationDone();
        return missingParameterNames;
    }

    public static List<string> getMissingButRequiredParameterNames() {
        ensureInitializationDone();
        return missingButRequiredParameterNames;
    }


    public static bool getIsTainted() {
        ensureInitializationDone();
        return isTainted;
    }

    public static bool wasInitialized() {
        return wasInitializedFlag;
    }

    public static string getParameterPrefix() {
        ensureInitializationDone();
        return parameterPrefix;
    }

    public static List<Parameter> getParameters() {
        ensureInitializationDone();
        return ConsoleParameters.listOfParameters;
    }

    public static List<string> getDoubledParameterNames() {
        ensureInitializationDone();
        return ConsoleParameters.doubledParameterNames;
    }

    public static List<string> getUnknownParameterNames() {
        ensureInitializationDone();
        return ConsoleParameters.unknownParameterNames;
    }

    public static List<string> getAllProvidedParameterNames() {
        ensureInitializationDone();
        return ConsoleParameters.allProvidedParameterNames;
    }

    public static List<string> getAllowedProvidedParameterNames() {
        ensureInitializationDone();
        return ConsoleParameters.allowedProvidedParameterNames;
    }

    public static Parameter getParameterByName(string parameterName) {
        ensureInitializationDone();
        foreach (Parameter currentParameter in listOfParameters) {
            if (currentParameter.getName().Equals(parameterName)) return currentParameter;
        }
        return null;
    }

    public static ParameterDefinition getParameterDefinitionByName(string parameterName) {
        ensureInitializationDone();
        foreach (ParameterDefinition currentParameter in listOfParameterDefinitions) {
            if (currentParameter.getParameterName().Equals(parameterName)) return currentParameter;
        }
        return null;
    }

    private static void ensureInitializationDone() {
        if (!ConsoleParameters.wasInitializedFlag) {
            throw new ParameterUninitializedException();
        }
    }

    public static bool parameterWasFound(string parameterName) {
        ensureInitializationDone();
        Parameter result = getParameterByName(parameterName);
        return result != null;
    }

    public static bool parameterIsSet(string parameterName) {
        ensureInitializationDone();
        Parameter parameter = getParameterByName(parameterName);
        if (parameter != null) {
            if (parameter.getType() == ParameterType.Boolean) {
                return true; //Bools are by definition always set, but either true or false.
            }
            else {
                return parameter.getNumberOfValues() != 0;
            }
        }
        else {
            throw new ParameterNotFoundException("Parameter " + parameterName + "could not be found.");
        }
    }

    private static string withPrefix(string parameterName) {
        if (hasPrefix(parameterName)) {
            return parameterName;
        }
        else {
            return ConsoleParameters.parameterPrefix + parameterName;
        }
    }

    private static string withoutPrefix(string parameter) {
        if (hasPrefix(parameter)) {
            return parameter.Substring(ConsoleParameters.parameterPrefix.Length);
        }
        else {
            return parameter;
        }
    }

    private static bool hasPrefix(string value) {
        return (   value.Length > ConsoleParameters.parameterPrefix.Length
                && value.Substring(0,
                                   ConsoleParameters.parameterPrefix.Length).Equals(ConsoleParameters.parameterPrefix));
    }

}

public class ParameterPrefixFaultyException : System.Exception {
    public ParameterPrefixFaultyException() : base() { }
    public ParameterPrefixFaultyException(string message) : base(message) { }
    public ParameterPrefixFaultyException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterPrefixFaultyException(System.Runtime.Serialization.SerializationInfo info,
                                             System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterDefinitionMissingException : System.Exception {
    public ParameterDefinitionMissingException() : base() {
        throw new ParameterPrefixFaultyException("The parameters were not initialized, yet. You must initialize the parameters first, before doing anything else with them."); }
    public ParameterDefinitionMissingException(string message) : base(message) { }
    public ParameterDefinitionMissingException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterDefinitionMissingException(System.Runtime.Serialization.SerializationInfo info,
                                                  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterUninitializedException : System.Exception {
    public ParameterUninitializedException() : base() { }
    public ParameterUninitializedException(string message) : base(message) { }
    public ParameterUninitializedException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterUninitializedException(System.Runtime.Serialization.SerializationInfo info,
                                              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterArgsMissingException : System.Exception {
    public ParameterArgsMissingException() : base() { }
    public ParameterArgsMissingException(string message) : base(message) { }
    public ParameterArgsMissingException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterArgsMissingException(System.Runtime.Serialization.SerializationInfo info,
                                            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterNameDoubledException : System.Exception {
    public ParameterNameDoubledException() : base() { }
    public ParameterNameDoubledException(string message) : base(message) { }
    public ParameterNameDoubledException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterNameDoubledException(System.Runtime.Serialization.SerializationInfo info,
                                            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterNotFoundException : System.Exception {
    public ParameterNotFoundException() : base() { }
    public ParameterNotFoundException(string message) : base(message) { }
    public ParameterNotFoundException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterNotFoundException(System.Runtime.Serialization.SerializationInfo info,
                                         System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterDefinitionNullException : System.Exception {
    public ParameterDefinitionNullException() : base() { }
    public ParameterDefinitionNullException(string message) : base(message) { }
    public ParameterDefinitionNullException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterDefinitionNullException(System.Runtime.Serialization.SerializationInfo info,
                                               System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ParameterAlreadyInitializedException : System.Exception {
    public ParameterAlreadyInitializedException() : base() {
        throw new ParameterAlreadyInitializedException("Console parameters were already initialized.");
    }
    public ParameterAlreadyInitializedException(string message) : base(message) { }
    public ParameterAlreadyInitializedException(string message, System.Exception inner) : base(message, inner) { }

    protected ParameterAlreadyInitializedException(System.Runtime.Serialization.SerializationInfo info,
                                                   System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
