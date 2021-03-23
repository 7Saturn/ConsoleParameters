all : efstats2.exe
ifeq ($(OS),Windows_NT)
efstats2.exe : Efstats2.cs C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:efstats2.exe Efstats2.cs
clean:
	if exist efstats2.exe del masterserver.exe
else
efstats2.exe : Efstats2.cs ConsoleParameters.cs ParameterDefinition.cs Parameter.cs
	mcs -out:efstats2.exe Efstats2.cs ConsoleParameters.cs ParameterDefinition.cs Parameter.cs
clean:
	rm -f efstats2.exe
endif
