all : parametertest.exe ConsoleParameters.dll
dll : ConsoleParameters.dll
ifeq ($(OS),Windows_NT)
parametertest.exe : ParameterTest.cs ConsoleParameters.dll
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:parametertest.exe ParameterTest.cs -r:ConsoleParameters.dll
ConsoleParameters.dll :
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:ConsoleParameters.dll -t:library ParameterTest.cs ConsoleParameters.cs ParameterDefinition.cs Parameter.cs
clean:
	if exist parametertest.exe del parametertest.exe
	if exist ConsoleParameters.dll del ConsoleParameters.dll
else
parametertest.exe : ParameterTest.cs ConsoleParameters.dll
	mcs -out:parametertest.exe ParameterTest.cs -r:ConsoleParameters.dll
ConsoleParameters.dll :
	mcs -out:ConsoleParameters.dll -t:library ParameterTest.cs ConsoleParameters.cs ParameterDefinition.cs Parameter.cs
clean:
	rm -f parametertest.exe ConsoleParameters.dll
endif
