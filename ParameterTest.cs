using System;
using System.Collections.Generic;

public static class ParameterTest {
    public static int Main(string[] args) {
        ConsoleParameters.InitializeParameters("--",
                                               new ParameterDefinition[] {
                                                   new ParameterDefinition("booltest",
                                                                           ParameterType.Boolean,
                                                                           false),
                                                   new ParameterDefinition("bier",
                                                                           ParameterType.String,
                                                                           true,
                                                                           2,  // So viele Biersorten sollte man schon kennen
                                                                           4), // aber wir wollen es mal nicht übertreiben
                                                   new ParameterDefinition("posganzzahlen",
                                                                           ParameterType.Uinteger,
                                                                           true),
                                                   new ParameterDefinition("ganzzahlen",
                                                                           ParameterType.Integer,
                                                                           false),
                                                   new ParameterDefinition("zahlen",
                                                                           ParameterType.Double,
                                                                           false),
                                                   new ParameterDefinition("zahl",
                                                                           ParameterType.Double,
                                                                           false,
                                                                           0,
                                                                           0,
                                                                           true), //Let's not split that one
                                                   new ParameterDefinition("mussdasein",
                                                                           ParameterType.Double,
                                                                           true),
                                                   new ParameterDefinition("kanndasein",
                                                                           ParameterType.Double,
                                                                           false),
                                               },
                                               args);
        List<string> parameterNames = ConsoleParameters.getAllParameterNames();
        Console.WriteLine("The following parameter names are potentially used: " + commaConcatStringList(parameterNames));
        Console.WriteLine(ConsoleParameters.getStartCommand());
        Console.WriteLine("The following console arguments were provided: " + commaConcatStringList(args));
        if (ConsoleParameters.getIsTainted()) {
            Console.WriteLine("---something went wrong:");
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

            foreach (Parameter p in ConsoleParameters.getParameters()) {
                if (p.getIsTainted()) {
                    Console.WriteLine("Parameter " + p.getName() + " hat folgende Probleme:");
                    foreach(ParameterFlaw prob in p.getFlaws()) {
                        Console.WriteLine(prob.ToString());
                    }
                }
            }
            if (ConsoleParameters.getMissingValueParameterNames().Count > 0) {
                Console.WriteLine("Die folgenden Parameter haben keine Werte:");
                foreach (string pName in ConsoleParameters.getMissingValueParameterNames()) {
                    Console.WriteLine(pName);
                }
            }
            Console.WriteLine("---");
        }
        else {
            Parameter booltest = ConsoleParameters.getParameterByName("booltest");
            bool testbool = booltest.getBoolValue();
            if (testbool) {
                Console.WriteLine("'booltest' was provided");
            }
            else {
                Console.WriteLine("'booltest' was not provided");
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
            if (zahlen.getNumberOfValues() == 0) {
                Console.WriteLine("Parameter zahlen wurde nicht angegeben!");
            }
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
            Parameter zahl = ConsoleParameters.getParameterByName("zahl");
            if (zahl.getNumberOfValues() == 0) {
                Console.WriteLine("Parameter zahl wurde nicht angegeben!");
            }
            double[] zahlWerte = zahl.getDoubleValues();
            foreach (double wert in zahlWerte) {
                Console.WriteLine("zahlen hat: " + wert);
            }
        }
        Console.WriteLine("\n" + ConsoleParameters.getParameters().Count + " parameters were derived from the definition.");
        ConsoleParameters.dumpListOfParameters();
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
