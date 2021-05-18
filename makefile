dll : build/ConsoleParameters.dll
all : dll build/parametertest.exe
ifeq ($(OS),Windows_NT)
build/parametertest.exe : src/ParameterTest.cs dll
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:build\\parametertest.exe src\\ParameterTest.cs -r:src\\ConsoleParameters.dll
build/ConsoleParameters.dll : src/ParameterTest.cs src/ConsoleParameters.cs src/ParameterDefinition.cs src/Parameter.cs
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:src\\ConsoleParameters.dll -t:library src\\ParameterTest.cs src\\ConsoleParameters.cs src\\ParameterDefinition.cs src\\Parameter.cs
clean:
	if exist src\\parametertest.exe del src\\parametertest.exe
	if exist src\\ConsoleParameters.dll del src\\ConsoleParameters.dll
else
build/parametertest.exe : build/ConsoleParameters.dll src/ParameterTest.cs
	mcs -out:build/parametertest.exe src/ParameterTest.cs -r:build/ConsoleParameters.dll
build/ConsoleParameters.dll : src/ParameterTest.cs src/ConsoleParameters.cs src/ParameterDefinition.cs src/Parameter.cs
	mcs -out:build/ConsoleParameters.dll -t:library src/ParameterTest.cs src/ConsoleParameters.cs src/ParameterDefinition.cs src/Parameter.cs
clean:
	rm -f build/parametertest.exe build/ConsoleParameters.dll
endif
