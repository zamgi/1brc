using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
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

        private static List< (long start, int length) > Split2Chunks( string filePath, int? initChunkCount )
        {
            using var fileHandle = File.OpenHandle( filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, FileOptions.SequentialScan, 0 );
            var fileLength = RandomAccess.GetLength( fileHandle );

            using var mmf = MemoryMappedFile.CreateFromFile( fileHandle, null, fileLength, MemoryMappedFileAccess.Read, HandleInheritability.None, true );
            
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
            var sw = Stopwatch.StartNew(); Console.Write( "(begin Split2Chunks..." );
            var ts = Split2Chunks( filePath, chunkCount ); sw.Stop();
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

        public static Map< ListSegment< byte >, SummaryDouble > Process_v2( string filePath, int? chunkCount = null
            , int innerBufferCapacity = 1 << 20
            , int mapCapacity = 1_000 )
        {
            //1
            var sw = Stopwatch.StartNew(); Console.Write( "(begin Split2Chunks..." );
            var ts = Split2Chunks( filePath, chunkCount ); sw.Stop();
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
                    var map = FileSectionProcessor_LR.Process_WithCallback_v2( filePath, readFileLock: ts, t, innerBufferCapacity, mapCapacity );
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

        public static Map_ByteListSegment< SummaryDouble > Process_v3( string filePath, int? chunkCount = null
            , int innerBufferCapacity = 1 << 20
            , int mapCapacity = 1_000 )
        {
            //1
            var sw = Stopwatch.StartNew(); Console.Write( "(begin Split2Chunks..." );
            var ts = Split2Chunks( filePath, chunkCount ); sw.Stop();
            Console.WriteLine( $"end, elapsed: {sw.Elapsed})" );

            //2
            var po = new ParallelOptions();
#if DEBUG
            po.MaxDegreeOfParallelism = 1; //ts.Count; //
#else
            po.MaxDegreeOfParallelism = ts.Count;
#endif
            var bag = new ConcurrentBag< Map_ByteListSegment< SummaryDouble > >();
            Parallel.ForEach( ts, po,
#if DEBUG
                (t, _, i) =>
#else
                t =>
#endif
                {
                    var map = FileSectionProcessor_LR.Process_WithCallback_v3( filePath, readFileLock: ts, t, innerBufferCapacity, mapCapacity );
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

        public static Map< ListSegment< byte >, SummaryDouble > Process_v2_Plus( string filePath, int? chunkCount = null
            , int innerBufferCapacity = 1 << 20
            , int mapCapacity = 1_000 )
        {
            //1
            var sw = Stopwatch.StartNew(); Console.Write( "(begin Split2Chunks..." );
            var ts = Split2Chunks( filePath, chunkCount ); sw.Stop();
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
                    var map = FileSectionProcessor_LR.Process_WithCallback_v2_Plus( filePath, readFileLock: ts, t, innerBufferCapacity, mapCapacity );
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

                ref var summary = ref map.GetValueRefOrAddDefault( name, createNewKeyFunc );
                summary.Apply( d );

                //ref var summary = ref map.GetValueRefOrAddDefault( name, createNewKeyFunc, out var exists );
                //summary.Apply( d, exists );
            }

            return (map);
        }


        /// <summary>
        /// 
        /// </summary>
        private sealed class ReadByLineCallback : FileByteLineReader.IReadByLineCallback
        {
            private Map< ListSegment< byte >, SummaryDouble > _Map;            
            public ReadByLineCallback( int mapCapacity ) => _Map = new Map< ListSegment< byte >, SummaryDouble >( mapCapacity, ListSegment< byte >.EqualityComparer.Inst );

            public Map< ListSegment< byte >, SummaryDouble > Map => _Map;

            private static Func< ListSegment< byte >, ListSegment< byte > > _CreateNewKeyFunc = new Func< ListSegment< byte >, ListSegment< byte > >( key => new ListSegment< byte >( key.ToArray() ) );
            private static IByteSearcher _ByteSearcher   = ByteSearcherHelper.Create_ByteSearcher( (byte) ';' );
#if DEBUG
            private static IByteSearcher _ByteSearcher_2 = new ByteSearcher( (byte) ';' );
#endif

            void FileByteLineReader.IReadByLineCallback.Callback( in ListSegment< byte > line_seg )
            {
                var idx   = _ByteSearcher.IndexOf( line_seg ); Debug.Assert( 0 <= idx );
#if DEBUG
                var idx_2 = _ByteSearcher_2.IndexOf( line_seg ); Debug.Assert( idx == idx_2 );
#endif                
                var name = line_seg.Slice( 0, idx );
                var val  = line_seg.Slice( idx + 1 );

                var suc = DoubleParser.TryParse( val, out var d, out _ ); Debug.Assert( suc );

                ref var summary = ref _Map.GetValueRefOrAddDefault( name, _CreateNewKeyFunc );
                summary.Apply( d );
            }
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


        /// <summary>
        /// 
        /// </summary>
        private sealed class ReadBufferCallback : FileByteBufferReader.IReadBufferCallback, IDisposable
        {
            private Map< ListSegment< byte >, SummaryDouble > _Map;
            private byte[]   _ReadBuffer;
            private GCHandle _ReadBuffer_GCHandle;
            private byte*    _ReadBuffer_BasePtr;
            private IByteSearcher_v2 _ByteSearcher;
#if DEBUG
            private IByteSearcher_v2 _ByteSearcher_2; 
#endif
            public ReadBufferCallback( int mapCapacity, byte[] readBuffer )
            {
                _Map = new Map< ListSegment< byte >, SummaryDouble >( mapCapacity, ListSegment< byte >.EqualityComparer.Inst );

                _ReadBuffer          = readBuffer;
                _ReadBuffer_GCHandle = GCHandle.Alloc( readBuffer, GCHandleType.Pinned );
                _ReadBuffer_BasePtr  = (byte*) _ReadBuffer_GCHandle.AddrOfPinnedObject().ToPointer();

                _ByteSearcher = ByteSearcherHelper.Create_ByteSearcher_v2( (byte) ';', _ReadBuffer_BasePtr );
#if DEBUG
                _ByteSearcher_2 = new ByteSearcher_v2( (byte) ';', _ReadBuffer_BasePtr );
#endif
            }
            public void Dispose()
            {
                _ReadBuffer_GCHandle.Free();
                _ByteSearcher.Dispose();
            }

            public Map< ListSegment< byte >, SummaryDouble > Map => _Map;

            private static Func< ListSegment< byte >, ListSegment< byte > > _CreateNewKeyFunc = new Func< ListSegment< byte >, ListSegment< byte > >( key => new ListSegment< byte >( key.ToArray() ) );

            void FileByteBufferReader.IReadBufferCallback.Callback( int readByteCount )
            {
                var span = new Span< byte >( _ReadBuffer_BasePtr, readByteCount );
                for ( var startIdx = 0; startIdx < readByteCount; )
                {
                    //if ( startIdx == 4095994 )
                    //{
                    //    if ( Debugger.IsAttached ) Debugger.Break();
                    //}
                    var idx   = _ByteSearcher.IndexOf( startIdx, readByteCount ); Debug.Assert( 0 <= idx );
#if DEBUG
                    var idx_2 = _ByteSearcher_2.IndexOf( startIdx, readByteCount ); Debug.Assert( idx == idx_2 );
#endif
                    var name = new ListSegment< byte >( _ReadBuffer, startIdx, idx ); //span.Slice( startIdx, idx - startIdx );
                    idx++;
                    var val = span.Slice( startIdx + idx, readByteCount - (startIdx + idx) ); //new ListSegment< byte >( _ReadBuffer, startIdx + idx, readByteCount - (startIdx + idx) );

                    var suc = DoubleParser.TryParse_ByNewLine( val, out var d, out var endPos ); Debug.Assert( suc );

                    ref var summary = ref _Map.GetValueRefOrAddDefault( name, _CreateNewKeyFunc );
                    summary.Apply( d );

                    startIdx += idx + endPos + 1;
                }
            }
        }

        public static Map< ListSegment< byte >, SummaryDouble > Process_WithCallback_v2( string filePath, object readFileLock, in (long startIndex, int length)? section = null
            , int innerBufferCapacity = 1 << 20
            , int mapCapacity = 1_000 )
        {
            var readBuffer = new byte[ innerBufferCapacity ];
            using var rbc = new ReadBufferCallback( mapCapacity, readBuffer );

            FileByteBufferReader.Read( filePath, rbc, readFileLock, readBuffer, section );

            return (rbc.Map);
        }


        /// <summary>
        /// 
        /// </summary>
        private sealed class ReadBufferCallback_N1 : FileByteBufferReader.IReadBufferCallback, IDisposable
        {
            private Map_ByteListSegment< SummaryDouble > _Map;
            private byte[]   _ReadBuffer;
            private GCHandle _ReadBuffer_GCHandle;
            private byte*    _ReadBuffer_BasePtr;
            private IByteSearcher_v2 _ByteSearcher;
#if DEBUG
            private IByteSearcher_v2 _ByteSearcher_2; 
#endif
            public ReadBufferCallback_N1( int mapCapacity, byte[] readBuffer )
            {
                _Map = new Map_ByteListSegment< SummaryDouble >( mapCapacity );

                _ReadBuffer          = readBuffer;
                _ReadBuffer_GCHandle = GCHandle.Alloc( readBuffer, GCHandleType.Pinned );
                _ReadBuffer_BasePtr  = (byte*) _ReadBuffer_GCHandle.AddrOfPinnedObject().ToPointer();

                _ByteSearcher = ByteSearcherHelper.Create_ByteSearcher_v2( (byte) ';', _ReadBuffer_BasePtr );
#if DEBUG
                _ByteSearcher_2 = new ByteSearcher_v2( (byte) ';', _ReadBuffer_BasePtr );
#endif
            }
            public void Dispose()
            {
                _ReadBuffer_GCHandle.Free();
                _ByteSearcher.Dispose();
            }

            public Map_ByteListSegment< SummaryDouble > Map => _Map;

            private static GenNewKeyDelegate _CreateNewKeyFunc = new GenNewKeyDelegate( (in ByteListSegment key) => new ByteListSegment( key.ToArray() ) );

            void FileByteBufferReader.IReadBufferCallback.Callback( int readByteCount )
            {
                var span = new Span< byte >( _ReadBuffer_BasePtr, readByteCount );
                for ( var startIdx = 0; startIdx < readByteCount; )
                {
                    var idx   = _ByteSearcher.IndexOf( startIdx, readByteCount ); Debug.Assert( 0 <= idx );
#if DEBUG
                    var idx_2 = _ByteSearcher_2.IndexOf( startIdx, readByteCount ); Debug.Assert( idx == idx_2 );
#endif
                    var name = new ByteListSegment( _ReadBuffer, startIdx, idx );
                    idx++;
                    var val = span.Slice( startIdx + idx, readByteCount - (startIdx + idx) ); //new ByteListSegment( _ReadBuffer, startIdx + idx, readByteCount - (startIdx + idx) );

                    var suc = DoubleParser.TryParse_ByNewLine( val, out var d, out var endPos ); Debug.Assert( suc );

                    ref var summary = ref _Map.GetValueRefOrAddDefault( name, _CreateNewKeyFunc );
                    summary.Apply( d );

                    startIdx += idx + endPos + 1;
                }
            }
        }

        public static Map_ByteListSegment< SummaryDouble > Process_WithCallback_v3( string filePath, object readFileLock, in (long startIndex, int length)? section = null
            , int innerBufferCapacity = 1 << 20
            , int mapCapacity = 1_000 )
        {
            var readBuffer = new byte[ innerBufferCapacity ];
            using var rbc = new ReadBufferCallback_N1( mapCapacity, readBuffer );

            FileByteBufferReader.Read( filePath, rbc, readFileLock, readBuffer, section );

            return (rbc.Map);
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class ReadBufferCallback_Plus : FileByteBufferReader.IReadBufferCallback, IDisposable
        {
            private Map< ListSegment< byte >, SummaryDouble > _Map;
            private byte[]   _ReadBuffer;
            private GCHandle _ReadBuffer_GCHandle;
            private byte*    _ReadBuffer_BasePtr;
            private Vector256< byte > _SearchByte_Vector256;
            private Vector128< byte > _SearchByte_Vector128;
            private Vector64 < byte > _SearchByte_Vector64;
            private IByteSearcher_v2  _ByteSearcher; 
            public ReadBufferCallback_Plus( int mapCapacity, byte[] readBuffer )
            {
                Debug.Assert( Vector256.IsHardwareAccelerated );

                _Map = new Map< ListSegment< byte >, SummaryDouble >( mapCapacity, ListSegment< byte >.EqualityComparer.Inst );

                _ReadBuffer          = readBuffer;
                _ReadBuffer_GCHandle = GCHandle.Alloc( readBuffer, GCHandleType.Pinned );
                _ReadBuffer_BasePtr  = (byte*) _ReadBuffer_GCHandle.AddrOfPinnedObject().ToPointer();

                const byte SEARCH_BYTE = (byte) ';';
                _SearchByte_Vector256 = Vector256.Create( SEARCH_BYTE );
                _SearchByte_Vector128 = Vector128.Create( SEARCH_BYTE );
                _SearchByte_Vector64  = Vector64.Create( SEARCH_BYTE );
                _ByteSearcher         = new ByteSearcher_v2( SEARCH_BYTE, _ReadBuffer_BasePtr );
            }
            public void Dispose() => _ReadBuffer_GCHandle.Free();

            public Map< ListSegment< byte >, SummaryDouble > Map => _Map;

            private static Func< ListSegment< byte >, ListSegment< byte > > _CreateNewKeyFunc = new Func< ListSegment< byte >, ListSegment< byte > >( key => new ListSegment< byte >( key.ToArray() ) );

            void FileByteBufferReader.IReadBufferCallback.Callback( int readByteCount )
            {
                var span = new Span< byte >( _ReadBuffer_BasePtr, readByteCount );
                var ptr = _ReadBuffer_BasePtr;
                var startIdx = 0;
                //var end = (readByteCount / _vc.VECTOR256_SIZE_IN_BYTES) * _vc.VECTOR256_SIZE_IN_BYTES - _vc.VECTOR256_SIZE_IN_BYTES;
                for ( var end = readByteCount - _vc.VECTOR256_SIZE_IN_BYTES; startIdx < end; )
                {
                    //if ( startIdx == 4095994 )
                    //{
                    //    if ( Debugger.IsAttached ) Debugger.Break();
                    //}

                    var matches = Vector256.Equals( Unsafe.ReadUnaligned< Vector256< byte > >( ptr + startIdx ), _SearchByte_Vector256 );
                    var mask    = (uint) Avx2.MoveMask( matches );
                    for ( int prev_len = 0, prev_tzcnt = 0; mask != 0; )
                    {
                        var tzcnt = BitOperations.TrailingZeroCount( mask );
                        var name_len = (tzcnt + prev_tzcnt) - prev_len;

                        #region [..]
                        var name  = new ListSegment< byte >( _ReadBuffer, startIdx, name_len );
                        var val_i = startIdx + 1 + name_len;
                        var val   = span.Slice( val_i, readByteCount - val_i ); //new ListSegment< byte >( _ReadBuffer, val_i, readByteCount - val_i );

                        var suc = DoubleParser.TryParse_ByNewLine( val, out var d, out var endPos ); Debug.Assert( suc );

                        ref var summary = ref _Map.GetValueRefOrAddDefault( name, _CreateNewKeyFunc );
                        summary.Apply( d );

                        prev_tzcnt += tzcnt + 1;
                        var end_i  = val_i + endPos + 1;
                        prev_len   += end_i - startIdx;

                        startIdx = end_i;
                        #endregion

                        mask = mask >> (tzcnt + 1);
                    }
                }

                //tail
                for (; startIdx < readByteCount; )
                {
                    var idx  = _ByteSearcher.IndexOf( startIdx, readByteCount ); Debug.Assert( 0 <= idx );
                    var name = new ListSegment< byte >( _ReadBuffer, startIdx, idx );
                    idx++;
                    var val_i = startIdx + idx;
                    var val   = span.Slice( val_i, readByteCount - val_i ); //new ListSegment< byte >( _ReadBuffer, val_i, readByteCount - val_i );

                    var suc = DoubleParser.TryParse_ByNewLine( val, out var d, out var endPos ); Debug.Assert( suc );

                    ref var summary = ref _Map.GetValueRefOrAddDefault( name, _CreateNewKeyFunc );
                    summary.Apply( d );

                    startIdx += idx + endPos + 1;
                }
            }
        }

        public static Map< ListSegment< byte >, SummaryDouble > Process_WithCallback_v2_Plus( string filePath, object readFileLock, in (long startIndex, int length)? section = null
            , int innerBufferCapacity = 1 << 20
            , int mapCapacity = 1_000 )
        {
            innerBufferCapacity = (innerBufferCapacity / _vc.VECTOR256_SIZE_IN_BYTES) * _vc.VECTOR256_SIZE_IN_BYTES;

            var readBuffer = new byte[ innerBufferCapacity ];
            using var rbc = new ReadBufferCallback_Plus( mapCapacity, readBuffer );

            FileByteBufferReader.Read( filePath, rbc, readFileLock, readBuffer, section );

            return (rbc.Map);
        }
    }
}