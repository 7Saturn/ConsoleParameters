using System;
using System.Collections.Generic;

public static class Efstats2 {
    public static int Main(string[] args) {
        ConsoleParameters.InitializeParameters("--",
                                               new ParameterDefinition[] {
                                                   new ParameterDefinition("test",
                                                                           ParameterType.Boolean,
                                                                           false),
                                                   new ParameterDefinition("bier",
                                                                           ParameterType.String,
                                                                           true),
                                               },
                                               args);
        Console.WriteLine(ConsoleParameters.getStartCommand());
        Console.WriteLine("The following console values were provided: " + commaConcatStringList(args));
        if (ConsoleParameters.getIsTainted()) {
            List<string> missingParameters = ConsoleParameters.getMissingButRequiredParameterNames();
            if (missingParameters.Count > 0) {
                string missingParameterNames = commaConcatStringList(missingParameters);
                Console.WriteLine("The following parameters are required but were not provided: " + missingParameterNames);
            }
            List<string> doubledParameters = ConsoleParameters.getDoubledParameterNames();
            if (doubledParameters.Count > 0) {
                string doubledParameterNames = commaConcatStringList(doubledParameters);
                Console.WriteLine("The following parameters were provided at least twice by the user: " + doubledParameterNames);
            }
            List<string> unknownParameters = ConsoleParameters.getUnknownParameterNames();
            if (unknownParameters.Count > 0) {
                string unknownParametersNames = commaConcatStringList(unknownParameters);
                Console.WriteLine("The following unknown parameters were provided: " + unknownParametersNames);
            }
        }
        Parameter TestParameter = ConsoleParameters.getParameterByName("test");
        bool asTest = TestParameter.getBoolValue();
        if (asTest) {
            Console.WriteLine("'test' was provided");
        }
        else {
            Console.WriteLine("'test' was not provided");
        }

        string[] werte = ConsoleParameters.getParameterByName("bier").getStringValues();
        foreach (string wert in werte) {
            Console.WriteLine(wert);
        }
        return 0;
    }

    private static string commaConcatStringList(List<string> strings) {
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

    private static string commaConcatStringList(string[] strings) {
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
}
