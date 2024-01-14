using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace _1brc
{
    /// <summary>
    /// 
    /// </summary>
    unsafe internal static class Program
    {
        private static void Main( string[] args )
        {
            Console.ResetColor();
            try
            {
                const string FILE_NAME = "../../[data]/1brc_1B.txt"; //"../../[data]/1brc_100M.txt"; //

                var path = args.FirstOrDefault() ?? FILE_NAME;
                //-------------------------------------------------------//

                var sw = new Stopwatch();
                //var innerBufferCapacity = (1 << 24); //16MB
                //var innerBufferCapacity = (1 << 23); //8MB
                //var innerBufferCapacity = (1 << 20); //1MB
                //var innerBufferCapacity = (1 << 17); //130KB
                //var innerBufferCapacity = (1 << 14); //16KB
                //var innerBufferCapacity = (1 << 10); //1KB
                //var innerBufferCapacity = 512 * 10_000; //4.8MB
                //var innerBufferCapacity = 4096 * 10_000; //39MB
                var innerBufferCapacity = 4096 * 1_000; //3.9MB
                sw.Restart();
                var map = FileProcessor_LR.Process( path, chunkCount: Environment.ProcessorCount, innerBufferCapacity );
                sw.Stop();

                map.Print2Console();

                Console.WriteLine( $"Total elapsed: {sw.Elapsed}\r\n" );
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( ex );
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine( $"\r\n [.....finita.....]" );
            Console.ReadLine();
        }

        private static void Print2Console( this IDictionary< ListSegment< byte >, SummaryDouble > map )
        {
            long count   = 0;
            //var  line  = 0;
            //Console.Write( '{' );
            var query = map;//.Select( x => (Name: x.Key.ToString(), Summary: x.Value) ).OrderBy( x => x.Name, StringComparer.InvariantCulture );
            foreach ( var (Name, Summary) in query )
            {
                count   += Summary.Count;
                //Console.WriteLine( $"{Name} = {Summary}" );
                //if ( ++line < map.Count ) Console.Write( ", " );
            }

            //Console.WriteLine( '}' );
            //Console.WriteLine();
            Console.WriteLine( $"Total row count: {count:#,#}" );
        }
    }
}