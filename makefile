all : parametertest.exe ConsoleLibrary.dll
dll : ConsoleLibrary.dll
ifeq ($(OS),Windows_NT)
parametertest.exe : ParameterTest.cs ConsoleLibrary.dll
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:parametertest.exe ParameterTest.cs -r:ConsoleLibrary.dll
ConsoleLibrary.dll :
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:ConsoleLibrary.dll -t:library ParameterTest.cs ConsoleParameters.cs ParameterDefinition.cs Parameter.cs
clean:
	if exist parametertest.exe del parametertest.exe
	if exist ConsoleLibrary.dll del ConsoleLibrary.dll
else
parametertest.exe : ParameterTest.cs ConsoleLibrary.dll
	mcs -out:parametertest.exe ParameterTest.cs -r:ConsoleLibrary.dll
ConsoleLibrary.dll :
	mcs -out:ConsoleLibrary.dll -t:library ParameterTest.cs ConsoleParameters.cs ParameterDefinition.cs Parameter.cs
clean:
	rm -f parametertest.exe ConsoleLibrary.dll
endif
