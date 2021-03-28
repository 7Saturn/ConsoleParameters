using System;
using System.Collections.Generic;

public static class ParameterTest {
    public static int Main(string[] args) {
        ConsoleParameters.InitializeParameters("--",
                                               new ParameterDefinition[] {
                                                   new ParameterDefinition("booltest",
                                                                           ParameterType.Boolean,
                                                                           false,
                                                                           0,
                                                                           0,
                                                                           false),
                                                   new ParameterDefinition("uinttest",
                                                                           ParameterType.Uinteger,
                                                                           false,
                                                                           0,
                                                                           1,
                                                                           false),
                                                   new ParameterDefinition("inttest",
                                                                           ParameterType.Integer,
                                                                           false,
                                                                           1,
                                                                           2,
                                                                           false),
                                                   new ParameterDefinition("dtest",
                                                                           ParameterType.Double,
                                                                           false,
                                                                           2,
                                                                           3,
                                                                           false),
                                                   new ParameterDefinition("stringtest",
                                                                           ParameterType.String,
                                                                           true,
                                                                           3,
                                                                           4,
                                                                           false),
                                               },
                                               args);
        List<string> parameterNames = ConsoleParameters.getAllParameterNames();
        Console.WriteLine("The following parameter names are potentially used: " + commaConcatStringList(parameterNames));
        Console.WriteLine(ConsoleParameters.getStartCommand());
        Console.WriteLine("The following console arguments were provided: " + commaConcatStringList(args));
        Console.WriteLine("--------------------");

        if (ConsoleParameters.getIsTainted()) {
            Console.WriteLine("---------- something went wrong:");
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
                    Console.WriteLine("-----");
                    Console.WriteLine("Parameter " + p.getName() + " hat folgende Probleme:");
                    foreach(ParameterFlaw prob in p.getFlaws()) {
                        Console.WriteLine(prob.ToString());
                    }
                    Console.WriteLine("-----");
                }
            }
            if (ConsoleParameters.getMissingValueParameterNames().Count > 0) {
                Console.WriteLine("Die folgenden Parameter haben keine Werte:");
                Console.WriteLine(commaConcatStringList(ConsoleParameters.getMissingValueParameterNames()));
            }
            Console.WriteLine("----------");
        }
        else {
            Console.WriteLine("----------Alles gut:");
            Parameter booltest = ConsoleParameters.getParameterByName("booltest");
            bool testbool = booltest.getBoolValue();
            if (testbool) {
                Console.WriteLine("'booltest' was provided");
            }
            else {
                Console.WriteLine("'booltest' was not provided");
            }

            Parameter uinttest = ConsoleParameters.getParameterByName("uinttest");
            uint[] uinttestWerte = uinttest.getUintegerValues();
            Console.WriteLine("Parameter uinttest hat " + uinttest.getNumberOfValues() + " Parameter:");
            foreach (uint wert in uinttestWerte) {
                Console.WriteLine(wert);
            }

            Parameter inttest = ConsoleParameters.getParameterByName("inttest");
            int[] inttestWerte = inttest.getIntegerValues();
            Console.WriteLine("Parameter inttest hat folgende Werte:");
            foreach (int wert in inttestWerte) {
                Console.WriteLine(wert);
            }

            Parameter dtest = ConsoleParameters.getParameterByName("dtest");
            Console.WriteLine("dtest hat " + dtest.getNumberOfValues() + " Werte.");
            double[] dtestWerte = dtest.getDoubleValues();
            Console.WriteLine("Parameter dtest hat folgende Werte:");
            foreach (double wert in dtestWerte) {
                Console.WriteLine(wert);
            }
            Parameter stringtest = ConsoleParameters.getParameterByName("stringtest");
            if (stringtest.getNumberOfValues() == 0) {
                Console.WriteLine("Parameter stringtest wurde nicht angegeben!");
            }
            string[] stringtestWerte = stringtest.getStringValues();
            Console.WriteLine("Parameter stringtest hat folgende Werte:");
            foreach (string wert in stringtestWerte) {
                Console.WriteLine(wert);
            }
            Console.WriteLine("----------");
        }
        if (ConsoleParameters.getResidualArgs().Count > 0) {
            Console.WriteLine("Diese Werte blieben am Ende über:");
            foreach (string rest in ConsoleParameters.getResidualArgs()) {
                Console.WriteLine(rest);
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

    private static void dumpParameterValues(Parameter p) {
        if (p.getNumberOfValues() == 0) return;
        switch (p.getType())
        {
            case ParameterType.String:
                foreach(string s in p.getStringValues()){
                    Console.WriteLine(s);
                }
                break;
            case ParameterType.Integer:
                foreach(int s in p.getIntegerValues()){
                    Console.WriteLine(s);
                }
                break;
            case ParameterType.Uinteger:
                foreach(uint s in p.getUintegerValues()){
                    Console.WriteLine(s);
                }
                break;
            case ParameterType.Double:
                foreach(double s in p.getDoubleValues()){
                    Console.WriteLine(s);
                }
                break;
            case ParameterType.Boolean:
                Console.WriteLine(p.getBoolValue());
                break;
            default:
                break;
        }
    }
}
