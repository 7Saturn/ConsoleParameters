using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices; //Caller-Info
using System.Threading;

/* Ideas for expansion:

 * Name space for ConsoleParameters.
 * A more explicit information, what went wrong with the provided arguments from the user during initialisation (doubled parameters provided, missing parameters, unknown parameter names) on a more generalized level (not only per parameter but somewhere, e. g. at least one parameter was missing, at least one parameter had to few values, etc.). Right now tainted for ConsoleParameters only means something went wrong but you have to find out what it was. Checking all the before mentioned and all parameters for faults is a bit tiresome.
 * Getter for list of parameters with help texts alone, helps creating a more personal version of the help without a lack of nice formatting.
 * New parameter that overrides standard parameter list in command line call example (because sometimes the interactions of parameters are a bit more complex than just being optional all by themselves).

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
    private static List<string> allowedProvidedParameterNames = new List<string>(); // All parameters provided by the user, but not values (e. g. »--test« but not »test«), that are actually allowed by the definition
    private static List<string> doubledParameterNames = new List<string>(); // The parameters provided by the user at least twice
    private static List<string> missingParameterNames = new List<string>(); // All parameters present in the definition /not/ provided by the user
    private static List<string> missingValueParameterNames = new List<string>(); // All parameters present in the definition and used by the user, but without values
    private static List<string> unknownParameterNames = new List<string>(); // All parameters provided by the user /not/ present in the definition, aka unknown parameters
    private static List<string> missingButRequiredParameterNames = new List<string>(); //All parameters omitted by the user but required of him
    private static List<string> residualArgs; //All the rest or args after removing all parameters and their values. This allowes things like 7za a archivename.7z -mx0, getting »a« as command, »archivename.7z« as archive name and still leave -mx0 as a parameter name that can be present or not.
    private static string helpText; //This is the text being printed between the generic console call shown and the list of parameters. Should describe what the program is supposed to do and stuff like that. The actual parameters are /not/ to be described here, unless it helps for understanding!
    private static bool autoHelp = false;
    private static uint maxWidth = (uint) Console.WindowWidth; //This *will* fail on Moba X Term.

    public static void InitializeParameters(string newParameterPrefix,
                                            ParameterDefinition[] newParameterDefinitions,
                                            string[] args,
                                            string description = null,
                                            bool autoHelpOnError = false) {
        helpText = description;
        autoHelp = autoHelpOnError;
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
            throw new ParameterDefinitionMissingException("Parameter initialisation failed. The parameter definition array must provide at least one parameter definition. If you only want the provided words from the console, don't use this class, take args directly.");
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

        // Automatic help texts cannot be done without their content provided.
        if (   helpText == null
            && autoHelp) throw new HelpTextUnavailableException("Parameter initialization failed. auto Help was set. This is only possible if a help Text was provided, too, which is not!");

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

        //Which parameters provided by the user were never defined?
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
                newBool = new Parameter(boolParameterDefinition.getParameterName(), true, boolParameterDefinition.getHelpText()); //Present, so flag will be set.
            }
            else {// A Bool defined in the Defintions, but not given by the user
                newBool = new Parameter(boolParameterDefinition.getParameterName(), false, boolParameterDefinition.getHelpText()); //Not present, so flag unset.
            }
            listOfParameters.Add(newBool);
        }

        //Remove unknown and bool Parameters, which leaves only »real« Parameters and their values.
        List<string> resArgs = new List<string>();

        //Let's remove Bools, as they were already completed. And also unknown parameters, as we cannot do anything with them anyways. The rest we work with.
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
                        missingValueParameterNames.Add(defectOne.getParameterName());
                    }

                }
            }
        }

        //All parameters defined but not provided and not required can be added as OKish but with no values. Let the programmer decide, what to do with those missing ones...
        foreach (ParameterDefinition pDef in newParameterDefinitions) {
            if (   !allProvidedParameterNames.Contains(withPrefix(pDef.getParameterName())) // Not provided values are added as default with no content
                && pDef.getType() != ParameterType.Boolean) { // Except for bools, of course. They were already taken care of above.
                listOfParameters.Add(new Parameter(pDef, false));
            }
        }

        // If only one of them is faulty, the entire list must be marked faulty as well!
        foreach (Parameter toCheck in listOfParameters) {
            if (toCheck.getIsTainted()) ConsoleParameters.isTainted = true;
        }
        ConsoleParameters.wasInitializedFlag = true;
        if (   autoHelp
            && isTainted) {
            printParameterHelp();
            Console.WriteLine();
            printParameterFaults();
            Environment.Exit(2);
        }
    }

    private static void printParameterFaults() {
        List<string> missingParameters = ConsoleParameters.getMissingButRequiredParameterNames();
        if (missingParameters.Count > 0) {
            string missingParameterNames = commaConcatStringList(missingParameters);
            missingParameterNames = "The following parameters are required but were not provided: " + missingParameterNames;
            printStringList(textBrokenUp(missingParameterNames, maxWidth));
        }

        List<string> doubledParameters = ConsoleParameters.getDoubledParameterNames();
        if (doubledParameters.Count > 0) {
            string doubledParameterNames = commaConcatStringList(doubledParameters);
            doubledParameterNames = "The following parameters were provided at least twice: " + doubledParameterNames;
            printStringList(textBrokenUp(doubledParameterNames, maxWidth));
        }

        List<string> unknownParameters = ConsoleParameters.getUnknownParameterNames();
        if (unknownParameters.Count > 0) {
            string unknownParametersNames = commaConcatStringList(unknownParameters);
            unknownParametersNames = "The following provided parameters are unknown: " + unknownParametersNames;
            printStringList(textBrokenUp(unknownParametersNames, maxWidth));
        }
        if (ConsoleParameters.getMissingValueParameterNames().Count > 0) {
            List <string> MissingValueParameterNamesNoPfx = ConsoleParameters.getMissingValueParameterNames();
            List <string> MissingValueParameterNames = new List<string>();
            foreach (string m in MissingValueParameterNamesNoPfx) {
                MissingValueParameterNames.Add(withPrefix(m));
            }
            string valueLessParameters = "The following parameters have no values: " + commaConcatStringList(MissingValueParameterNames);
            printStringList(textBrokenUp(valueLessParameters, maxWidth));
        }
        List<string> parseErrorParameterNames = new List<string>();
        List<string> tooManyParameterNames = new List<string>();
        List<string> tooFewParameterNames = new List<string>();
        List<string> ruleViolationHelpTexts = new List<string>();
        foreach(Parameter p in getParameters()) {
            if (p.getFlaws().Contains(ParameterFlaw.ParseError)) parseErrorParameterNames.Add(withPrefix(p.getName()));
            if (p.getFlaws().Contains(ParameterFlaw.TooManyValues)) tooManyParameterNames.Add(withPrefix(p.getName()));
            if (p.getFlaws().Contains(ParameterFlaw.TooFewValues)) tooFewParameterNames.Add(withPrefix(p.getName()));
            if (p.getFlaws().Contains(ParameterFlaw.RuleViolation)){
                ruleViolationHelpTexts.Add(withPrefix(p.getName()) + ": " + p.runCheck());
            }
        }
        if (parseErrorParameterNames.Count > 0) {
            string parseErrorParameters = "The following parameters could not be parsed properly: " + commaConcatStringList(parseErrorParameterNames);
            printStringList(textBrokenUp(parseErrorParameters, maxWidth));
        }
        if (tooManyParameterNames.Count > 0) {
            string tooManyParameters = "The following parameters provided too many values: " + commaConcatStringList(tooManyParameterNames);
            printStringList(textBrokenUp(tooManyParameters, maxWidth));
        }
        if (tooFewParameterNames.Count > 0) {
            string tooFewParameters = "The following parameters provided too few values: " + commaConcatStringList(tooFewParameterNames);
            printStringList(textBrokenUp(tooFewParameters, maxWidth));
        }
        if (ruleViolationHelpTexts.Count > 0) {
            foreach(string ruleViolationHelpText in ruleViolationHelpTexts) {
                printStringList(textBrokenUp(ruleViolationHelpText, maxWidth));
            }
        }
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

    public static List<string> getAllParameterNames() {
        ensureInitializationDone();
        return ConsoleParameters.allowedParameterNames;
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
        throw new ParameterNotFoundException("Parameter " + parameterName + " could not be found.");
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
            throw new ParameterNotFoundException("Parameter " + parameterName + " could not be found.");
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

    public static void dumpListOfParameters() {
        foreach (Parameter p in listOfParameters) {
            Console.WriteLine(p.ToString());
        }
    }

    private static List<string> getParameterHelpLines() {
        ensureInitializationDone();
        if (helpText == null) {
            throw new HelpTextUnavailableException("ConsoleParameters helpText is not set. Automatic help text is unavailable.");
        }
        List<string> result = new List<string>();
        string runcmd = getStartCommand() + " ";
        string mandatory = "";
        string optional = "";
        foreach(Parameter p in getParameters()) {
            ParameterDefinition pDef = getParameterDefinitionByName(p.getName());
            string parameterName = p.getName();
            if (p.getType() == ParameterType.Boolean){ //are always optional and take no values at all
                parameterName = " [" + withPrefix(parameterName) + "]";
                if (optional.Equals("")) {
                    optional = parameterName;
                }
                else {
                    optional += " " + parameterName;
                }
            }
            else {//non-Bool
                parameterName = withPrefix(parameterName) + " <";
                switch (pDef.getType()) {
                    case ParameterType.String:
                        parameterName += "string value";
                        break;
                    case ParameterType.Uinteger:
                        parameterName += "unsigned integer value";
                        break;
                    case ParameterType.Integer:
                        parameterName += "signed integer value";
                        break;
                    case ParameterType.Double:
                        parameterName += "floating point value";
                        break;
                    default:
                        break;
                }
                if (pDef.getMinValues() > 1) parameterName += "s"; //requires multiple ones in any case.
                if (    pDef.getMinValues() < 2 //allowes for multiple ones but does not need more than one.
                    && !pDef.getNoSplit() //always one
                    && !(pDef.getMaxValues() < 2)){// not 1 max.
                    parameterName += "(s)";
                }
                parameterName = parameterName + ">";
                if (pDef.getIsRequired()) {
                    parameterName = " " + parameterName;
                    if (mandatory.Equals("")) {
                        mandatory = parameterName;
                    }
                    else {
                        mandatory += " " + parameterName;
                    }
                }
                else { // optional
                    parameterName = " [" + parameterName + "]";
                    if (optional.Equals("")) {
                        optional = parameterName;
                    }
                    else {
                        optional += parameterName;
                    }
                }

            }
        }
        runcmd += mandatory + optional;
        List<string> runcmdLines = textBrokenUp(runcmd, maxWidth);
        result.AddRange(runcmdLines);
        result.Add("");
        List<string> overallDescription = textBrokenUp(helpText, maxWidth);
        result.AddRange(overallDescription);
        result.Add("");
        List<string> parameterNames = getAllParameterNames(); // These include the prefix already.
        uint leftColWidth = getMaxWidth(parameterNames) + 2; // Giving the parameter name a little distance from the description.
        uint rightColWidth = 0;
        try {
            rightColWidth = maxWidth - leftColWidth;
        }
        catch {
            Console.WriteLine("Seriously? A console window with width " + maxWidth + "characters? Make room for some output text! At least " + leftColWidth + 3 + "columns are recommended.");
            Environment.Exit(1);
        }
        foreach (Parameter p in getParameters()) {
            string pName = withPrefix(p.getName());
            string padding = Space((int) (leftColWidth - pName.Length));
            string pDescription = p.getHelpText();
            if (pDescription == null) throw new HelpTextUnavailableException("ParameterDefinition " + pName + ": No help text set, automatic help text is unavailable.");
            if (pDescription == null) return result;
            List<string> descriptionParts = textBrokenUp(pDescription, rightColWidth);
            string firstLine = pName + padding + descriptionParts[0];
            result.Add(firstLine);
            descriptionParts.RemoveAt(0);
            padding = Space((int) leftColWidth);
            foreach(string s in descriptionParts){
                string line = padding + s;
                result.Add(line);
            }
        }
        return result;
    }

    public static void printParameterHelp() {
        List<string> parameterHelpLines = getParameterHelpLines();
        printStringList(parameterHelpLines);
    }

    private static uint getMaxWidth(List<string> strings) {
        uint maxLength = 0;
        foreach(string s in strings) {
            if (maxLength < (uint) s.Length) maxLength =  (uint) s.Length;
        }
        return maxLength;
    }

    public static List<string> textBrokenUp(string text,
                                            uint maxWidth) {
        if (maxWidth < 2) { // probably academic, but anyways, this might happen.
            maxWidth = 1;
        }
        else {
            maxWidth -= 1; //The tiresome problem of optical line beaks at the end of the line when reaching its border, that some consoles rewrap, and others don't... So one less than specified, even if it breaks my heart.
        }
        List<string> textList = new List<string>(Regex.Split(text, @"\s+"));
        List<string> trunkatedTextList = new List<string>();
        string nextBlock = "";
        while (textList.Count > 0) { // When there is nothing left, stop.
            if (textList[0].Length > maxWidth) { // That's not exactly a good start...
                string toBeSplit = textList[0];
                textList.RemoveAt(0);
                string first = toBeSplit.Substring(0, (int) maxWidth);
                string second = toBeSplit.Substring((int) maxWidth);
                trunkatedTextList.Add(first); //Add that full-width line already. It won't get any longer...
                textList.Insert(0, second); //put back the rest.
            }
            while (   textList.Count > 0 //this may change at any time
                   && nextBlock.Length + 1 + textList[0].Length <= maxWidth) {
                string newBlockElement = textList[0];
                textList.RemoveAt(0);
                if (nextBlock.Length > 0) {
                    nextBlock += " " + newBlockElement;
                }
                else {
                    nextBlock = newBlockElement;
                }
            }
            if (   textList.Count > 0
                && textList[0].Length > maxWidth
                && (nextBlock.Length + 1) < maxWidth) { // That one would have to be split anyways next round, so we might as well do it here and add what we can right now to the current line
                string toBeSplit = textList[0];
                uint maximumResidualLength = maxWidth - (uint) nextBlock.Length - 1; //There's a space added on top of that, so one less.
                if (nextBlock.Length == 0) maximumResidualLength += 1;
                textList.RemoveAt(0);
                string first = toBeSplit.Substring(0, (int) maximumResidualLength);
                string second = toBeSplit.Substring((int) maximumResidualLength);
                if (nextBlock.Length == 0) {
                    nextBlock = first;
                }
                else {
                    nextBlock += " " + first; // Add that rest to the current line, wrapping things up there.
                }
                textList.Insert(0, second); // Rest will be taken care of later
            }
            trunkatedTextList.Add(nextBlock);
            nextBlock = "";
        }
        return trunkatedTextList;
    }

    private static string Space(int length) {
        string spaces = "".PadLeft(length);
        return spaces;
    }

    public static string getHelpText() {
        return helpText;
    }

    public static bool getAutoHelp() {
        return autoHelp;
    }

    public static string commaConcatStringList(List<string> strings) {
        string output = null;
        foreach (string element in strings) {
            if (output == null) {
                output = element;
            }
            else {
                output += ", " + element;
            }
        }
        return output;
    }

    private static void printStringList(List<string> list) {
        foreach(string item in list) Console.WriteLine(item);
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

public class HelpTextUnavailableException : System.Exception {
    public HelpTextUnavailableException() : base() {
        throw new HelpTextUnavailableException("The automatically generated help text is not available. Either you forgot setting the description value for ConsoleParameters or for at least one ParameterDefinition.");
    }
    public HelpTextUnavailableException(string message) : base(message) { }
    public HelpTextUnavailableException(string message, System.Exception inner) : base(message, inner) { }

    protected HelpTextUnavailableException(System.Runtime.Serialization.SerializationInfo info,
                                           System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
