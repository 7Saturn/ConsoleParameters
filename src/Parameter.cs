using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

namespace ConsoleParameters {
    public enum ParameterType {
        String,
        Integer,
        Uinteger,
        Double,
        Boolean
    }

    public enum ParameterFlaw {
        ParseError,
        ValuesMissing,
        TooManyValues,
        TooFewValues,
        RuleViolation,
        ParameterMissing,
    }

    public class Parameter {
        public delegate string CheckFunction(Parameter p); //Used for automated checking on the provided values from the user. Requires a string to be returned, /if/ something went wrong. This help text should be displayed when the initiation of a Parameter went wrong due to faulty values.

        string parameterName; //Those must not contain the prefix --, - or /! Just like in the ParameterDefinition
        ParameterType type;
        uint numberOfValues;
        bool boolValue;
        string[] stringValues = new string[0]; //These default values will be overwritten, if the corresponding data is provided. But it is initalized for all of the datatypes as debugging these null values is a real bitch for someone who does not know they might be null (unrequired parameters that were not provided will be untainted but still empty)!
        double[] doubleValues = new double[0];
        uint[] uintValues = new uint[0];
        int[] intValues = new int[0];
        bool isTainted = false;
        List<ParameterFlaw> flaws = new List<ParameterFlaw>();
        string helpText; //May contain an explanation of what this parameter is supposed to do, what values it will expect and if it is mandatory or not.
        CheckFunction checker;

        public Parameter(string newParameterName, // Only to be used to create boolean type parameters
                         bool newValue,
                         string newHelpText = null) {
            if (   newParameterName == null
                || newParameterName.Length < 1) {
                throw new ParameterNameRequiredException("For a parameter definition a parameter name of at least one character length is required!");
            }
            this.parameterName = newParameterName;
            this.type = ParameterType.Boolean;
            this.numberOfValues = 1;
            this.boolValue = newValue; //Funny thing is, Bools can never be tainted. Either they are present, or not. =)
            this.helpText = newHelpText;
        }

        public Parameter(ParameterDefinition pDef,
                         string values) {
            string[] splitString;
            if (pDef.getNoSplit()) {
                splitString = new string[]{values};
            }
            else {
                splitString = valueSplit(values);
            }
            Parameter newOne = new Parameter (pDef.getParameterName(),
                                              pDef.getType(),
                                              splitString,
                                              pDef.getMinValues(),
                                              pDef.getMaxValues(),
                                              pDef.getHelpText(),
                                              pDef.getCheckFunction());
            this.parameterName = pDef.getParameterName();
            this.type = pDef.getType();
            this.numberOfValues = newOne.getNumberOfValues();
            this.boolValue = newOne.getBoolValueUnsafe();
            this.stringValues = newOne.getStringValuesUnsafe();
            this.uintValues = newOne.getUintegerValuesUnsafe();
            this.intValues = newOne.getIntegerValuesUnsafe();
            this.doubleValues = newOne.getDoubleValuesUnsafe();
            this.flaws = newOne.getFlaws();
            this.helpText = newOne.getHelpText();
            this.isTainted = newOne.getIsTainted();
            this.checker = newOne.getCheckFunction();
        }

        public Parameter(ParameterDefinition pDef,
                         bool isTainted) {// For parameters without values provided!
            this.parameterName = pDef.getParameterName();
            this.type = pDef.getType();
            this.numberOfValues = 0;
            this.isTainted = isTainted;
            this.helpText = pDef.getHelpText();
            if (   this.numberOfValues == 0
                && pDef.getIsRequired()) this.isTainted = true; //If it's required, it's required!
            if (isTainted) { //Means, nothing was provided but the parameter was present!
                this.flaws.Add(ParameterFlaw.ValuesMissing);
                this.flaws.Add(ParameterFlaw.TooFewValues);
            }
            else {//Means the parameter was not even present. That's not neccessarily wrong so it's not automatically tainting.
                this.flaws.Add(ParameterFlaw.ParameterMissing);
            }
        }

