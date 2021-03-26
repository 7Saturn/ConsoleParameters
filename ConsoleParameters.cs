using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices; //Caller-Info

/* Ideas for expansion:

 * Automatic Help-Text generation: ParameterDefinition gets a description text and a summary text is provided, too. This way a generic Help-String can be generated, including how to run the command with optional and required parameters, description of parameter's effect and a overall description, what the program does or requires. Can even be automatically invoked, if the Parameters were tainted, somehow.
 * Parameters with some requirements for the number of values (e. g. 2-4 values are required, no more, no less)
 * Special requirements for the provided values might as well be checked by a CallBack/Delegate, that does checks on them.
 * A more explicit check of what might be wrong with a parameter, would be nice: List of enum values that show, what went on, e. g., number of values wrong, values missing, parsing error, parameter missing, parameter was provided more than once.

*/


public static class ConsoleParameters {
    private static string ownFileName = Environment.GetCommandLineArgs()[0].Replace(Directory.GetCurrentDirectory() + "/", ""); //Only the file name, e. g. TestParam.exe
    private static string parameterPrefix;
    private static bool wasInitializedFlag = false; // Important, so that nobody tries bullshitting around with it, before it was set up properly.

    private static List<Parameter> listOfParameters = new List<Parameter>(); //This is where the actual data will be put, that could be derived from the provided parameters. If something smells fishy, this should indicate it:
    private static bool isTainted = false; //If something goes wrong during parsing, this flag can be queried very easily (instead of having to recheck all those lists shown above).

    private static List<ParameterDefinition> listOfParameterDefinitions = new List<ParameterDefinition>(); //If the programmer provided a faulty definition for initialization, he's not supposed to just catch that exception , but do it properly instead. So if he survives the checks, this list will only contain proper definitions.
    private static List<ParameterDefinition> doubledParameterDefinitions = new List<ParameterDefinition>(); // Entered by the programmer into the definition at least twice

    // The parameter names here will have to include the prefix (--, - or /), in contrast to ParameterDefinitions and Parameters.
    private static List<string> doubledParameterNamesInDefinition = new List<string>(); //The parameters provided by the Programmer at least twice
    private static List<string> allowedParameterNames = new List<string>(); //The parameters allowed according to the parameter definition provided

    private static List<string> allProvidedParameterNames = new List<string>(); // All parameters provided by the user, but not values (e. g. »--test« but not »test«)
    private static List<string> allowedProvidedParameterNames = new List<string>(); // All parameters provided by the user, but not values (e. g. »--test« but not »test«), that are acutally allowed by the definition
    private static List<string> doubledParameterNames = new List<string>(); // The parameters provided by the user at least twice
    private static List<string> missingParameterNames = new List<string>(); // All parameters present in the definition /not/ provided by the user
    private static List<string> missingValueParameterNames = new List<string>(); // All parameters present in the definition and used by the user, but without values
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

        // Some general checks, just to make sure, no bullshit was defined *by the programmer*.

        // Do the definitions check out?
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

        // And of course the actual arguments provided have to be OK, too.
        if (args == null) throw new ParameterArgsMissingException("Parameter initialization failed. The parameter string array must provide at least an empty string array (usually that is 'args' provided to Main).");

        //So from here on, all ParameterDefinitions are OK. Let's see, what the user provided on the console...
        residualArgs = new List<string>();

        List<ParameterDefinition> boolParameterDefinitions     = new List<ParameterDefinition>(); // Only Bool ParameterDefinitions, which will not require any provided values. Their presence (or lack of it) already gives you true or false
        List<ParameterDefinition> nonBoolParameterDefinitions  = new List<ParameterDefinition>(); // Those require values, in contrast to the bools, which are the value by themselves.
        List<ParameterDefinition> requiredParameterDefinitions = new List<ParameterDefinition>(); // These parameters will have to be provided by the user

