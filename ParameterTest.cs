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
                                                                           false,
                                                                           "This is a testing Boolean."),
                                                   new ParameterDefinition("uinttest",
                                                                           ParameterType.Uinteger,
                                                                           false,
                                                                           0,
                                                                           1,
                                                                           true,
                                                                           "This is a testing unsigned integer. It is optional, but if used, may only provide one value. It will not be split at comma positions."),
                                                   new ParameterDefinition("inttest",
                                                                           ParameterType.Integer,
                                                                           false,
                                                                           1,
                                                                           2,
                                                                           false,
                                                                           "This is a testing Integer. It is optional, but if used, requires exactly 1 or 2 values. It is split normally."),
                                                   new ParameterDefinition("dtest",
                                                                           ParameterType.Double,
                                                                           true,
                                                                           2,
                                                                           3,
                                                                           false,
                                                                           "This is a testing floating point value. It is mandatory and may hold exactly 2 or 3 values."),
                                                   new ParameterDefinition("stringtest",
                                                                           ParameterType.String,
                                                                           true,
                                                                           1,
                                                                           4,
                                                                           false,
                                                                           "This is a testing String parameter. It is mandatory. It requires 1-4 values, no more."),
                                               },
                                               args,
                                               "This is a parameter test program.",
                                               true);
        List<string> parameterNames = ConsoleParameters.getAllParameterNames();
        Console.WriteLine("The following parameter names are potentially used: " + ConsoleParameters.commaConcatStringList(parameterNames));
        Console.WriteLine(ConsoleParameters.getStartCommand());
        Console.WriteLine("The following console arguments were provided: " + commaConcatStringList(args));
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
        Console.WriteLine("Parameter uinttest has " + uinttest.getNumberOfValues() + " values:");
        foreach (uint wert in uinttestWerte) {
            Console.WriteLine(wert);
        }

        Parameter inttest = ConsoleParameters.getParameterByName("inttest");
        int[] inttestWerte = inttest.getIntegerValues();
        Console.WriteLine("Parameter inttest has the following values:");
        foreach (int wert in inttestWerte) {
            Console.WriteLine(wert);
        }

        Parameter dtest = ConsoleParameters.getParameterByName("dtest");
        Console.WriteLine("dtest has " + dtest.getNumberOfValues() + " values.");
        if (dtest.getNumberOfValues() > 0) {
            double[] dtestWerte = dtest.getDoubleValues();
            Console.WriteLine("Parameter dtest has the following values:");
            foreach (double wert in dtestWerte) {
                Console.WriteLine(wert);
            }
        }
        else {
            dumpParameterValues(dtest);
        }
        Parameter stringtest = ConsoleParameters.getParameterByName("stringtest");
        if (stringtest.getNumberOfValues() == 0) {
            Console.WriteLine("Parameter stringtest was not provided!");
        }
        string[] stringtestWerte = stringtest.getStringValues();
        Console.WriteLine("Parameter stringtest has the following values:");
        foreach (string wert in stringtestWerte) {
            Console.WriteLine(wert);
        }
        Console.WriteLine("----------");
        if (ConsoleParameters.getResidualArgs().Count > 0) {
            Console.WriteLine("These are the residual values (not part of any parameter):");
            foreach (string rest in ConsoleParameters.getResidualArgs()) {
                Console.WriteLine(rest);
            }
        }
        Console.WriteLine("\n" + ConsoleParameters.getParameters().Count + " parameters were derived from the definition.");
        ConsoleParameters.dumpListOfParameters();
        return 0;
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

    private static void printRuler(uint maxWidth) {
        Console.WriteLine("Maximum width: " + maxWidth);
        for (uint counter = 1; counter < maxWidth; counter++){
            if (counter % 100 == 0 && counter > 0) {
                Console.Write((counter / 100) % 10);
            }
            else {
                Console.Write(" ");
            }
        }
        Console.WriteLine("");
        for (uint counter = 1; counter < maxWidth; counter++){
            if (counter % 10 == 0 && counter > 0) {
                Console.Write((counter / 10) % 10);
            }
            else {
                Console.Write(" ");
            }
        }
        Console.WriteLine("");
        for (uint counter = 1; counter < maxWidth; counter++){
            Console.Write(counter % 10);
        }
        Console.WriteLine("");
    }
}