        public Parameter(string newParameterName,
                         ParameterType newType,    // Will always be provided (cannot be defined as null by the caller), so no exception required!
                         string[] providedValues,
                         uint newMinValues = 0,
                         uint newMaxValues = 0,
                         string newHelpText = null,
                         CheckFunction newChecker = null) {
            if (   newParameterName == null
                || newParameterName.Length < 1) {
                throw new ParameterNameRequiredException("For a parameter definition a parameter name of at least one character length is required!");
            }
            this.parameterName = newParameterName;
            this.type = newType;
            if (providedValues == null) {
                throw new ParameterValuesRequiredException("For a parameter a list of parameter values is required (at least an empty list)!");
            }

            if (newType == ParameterType.Boolean) {
                this.numberOfValues = 1;
                this.boolValue = true;
            }
            else {
                if (providedValues.Length == 0) { // It may be empty. If it is, it's wrong this way, unless it's a Bool. They don't have any payload.
                    this.flaws.Add(ParameterFlaw.ValuesMissing);
                }
            }

            if (newType == ParameterType.String) {
                string[] tempStrings = new string[providedValues.Length];
                Array.Copy(providedValues, tempStrings, providedValues.Length);
                this.stringValues = tempStrings;
                this.numberOfValues = (uint) providedValues.Length;
            }
            if (newType == ParameterType.Integer) {
                this.intValues = intArray(providedValues);
                if (this.intValues == null) {
                    this.numberOfValues = 0;
                    this.flaws.Add(ParameterFlaw.ParseError);
                }
                else {
                    this.numberOfValues = (uint) providedValues.Length;
                }
            }
            if (newType == ParameterType.Double) {
                this.doubleValues = doubleArray(providedValues);
                if (this.doubleValues == null) {
                    this.numberOfValues = 0;
                    this.flaws.Add(ParameterFlaw.ParseError);
                }
                else {
                    this.numberOfValues = (uint) providedValues.Length;
                }
            }
            if (newType == ParameterType.Uinteger) {
                this.uintValues = uIntArray(providedValues);
                if (this.uintValues == null) {
                    this.numberOfValues = 0;
                    this.flaws.Add(ParameterFlaw.ParseError);
                }
                else {
                    this.numberOfValues = (uint) providedValues.Length;
                }
            }

            if (newMinValues > 0 && newMinValues > this.numberOfValues) {
                this.flaws.Add(ParameterFlaw.TooFewValues);
            }

            if (newMaxValues > 0 && newMaxValues < this.numberOfValues) {
                this.flaws.Add(ParameterFlaw.TooManyValues);
            }

            this.helpText = newHelpText;

            this.checker = newChecker;
            if (flaws.Count > 0) this.isTainted = true;
            if (   checker != null
                && !this.isTainted) {
                /* Important: Must not be tainted. The programmer must be able to rely on a defined situation, such as, the number of parameters is within the definition realm.
                   If not, some checks will fail, so better not do this, when something went wront, already. */
                string problem = checker(this);
                if (problem != null) {
                    this.flaws.Add(ParameterFlaw.RuleViolation);
                    this.isTainted = true;
                }
            }


        }

        public string getName() {
            return parameterName;
        }

        public bool getIsTainted() {
            return isTainted;
        }

        public ParameterType getType() {
            return type;
        }

        public uint getNumberOfValues() {
            return numberOfValues;
        }

        public bool getBoolValue() {
            if (type != ParameterType.Boolean) {
                throw new ParameterTypeWrongForGetting("Boolean parameter value requested but parameter " + parameterName + " is not of type Boolean.");
            }
            return boolValue;
        }

        private bool getBoolValueUnsafe() {
            return boolValue;
        }

        public string[] getStringValues() {
            if (type != ParameterType.String) {
                throw new ParameterTypeWrongForGetting("String parameter values requested but parameter " + parameterName + " is not of type String.");
            }
            return stringValues;
        }

        private string[] getStringValuesUnsafe() {
            return stringValues;
        }

        public double[] getDoubleValues() {
            if (type != ParameterType.Double) {
                throw new ParameterTypeWrongForGetting("Double parameter values requested but parameter " + parameterName + " is not of type Double.");
            }
            return doubleValues;
        }

        private double[] getDoubleValuesUnsafe() {
            return doubleValues;
        }

        public override string ToString() {
            string result = parameterName;
            result += " " + numberOfValues + " x";
            result += " " + type.ToString();
            if (type == ParameterType.Boolean) result += " " + boolValue.ToString();
            if (stringValues != null && stringValues.Length > 0) result += " " + stringValues.Length + " string(s)";
            if (doubleValues != null && doubleValues.Length > 0) result += " " + doubleValues.Length + " double(s)";
            if (uintValues   != null &&   uintValues.Length > 0) result += " " + uintValues.Length + " uint(s)";
            if (intValues    != null &&    intValues.Length > 0) result += " " + intValues.Length + " int(s)";
            if (isTainted) {
                result += " tainted:";
                foreach(ParameterFlaw fault in flaws) {
                    result += " " + fault.ToString();
                }
            }
            else {
                result += " OK";
            }
            return result;
        }

