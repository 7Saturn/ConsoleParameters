using System;
using System.Collections.Generic;

public static class ParameterTest {
    public static int Main(string[] args) {
        ConsoleParameters.InitializeParameters("--",
                                               new ParameterDefinition[] {
                                                   new ParameterDefinition("test",
                                                                           ParameterType.Boolean,
                                                                           false),
                                                   new ParameterDefinition("bier",
                                                                           ParameterType.String,
                                                                           true),
                                                   new ParameterDefinition("posganzzahlen",
                                                                           ParameterType.Uinteger,
                                                                           true),
                                                   new ParameterDefinition("ganzzahlen",
                                                                           ParameterType.Integer,
                                                                           false),
                                                   new ParameterDefinition("zahlen",
                                                                           ParameterType.Double,
                                                                           false),
                                                   new ParameterDefinition("mussdasein",
                                                                           ParameterType.Double,
                                                                           true),
                                                   new ParameterDefinition("kanndasein",
                                                                           ParameterType.Double,
                                                                           false),
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
            if (ConsoleParameters.getParameters().Count > 0) {
                foreach (Parameter p in ConsoleParameters.getParameters()) {
                    if (p.getIsTainted()) {
                        Console.WriteLine("Parameter " + p.getName() + " hat ein Problem!");
                        if (p.getNumberOfValues() == 0) Console.WriteLine("Parameter " + p.getName() + " hat keinen Wert!");
                    }
                }
            }
            if (ConsoleParameters.getMissingValueParameterNames().Count > 0) {
                Console.WriteLine("Die folgenden Parameter haben keine Werte:");
                foreach (string pName in ConsoleParameters.getMissingValueParameterNames()) {
                    Console.WriteLine(pName);
                }
            }
        }
        else {
            Parameter test = ConsoleParameters.getParameterByName("test");
            bool testbool = test.getBoolValue();
            if (testbool) {
                Console.WriteLine("'test' was provided");
            }
            else {
                Console.WriteLine("'test' was not provided");
            }
            Parameter bier = ConsoleParameters.getParameterByName("bier");
            string[] bierWerte = bier.getStringValues();
            Console.WriteLine("Parameter Bier hat " + bier.getNumberOfValues() + " Parameter:");
            foreach (string wert in bierWerte) {
                Console.WriteLine(wert);
            }
            Parameter posganzzahlen = ConsoleParameters.getParameterByName("posganzzahlen");
            uint[] posganzzahlenWerte = posganzzahlen.getUintegerValues();
            Console.WriteLine("Parameter posganzzahlen hat folgende Werte:");
            foreach (uint wert in posganzzahlenWerte) {
                Console.WriteLine(wert);
            }

            Parameter ganzzahlen = ConsoleParameters.getParameterByName("ganzzahlen");
            if (ganzzahlen.getNumberOfValues() > 0) {
                Console.WriteLine("ganzzahlen hat (angeblich) " + ganzzahlen.getNumberOfValues() + "Werte.");
                int[] ganzzahlenWerte = ganzzahlen.getIntegerValues();
                Console.WriteLine("Parameter ganzzahlen hat folgende Werte:");
                foreach (uint wert in ganzzahlenWerte) {
                    Console.WriteLine(wert);
                }
            }
            Parameter zahlen = ConsoleParameters.getParameterByName("zahlen");
            double[] zahlenWerte = zahlen.getDoubleValues();
            Console.WriteLine("Parameter zahlen hat folgende Werte:");
            foreach (double wert in zahlenWerte) {
                Console.WriteLine(wert);
            }
            if (ConsoleParameters.getResidualArgs().Count > 0) {
                Console.WriteLine("Diese Werte blieben am Ende über:");
                foreach (string rest in ConsoleParameters.getResidualArgs()) {
                    Console.WriteLine(rest);
                }
            }
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