        // Let's fill those three. These lists will make our work easier later
        foreach (ParameterDefinition pDef in newParameterDefinitions) {
            if (pDef.getType() == ParameterType.Boolean) {
                boolParameterDefinitions.Add(pDef);
            }
            else {
                nonBoolParameterDefinitions.Add(pDef);
            }
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

        // Which Parameters are missing in general?
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
                    if (!doubledParameterNames.Contains(parameterName)) doubledParameterNames.Add(parameterName);
                }
                else {
                    allProvidedParameterNames.Add(parameterName);
                }
            }
        }

        if (doubledParameterNames.Count > 0) ConsoleParameters.isTainted = true; //If so, that's bad, too!

        foreach (string parameterName in allProvidedParameterNames) {
            if (!allowedParameterNames.Contains(parameterName)) { // That one is not part of the definition!
                unknownParameterNames.Add(parameterName);
            }
            else {
                allowedProvidedParameterNames.Add(parameterName);
            }
        }

        if (unknownParameterNames.Count > 0) ConsoleParameters.isTainted = true; //And that's bad.

        List<string> allowedBoolParameterNames = new List<string>(); // Bools allowed for the user

        foreach (ParameterDefinition boolParameterDefinition in boolParameterDefinitions) {
            string parameterName = withPrefix(boolParameterDefinition.getParameterName());
            allowedBoolParameterNames.Add(parameterName);
            Parameter newBool;
            if (allowedProvidedParameterNames.Contains(parameterName)) { // A Bool defined in the Definitions and given by the user
                newBool = new Parameter(boolParameterDefinition.getParameterName(), true); //Present, so flag will be set.
            }
            else {// A Bool defined in the Defintions, but not give by the user
                newBool = new Parameter(boolParameterDefinition.getParameterName(), false); //Not present, so flag unset.
            }
            listOfParameters.Add(newBool);
        }

        //Remove unknown and bool Parameters, which leaves only »real« Parameters and their values.
        List<string> resArgs = new List<string>();

        //Let's remove Bools, as they were already completed and unknown parameters, as we cannot do anything with them anyways. The rest we work with.
        foreach (string arg in args) {
            if (!(   allowedBoolParameterNames.Contains(arg)
                  || unknownParameterNames.Contains(arg))){
                resArgs.Add(arg);
            }
        }

        //So from here on, »only« an analysis of Parameter-value-pairs is required.
        while (resArgs.Count > 0) {
            if ((resArgs.Count) == 1) { //Only one, so there cannot be any values provided.
                if (!hasPrefix(resArgs[0])) {// just another lone (residual) value, that's OK.
                    residualArgs.Add(resArgs[0]);
                }
                else { // Last one is a parameter name, so no values.
                    ParameterDefinition defectOne = getParameterDefinitionByNameInternal(withoutPrefix(resArgs[0]));
                    listOfParameters.Add(new Parameter (defectOne, true));
                    ConsoleParameters.isTainted = true;
                    missingValueParameterNames.Add(defectOne.getParameterName());
                }
                resArgs.RemoveAt(0);
            }
            else {
                //at least two values are present. First one ought to be the parameter name, second the value. Let's check it out!
                if (!hasPrefix(resArgs[0])) { //It's a value, no parameter name. Next!
                    residualArgs.Add(resArgs[0]);
                    resArgs.RemoveAt(0);
                }
                else {
                    //So the first one is a parameter name. Second should not be a parameter name!
                    if (!hasPrefix(resArgs[1])) {// Bingo! Second one holds values
                        ParameterDefinition theRightOne = getParameterDefinitionByNameInternal(withoutPrefix(resArgs[0]));
                        listOfParameters.Add(new Parameter (theRightOne, resArgs[1]));
                        resArgs.RemoveAt(0);
                        resArgs.RemoveAt(0);
                    }
                    else {// That's bad. Both are parameter names... But as both must be allowed ones, the first one simply lacks values... Next!
                        ParameterDefinition defectOne = getParameterDefinitionByNameInternal(withoutPrefix(resArgs[0]));
                        listOfParameters.Add(new Parameter (defectOne, true));
                        resArgs.RemoveAt(0);
                        ConsoleParameters.isTainted = true;
                        missingValueParameterNames.Add(defectOne.getParameterName());
                    }

                }
            }
        }

        //All parameters defined but not provided and not required can be added as OKish but with no values. Let the programmer decide, what to do with those missing ones...
        foreach (ParameterDefinition pDef in newParameterDefinitions) {
            if (!allProvidedParameterNames.Contains(withPrefix(pDef.getParameterName()))) {
                listOfParameters.Add(new Parameter(pDef, false));
            }
        }

        // If only one of them is faulty, the entire list must be marked faulty as well!
        foreach (Parameter toCheck in listOfParameters) {
            if (toCheck.getIsTainted()) ConsoleParameters.isTainted = true;
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

    public static List<string> getMissingValueParameterNames() {
        ensureInitializationDone();
        return missingValueParameterNames;
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

    private static ParameterDefinition getParameterDefinitionByNameInternal(string parameterName) {
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

    public static List<string> getResidualArgs() {
        return residualArgs;
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

    public static void dumpListOfParameters () {
        foreach (Parameter p in listOfParameters) {
            Console.WriteLine(p.ToString());
        }
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