        public int[] getIntegerValues() {
            if (type != ParameterType.Integer) throw new ParameterTypeWrongForGetting("Integer parameter values requested but parameter " + parameterName + " is not of type Integer.");
            return intValues;
        }

        private int[] getIntegerValuesUnsafe() {
            return intValues;
        }

        public uint[] getUintegerValues() {
            if (type != ParameterType.Uinteger) {
                throw new ParameterTypeWrongForGetting("Uint parameter values requested but parameter " + parameterName + " is not of type Uinteger.");
            }
            return uintValues;
        }

        private uint[] getUintegerValuesUnsafe() {
            return uintValues;
        }

        public List<ParameterFlaw> getFlaws() {
            return flaws;
        }

        public string getHelpText() {
            return this.helpText;
        }

        public CheckFunction getCheckFunction() {
            return checker;
        }

        public string runCheck() {
            if (checker == null) {
                throw new ParameterCheckerMissing("No parameter checking function was defined for " + parameterName + ". Cannot invoke it.");
            }
            return checker(this);
        }

        private static string[] valueSplit (string value) {
            List<string> list1 = new List<string>(Regex.Split(value, @"\s*,\s*"));
            List<string> list2 = new List<string>();
            foreach (string element in list1) {
                if (!element.Equals("")) list2.Add(element);
            }
            return list2.ToArray();
        }

        private static uint[] uIntArray(string[] strings) {
            uint[] uIntValues = new uint[strings.Length];
            for (uint counter = 0; counter < strings.Length; counter++) {
                uint tempValue;
                /* "en-US" is required, as for example in Germany, -3.3 would become -33 instead of the value.
                   As we use "," as separators for multiple values, it is not available for number entering.
                   Aside from that, people are rather used to use . as decimal point.
                   Also know: 1,000 and 1e3 are not allowed both with Number and Float.
                   Only one at a time can be used, so both may be helpful.
                */
                bool success = UInt32.TryParse(strings[counter], NumberStyles.Number, CultureInfo.CreateSpecificCulture ("en-US"), out tempValue);
                if (!success) success = UInt32.TryParse(strings[counter], NumberStyles.Float, CultureInfo.CreateSpecificCulture ("en-US"), out tempValue); //Sounds strange, but 1e2 is OK, too. 1.001e2 would not be, so we can do this to allow exponential values!
                if (!success) return null;
                uIntValues[counter] = tempValue;
            }
            return uIntValues;
        }

        private static int[] intArray(string[] strings) {
            int[] intValues = new int[strings.Length];
            for (uint counter = 0; counter < strings.Length; counter++) {
                int tempValue;
                bool success = Int32.TryParse(strings[counter], NumberStyles.Number, CultureInfo.CreateSpecificCulture ("en-US"), out tempValue);
                if (!success) success = Int32.TryParse(strings[counter], NumberStyles.Float, CultureInfo.CreateSpecificCulture ("en-US"), out tempValue);
                if (!success) return null;
                intValues[counter] = tempValue;
            }
            return intValues;
        }

        private static double[] doubleArray(string[] strings) {
            double[] doubleValues = new double[strings.Length];
            for (uint counter = 0; counter < strings.Length; counter++) {
                double tempValue;
                bool success = double.TryParse(strings[counter], NumberStyles.Float, CultureInfo.CreateSpecificCulture ("en-US"), out tempValue);
                if (!success) success = double.TryParse(strings[counter], NumberStyles.Number, CultureInfo.CreateSpecificCulture ("en-US"), out tempValue);
                if (!success) return null;
                doubleValues[counter] = tempValue;
            }
            return doubleValues;
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

    public class ParameterTypeWrongForGetting : System.Exception {
        public ParameterTypeWrongForGetting() : base() { }
        public ParameterTypeWrongForGetting(string message) : base(message) { }
        public ParameterTypeWrongForGetting(string message, System.Exception inner) : base(message, inner) { }

        protected ParameterTypeWrongForGetting(System.Runtime.Serialization.SerializationInfo info,
                                               System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class ParameterCheckerMissing : System.Exception {
        public ParameterCheckerMissing() : base() { }
        public ParameterCheckerMissing(string message) : base(message) { }
        public ParameterCheckerMissing(string message, System.Exception inner) : base(message, inner) { }

        protected ParameterCheckerMissing(System.Runtime.Serialization.SerializationInfo info,
                                          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
