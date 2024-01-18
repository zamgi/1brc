using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics;

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

                var fn = args.FirstOrDefault() ?? FILE_NAME;

                Console.WriteLine( $"Vector512.IsHardwareAccelerated: {Vector512.IsHardwareAccelerated}" );
                Console.WriteLine( $"Vector256.IsHardwareAccelerated: {Vector256.IsHardwareAccelerated}" );
                Console.WriteLine( $"Vector128.IsHardwareAccelerated: {Vector128.IsHardwareAccelerated}" );
                Console.WriteLine( $" Vector64.IsHardwareAccelerated: {Vector64.IsHardwareAccelerated}" );
                //Console.WriteLine( $"System.Numerics.Vector.IsHardwareAccelerated: {System.Numerics.Vector.IsHardwareAccelerated}" );
                Console.WriteLine( $"FILE_NAME: '{Path.GetFullPath( fn )}'" );
                Console.WriteLine();
                //-------------------------------------------------------//

                using ( var p = Process.GetCurrentProcess() )
                {
                    p.PriorityBoostEnabled = true;
                    p.PriorityClass        = ProcessPriorityClass.High/*RealTime*/;
                    p.Threads.Cast< ProcessThread >().ToList().ForEach( t => { t.PriorityBoostEnabled = true; t.PriorityLevel = ThreadPriorityLevel.Highest; } );
                }

                var sw = new Stopwatch();
                //var innerBufferCapacity = (1 << 24); //16MB
                //var innerBufferCapacity = (1 << 23); //8MB
                //var innerBufferCapacity = (1 << 20); //1MB
                //var innerBufferCapacity = (1 << 17); //130KB
                //var innerBufferCapacity = (1 << 14); //16KB
                //var innerBufferCapacity = (1 << 10); //1KB
                //var innerBufferCapacity = 512 * 10_000; //4.8MB
                //var innerBufferCapacity = 4096 * 10_000; //39MB
                //var innerBufferCapacity = 4096 * 25_000; //97MB
                var innerBufferCapacity = 4096 * 1_000; //3.9MB
                sw.Restart();
                var suc = GC.TryStartNoGCRegion( int.MaxValue );
                var map = FileProcessor_LR.Process_v2( fn, chunkCount: Environment.ProcessorCount, innerBufferCapacity );
                //---var map = FileProcessor_LR.Process_v3( fn, chunkCount: Environment.ProcessorCount, innerBufferCapacity );
                //---var map = FileProcessor_LR.Process_v2_Plus( fn, chunkCount: Environment.ProcessorCount, innerBufferCapacity );
                try { if ( suc ) GC.EndNoGCRegion(); } catch {; }
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
            Console.ResetColor();
            Console.ReadLine();
        }

        private static void Print2Console( this IDictionary< ListSegment< byte >, SummaryDouble > map )
        {
            long   count = 0;
#if (DEBUG || CALC_SUM2)
            long   sum2  = 0;
#endif
            //var  line  = 0;
            //Console.Write( '{' );
            var query = map;//.Select( x => (Name: x.Key.ToString(), Summary: x.Value) ).OrderBy( x => x.Name, StringComparer.InvariantCulture );
            foreach ( var (Name, Summary) in query )
            {
                count += Summary.Count;
#if (DEBUG || CALC_SUM2)
                sum2 += Summary.Sum2;
#endif
                //Console.WriteLine( $"{Name} = {Summary}" );
                //if ( ++line < map.Count ) Console.Write( ", " );
            }

            //Console.WriteLine( '}' );
            //Console.WriteLine();
            Console.WriteLine( $"Total row count: {count:#,#}"
#if (DEBUG || CALC_SUM2)
                + $", (avg={1.0 * sum2 / count})"
#endif
                );
        }
        private static void Print2Console( this IDictionary< ByteListSegment, SummaryDouble > map )
        {
            long   count = 0;
#if (DEBUG || CALC_SUM2)
            long   sum2  = 0;
#endif
            //var  line  = 0;
            //Console.Write( '{' );
            var query = map;//.Select( x => (Name: x.Key.ToString(), Summary: x.Value) ).OrderBy( x => x.Name, StringComparer.InvariantCulture );
            foreach ( var (Name, Summary) in query )
            {
                count += Summary.Count;
#if (DEBUG || CALC_SUM2)
                sum2 += Summary.Sum2;
#endif
                //Console.WriteLine( $"{Name} = {Summary}" );
                //if ( ++line < map.Count ) Console.Write( ", " );
            }

            //Console.WriteLine( '}' );
            //Console.WriteLine();
            Console.WriteLine( $"Total row count: {count:#,#}"
#if (DEBUG || CALC_SUM2)
                + $", (avg={1.0 * sum2 / count})"
#endif
                );
        }
    }
}