#region

using ProtoParser.Parsing;

#endregion

if ( args.Length == 0 )
{
    Console.Error.WriteLine(
        "Missing path to Protobuf file. Please specify a path to a valid Protobuf file (either relative to the current directory or fully qualified)" );
}

string path = args[ 0 ];

if ( !File.Exists( path ) )
{
    Console.Error.WriteLine( "Invalid path to Protobuf file" );
}

Console.WriteLine( $"Parsing '{path}'" );

using StreamReader reader = new( path );
Parser.Parse( reader );