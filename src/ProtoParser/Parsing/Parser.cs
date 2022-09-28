namespace ProtoParser.Parsing
{
    public class Parser
    {
        private readonly TextReader m_Reader;

        private Parser(
            TextReader reader )
        {
            m_Reader = reader;
        }

        public static void Parse(
            TextReader reader )
        {
            Parser parser = new( reader );
            parser.Parse( );
        }

        private void Parse( )
        {
            while ( m_Reader.ReadLine( ) is { } line )
            {
                Console.WriteLine( line );
            }
        }
    }
}