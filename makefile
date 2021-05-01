all : ConsoleParameters.dll parametertest.exe
dll : ConsoleParameters.dll
ifeq ($(OS),Windows_NT)
parametertest.exe : ParameterTest.cs ConsoleParameters.dll
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:parametertest.exe ParameterTest.cs -r:ConsoleParameters.dll
ConsoleParameters.dll : ParameterTest.cs ConsoleParameters.cs ParameterDefinition.cs Parameter.cs
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:ConsoleParameters.dll -t:library ParameterTest.cs ConsoleParameters.cs ParameterDefinition.cs Parameter.cs
clean:
	if exist parametertest.exe del parametertest.exe
	if exist ConsoleParameters.dll del ConsoleParameters.dll
else
parametertest.exe : ConsoleParameters.dll ParameterTest.cs
	mcs -out:parametertest.exe ParameterTest.cs -r:ConsoleParameters.dll
ConsoleParameters.dll : ParameterTest.cs ConsoleParameters.cs ParameterDefinition.cs Parameter.cs
	mcs -out:ConsoleParameters.dll -t:library ParameterTest.cs ConsoleParameters.cs ParameterDefinition.cs Parameter.cs
clean:
	rm -f parametertest.exe ConsoleParameters.dll
endif
