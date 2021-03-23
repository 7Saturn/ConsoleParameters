all : parametertest.exe
ifeq ($(OS),Windows_NT)
parametertest.exe : ParameterTest.cs C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:parametertest.exe ParameterTest.cs
clean:
	if exist parametertest.exe del parametertest.exe
else
parametertest.exe : ParameterTest.cs ConsoleParameters.cs ParameterDefinition.cs Parameter.cs
	mcs -out:parametertest.exe ParameterTest.cs ConsoleParameters.cs ParameterDefinition.cs Parameter.cs
clean:
	rm -f parametertest.exe
endif
