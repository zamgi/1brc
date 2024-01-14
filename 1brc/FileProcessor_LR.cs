﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading.Tasks;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace _1brc
{
    /// <summary>
    /// 
    /// </summary>
    unsafe internal static class FileProcessor_LR
    {
        private const int MAX_CHUNK_SIZE = int.MaxValue - 100_000;

        private static List< (long start, int length) > SplitIntoMemoryChunks( string filePath, int? initChunkCount )
        {
            using var fs = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0, FileOptions.SequentialScan );
            var fileLength = fs.Length;

            using var mmf = MemoryMappedFile.CreateFromFile( fs, null, fileLength, MemoryMappedFileAccess.Read, HandleInheritability.None, true );
            
            using var mmva = mmf.CreateViewAccessor( 0, fileLength, MemoryMappedFileAccess.Read );
            using var mmvh = mmva.SafeMemoryMappedViewHandle;

            var pointer = (byte*) 0;
            mmvh.AcquirePointer( ref pointer );
            //----------------------------------------------------------------//

            var chunkCount = Math.Max( 1, initChunkCount.GetValueOrDefault( Environment.ProcessorCount ) );
            var chunkSize  = fileLength / chunkCount;
            while ( MAX_CHUNK_SIZE < chunkSize )
            {
                chunkCount *= 2;
                chunkSize   = fileLength / chunkCount;
            }

            var chunks = new List< (long start, int length) >( chunkCount );
            
            long pos = 0L;
            for ( int i = 0; i < chunkCount; i++ )
            {
                if ( fileLength <= (pos + chunkSize) )
                {
                    chunks.Add( (pos, (int) (fileLength - pos)) );
                    break;
                }

                var nextPos = pos + chunkSize;
                var span = new ReadOnlySpan< byte >( pointer + nextPos, (int) chunkSize );
                var idx  = IndexOfNewlineChar( span, out var offset ); Debug.Assert( 0 <= idx );
                nextPos += idx + offset;
                var len = nextPos - pos;
                chunks.Add( (pos, (int) len) );
                pos = nextPos;
            }

            return (chunks);
        }
        [M(O.AggressiveInlining)] private static int IndexOfNewlineChar( in ReadOnlySpan< byte > span, out int offset )
        {
            offset = default;

            var idx = span.IndexOfAny( (byte) '\n', (byte) '\r' );
            if ( (uint) idx < (uint) span.Length )
            {
                offset = 1;
                if ( span[ idx ] == '\r' )
                {
                    var next_idx = idx + 1;
                    if ( ((uint) next_idx < (uint) span.Length) && (span[ next_idx ] == '\n') )
                    {
                        offset = 2;
                    }
                }
            }
            return (idx);
        }

        public static Map< ListSegment< byte >, SummaryDouble > Process( string filePath, int? chunkCount = null
            , int innerBufferCapacity = 1 << 20
            , int mapCapacity = 1_000 )
        {
            //1
            var sw = Stopwatch.StartNew(); Console.Write( "(begin SplitIntoMemoryChunks..." );
            var ts = SplitIntoMemoryChunks( filePath, chunkCount ); sw.Stop();
            Console.WriteLine( $"end, elapsed: {sw.Elapsed})" );

            //2
            var po = new ParallelOptions();
#if DEBUG
            po.MaxDegreeOfParallelism = 1; //ts.Count; //
#else
            po.MaxDegreeOfParallelism = ts.Count;
#endif
            var bag = new ConcurrentBag< Map< ListSegment< byte >, SummaryDouble > >();
            Parallel.ForEach( ts, po,
#if DEBUG
                (t, _, i) =>
#else
                t =>
#endif
                {
                    var map = FileSectionProcessor_LR.Process_WithCallback( filePath, readFileLock: ts, t, innerBufferCapacity, mapCapacity );
                    bag.Add( map );
#if DEBUG
                    Console.WriteLine( $"{i + 1} of {ts.Count}" );
#endif
                });

            var res_map = bag.Aggregate( (map, chunk) =>
            {
                foreach ( var p in chunk )
                {
                    ref var summary = ref map.GetValueRefOrAddDefault( p.Key, out bool exists );
                    if ( exists )
                        summary.Merge( p.Value );
                    else
                        summary = p.Value;
                }

                return (map);
            });
            return (res_map);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal static class FileSectionProcessor_LR
    {
        public static Map< ListSegment< byte >, SummaryDouble > Process( string filePath, in (long startIndex, int length)? section = null
            , int innerBufferCapacity = 1 << 20
            , int mapCapacity = 1_000 )
        {
            var map = new Map< ListSegment< byte >, SummaryDouble >( mapCapacity, ListSegment< byte >.EqualityComparer.Inst );

            var createNewKeyFunc     = new Func< ListSegment< byte >, ListSegment< byte > >( key => new ListSegment< byte >( key.ToArray() ) );
            var isBytesAreEqualsFunc = new Func< byte, byte, bool >( ( x, y ) => x == y );

            foreach ( var line_seg in FileByteLineReader.ReadByLine( filePath, section, innerBufferCapacity ) )
            {
                var idx = line_seg.IndexOf( (byte) ';', isBytesAreEqualsFunc ); Debug.Assert( 0 <= idx );

                var name = line_seg.Slice( 0, idx );
                var val  = line_seg.Slice( idx + 1 );

                var suc = DoubleParser.TryParse( val, out var d, out _ ); Debug.Assert( suc );

                ref var summary = ref map.GetValueRefOrAddDefault( name, createNewKeyFunc, out var exists );
                summary.Apply( d, exists );
            }

            return (map);
        }


        /// <summary>
        /// 
        /// </summary>
        private sealed class ReadByLineCallback : FileByteLineReader.IReadByLineCallback, IEqualityComparer< byte >
        {
            private Map< ListSegment< byte >, SummaryDouble > _Map;
            public Map< ListSegment< byte >, SummaryDouble > Map => _Map;
            public ReadByLineCallback( int mapCapacity ) => _Map = new Map< ListSegment< byte >, SummaryDouble >( mapCapacity, ListSegment< byte >.EqualityComparer.Inst );

            void FileByteLineReader.IReadByLineCallback.Callback( in ListSegment< byte > line_seg )
            {
                var idx = line_seg.IndexOf( (byte) ';', this/*_is_bytes_equals_func*/ ); Debug.Assert( 0 <= idx );

                var name = line_seg.Slice( 0, idx );
                var val  = line_seg.Slice( idx + 1 );

                var suc = DoubleParser.TryParse( val, out var d, out _ ); Debug.Assert( suc );

                ref var summary = ref _Map.GetValueRefOrAddDefault( name, _CreateNewKeyFunc, out var exists );
                summary.Apply( d, exists );
            }

            private static Func< ListSegment< byte >, ListSegment< byte > > _CreateNewKeyFunc = new Func< ListSegment< byte >, ListSegment< byte > >( key => new ListSegment< byte >( key.ToArray() ) );

            bool IEqualityComparer< byte >.Equals( byte x, byte y ) => x == y;
            int IEqualityComparer< byte >.GetHashCode( byte obj ) => obj;
        }

        public static Map< ListSegment< byte >, SummaryDouble > Process_WithCallback( string filePath, in (long startIndex, int length)? section = null
            , int innerBufferCapacity = 1 << 20
            , int mapCapacity = 1_000 )
        {
            var rblc = new ReadByLineCallback( mapCapacity );

            FileByteLineReader.ReadByLine( filePath, rblc, section, innerBufferCapacity );

            return (rblc.Map);
        }
        public static Map< ListSegment< byte >, SummaryDouble > Process_WithCallback( string filePath, object readFileLock, in (long startIndex, int length)? section = null
            , int innerBufferCapacity = 1 << 20
            , int mapCapacity = 1_000 )
        {
            var rblc = new ReadByLineCallback( mapCapacity );

            FileByteLineReader.ReadByLine( filePath, rblc, readFileLock, section, innerBufferCapacity );

            return (rblc.Map);
        }
    }
}