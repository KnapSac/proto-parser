# proto-parser

Experimental Protobuf parser written in C#, which is both incremental and error tolerant.

## Running the parser
The parser can be run on a Protobuf file as follows:
```pwsh
dotnet run --project .\src\ProtoSharp\ProtoSharp.csproj -- .\protos\helloworld.proto
```

Alternatively, by importing the defined aliases from the tools directory, this becomes even easier:
```pwsh
. .\tools\aliases.ps1
run
```
