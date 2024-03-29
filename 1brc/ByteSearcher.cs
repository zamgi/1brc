using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace _1brc
{
    /// <summary>
    /// 
    /// </summary>
    internal static class _vc
    {
        public const int VECTOR256_SIZE_IN_BYTES = 32; // (Vector256< byte >.Count >> 3);
        public const int VECTOR128_SIZE_IN_BYTES = 16; // (Vector128< byte >.Count >> 3);
        public const int VECTOR64_SIZE_IN_BYTES  = 8;  // (Vector64< byte >.Count >> 3);
    }

    /// <summary>
    /// 
    /// </summary>
    internal interface IByteSearcher : IDisposable
    {
        int IndexOf( in ListSegment< byte > line_seg );
    }
    /// <summary>
    /// 
    /// </summary>
    internal interface IByteSearcher_v2 : IDisposable
    {
        int IndexOf( int startIndex, int totalLength );
    }
    /// <summary>
    /// 
    /// </summary>
    internal interface INewLineSearcher : IDisposable
    {
        int IndexOfNewLine( int startIndex, int length, out int skip_char_cnt );
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal static class ByteSearcherHelper
    {
        public static IByteSearcher Create_ByteSearcher( byte searchByte ) => Vector128.IsHardwareAccelerated ? new ByteSearcher_With_Intrinsics_128( searchByte ) : new ByteSearcher( searchByte );
        public static IByteSearcher_v2 Create_ByteSearcher_v2( byte searchByte, byte* readBufferPtr ) 
            => Vector256.IsHardwareAccelerated ? new ByteSearcher_With_Intrinsics_v2_256( searchByte, readBufferPtr ) :
               Vector128.IsHardwareAccelerated ? new ByteSearcher_With_Intrinsics_v2_128( searchByte, readBufferPtr ) : new ByteSearcher_v2( searchByte, readBufferPtr );

        public static INewLineSearcher Create_NewLineSearcher( byte[] buf ) 
            => Vector256.IsHardwareAccelerated ? new NewLineSearcher_With_Intrinsics_256( buf ) : new NewLineSearcher( buf );
    }

    /// <summary>
    /// 
    /// </summary>
    internal abstract class ByteSearcher_Base : IByteSearcher, IEqualityComparer< byte >
    {        
        public abstract int IndexOf( in ListSegment< byte > line_seg );
        public abstract void Dispose();

        bool IEqualityComparer< byte >.Equals( byte x, byte y ) => x == y;
        int IEqualityComparer< byte >.GetHashCode( byte obj ) => obj;
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal class ByteSearcher_With_Intrinsics_128 : ByteSearcher_Base
    {
        protected Vector256< byte > _SearchByte_Vector256;
        protected Vector128< byte > _SearchByte_Vector128;
        protected Vector64 < byte > _SearchByte_Vector64;
        protected byte _SearchByte;

        public ByteSearcher_With_Intrinsics_128( byte searchByte )
        {
            Debug.Assert( Vector128.IsHardwareAccelerated );

            _SearchByte = searchByte;
            _SearchByte_Vector256 = Vector256.Create( searchByte );
            _SearchByte_Vector128 = Vector128.Create( searchByte );
            _SearchByte_Vector64  = Vector64.Create( searchByte );
        }
        public override void Dispose() { }

        public override int IndexOf( in ListSegment< byte > line_seg )
        {
            fixed ( byte* base_ptr = line_seg.Array )
            {
                int idx;
                var start_idx = 0;
                var len       = line_seg.Count;
                var ptr       = base_ptr + line_seg.Offset;

                /*
                for (; ; )
                {
                    var next_start = start_idx + VECTOR128_SIZE_IN_BYTES;
                    if ( len < next_start )
                    {
                        break;
                    }

                    var matches = Vector128.Equals( Unsafe.ReadUnaligned< Vector128< byte > >( ptr + start_idx ), _SearchByte_Vector128 );
                    var mask    = (uint) Sse2.MoveMask( matches );
                    if ( mask != 0 )
                    {
                        var tzcnt = (uint) BitOperations.TrailingZeroCount( mask );
                        idx = (int) (start_idx + tzcnt);
                        return (idx);
                    }

                    start_idx = next_start;
                }
                //*/
                for (; ; )
                {
                    var next_start_idx = start_idx + _vc.VECTOR64_SIZE_IN_BYTES;
                    if ( len < next_start_idx )
                    {
                        break;
                    }

                    var matches = Vector64.Equals( Unsafe.ReadUnaligned< Vector64< byte > >( ptr + start_idx ), _SearchByte_Vector64 );
                    var mask    = (uint) Sse2.MoveMask( Vector64.ToVector128( matches ) );
                    if ( mask != 0 )
                    {
                        var tzcnt = (uint) BitOperations.TrailingZeroCount( mask );
                        idx = (int) (start_idx + tzcnt);
                        return (idx);
                    }

                    start_idx = next_start_idx;
                }

                idx = IndexOf( ptr, start_idx, len, _SearchByte ); Debug.Assert( 0 <= idx );
                return (idx);
            }       
        }

        [M(O.AggressiveInlining)] private static int IndexOf( byte* base_ptr, int startIndex, int len, byte searchByte )
        {
            for ( byte* ptr = base_ptr + startIndex, end_ptr = base_ptr + len; ptr < end_ptr; ptr++/*, startIndex++*/ )
            {
                if ( *ptr == searchByte )
                {
                    var idx = (int) (ptr - base_ptr);
                    return (idx);
                    //return (startIndex);
                }
            }
            return (-1);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal sealed class ByteSearcher_With_Intrinsics_v2_128 : IByteSearcher_v2
    {
        private Vector128< byte > _SearchByte_Vector128;
        private Vector64 < byte > _SearchByte_Vector64;
        private byte* _Buf_BasePtr;
        private ByteSearcher_v2 _ByteSearcher_v2;

        public ByteSearcher_With_Intrinsics_v2_128( byte searchByte, byte* readBufferPtr )
        {
            Debug.Assert( Vector128.IsHardwareAccelerated );

            SearchByte = searchByte;
            _SearchByte_Vector128 = Vector128.Create( searchByte );
            _SearchByte_Vector64  = Vector64.Create( searchByte );

            _Buf_BasePtr = readBufferPtr;
            _ByteSearcher_v2 = new ByteSearcher_v2( searchByte, readBufferPtr );
        }
        public void Dispose() { }

        public byte SearchByte { get; }
        public int IndexOf( int startIndex, int totalLength )
        {
            int idx;
            var start_idx = 0;
            var ptr       = _Buf_BasePtr + startIndex;

            for (; ; )
            {
                var next_start = start_idx + _vc.VECTOR128_SIZE_IN_BYTES;
                if ( totalLength < next_start )
                {
                    break;
                }

                var matches = Vector128.Equals( Unsafe.ReadUnaligned< Vector128< byte > >( ptr + start_idx ), _SearchByte_Vector128 );
                var mask    = (uint) Sse2.MoveMask( matches );
                if ( mask != 0 )
                {
                    var tzcnt = (uint) BitOperations.TrailingZeroCount( mask );
                    idx = (int) (start_idx + tzcnt);
                    return (idx);
                }

                start_idx = next_start;
            }

            for (; ; )
            {
                var next_start_idx = start_idx + _vc.VECTOR64_SIZE_IN_BYTES;
                if ( totalLength < next_start_idx )
                {
                    break;
                }

                var matches = Vector64.Equals( Unsafe.ReadUnaligned< Vector64< byte > >( ptr + start_idx ), _SearchByte_Vector64 );
                var mask    = (uint) Sse2.MoveMask( Vector64.ToVector128( matches ) );
                if ( mask != 0 )
                {
                    var tzcnt = (uint) BitOperations.TrailingZeroCount( mask );
                    idx = (int) (start_idx + tzcnt);
                    return (idx);
                }

                start_idx = next_start_idx;
            }

            idx = _ByteSearcher_v2.IndexOf( startIndex + start_idx, totalLength ); Debug.Assert( 0 <= idx );
            return (idx);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal sealed class ByteSearcher_With_Intrinsics_v2_256 : IByteSearcher_v2
    {
        private Vector256< byte > _SearchByte_Vector256;
        private Vector128< byte > _SearchByte_Vector128;
        private Vector64 < byte > _SearchByte_Vector64;
        private byte* _Buf_BasePtr;
        private ByteSearcher_v2 _ByteSearcher_v2;

        public ByteSearcher_With_Intrinsics_v2_256( byte searchByte, byte* readBufferPtr )
        {
            Debug.Assert( Vector256.IsHardwareAccelerated );

            SearchByte = searchByte;
            _SearchByte_Vector256 = Vector256.Create( searchByte );
            _SearchByte_Vector128 = Vector128.Create( searchByte );
            _SearchByte_Vector64  = Vector64.Create( searchByte );

            _Buf_BasePtr = readBufferPtr;
            _ByteSearcher_v2 = new ByteSearcher_v2( searchByte, readBufferPtr );
        }
        public void Dispose() { }

        public byte SearchByte { get; }
        public int IndexOf( int startIndex, int totalLength )
        {
            int idx;
            var start_idx = 0;
            var ptr       = _Buf_BasePtr + startIndex;

            for (; ; )
            {
                var next_start = start_idx + _vc.VECTOR256_SIZE_IN_BYTES;
                if ( totalLength < next_start )
                {
                    break;
                }

                var matches = Vector256.Equals( Unsafe.ReadUnaligned< Vector256< byte > >( ptr + start_idx ), _SearchByte_Vector256 );
                var mask    = (uint) Avx2.MoveMask( matches );
                if ( mask != 0 )
                {
                    var tzcnt = (uint) BitOperations.TrailingZeroCount( mask );
                    idx = (int) (start_idx + tzcnt);
                    return (idx);
                }

                start_idx = next_start;
            }

            for (; ; )
            {
                var next_start = start_idx + _vc.VECTOR128_SIZE_IN_BYTES;
                if ( totalLength < next_start )
                {
                    break;
                }

                var matches = Vector128.Equals( Unsafe.ReadUnaligned< Vector128< byte > >( ptr + start_idx ), _SearchByte_Vector128 );
                var mask    = (uint) Sse2.MoveMask( matches );
                if ( mask != 0 )
                {
                    var tzcnt = (uint) BitOperations.TrailingZeroCount( mask );
                    idx = (int) (start_idx + tzcnt);
                    return (idx);
                }

                start_idx = next_start;
            }

            for (; ; )
            {
                var next_start_idx = start_idx + _vc.VECTOR64_SIZE_IN_BYTES;
                if ( totalLength < next_start_idx )
                {
                    break;
                }

                var matches = Vector64.Equals( Unsafe.ReadUnaligned< Vector64< byte > >( ptr + start_idx ), _SearchByte_Vector64 );
                var mask    = (uint) Sse2.MoveMask( Vector64.ToVector128( matches ) );
                if ( mask != 0 )
                {
                    var tzcnt = (uint) BitOperations.TrailingZeroCount( mask );
                    idx = (int) (start_idx + tzcnt);
                    return (idx);
                }

                start_idx = next_start_idx;
            }


            idx = _ByteSearcher_v2.IndexOf( startIndex + start_idx, totalLength ); Debug.Assert( 0 <= idx );
            return (idx);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal sealed class NewLineSearcher_With_Intrinsics_256 : ByteSearcher_With_Intrinsics_128, INewLineSearcher
    {
        private byte[]   _Buf;
        private GCHandle _Buf_GCHandle;
        private byte*    _Buf_BasePtr;
        public NewLineSearcher_With_Intrinsics_256( byte[] buf ) : base( (byte) '\n' )
        {
            Debug.Assert( Vector256.IsHardwareAccelerated );

            _Buf          = buf;
            _Buf_GCHandle = GCHandle.Alloc( buf, GCHandleType.Pinned );
            _Buf_BasePtr  = (byte*) _Buf_GCHandle.AddrOfPinnedObject().ToPointer();
        }
        ~NewLineSearcher_With_Intrinsics_256() => _Buf_GCHandle.Free();
        public override void Dispose()
        {
            GC.SuppressFinalize( this );
            _Buf_GCHandle.Free();
        }

        public int IndexOfNewLine( int startIndex, int total_length, out int skip_char_cnt )
        {
            int idx;

            for (; ; )
            {
                var next_start = startIndex + _vc.VECTOR256_SIZE_IN_BYTES;
                if ( total_length < next_start )
                {
                    break;
                }

                var matches = Vector256.Equals( Unsafe.ReadUnaligned< Vector256< byte > >( _Buf_BasePtr + startIndex ), _SearchByte_Vector256 );
                var mask    = (uint) Avx2.MoveMask( matches );
                if ( mask != 0 )
                {
                    var tzcnt = (uint) BitOperations.TrailingZeroCount( mask );
                    idx = (int) (startIndex + tzcnt);
                    if ( (0 < idx) && (_Buf_BasePtr[ idx - 1 ] == '\r') )
                    {
                        idx--;
                        skip_char_cnt = 1;
                    }
                    else
                    {
                        skip_char_cnt = 0;
                    }
                    return (idx);
                }

                startIndex = next_start;
            }

            for (; ; )
            {
                var next_start = startIndex + _vc.VECTOR128_SIZE_IN_BYTES;
                if ( total_length < next_start )
                {
                    break;
                }

                var matches = Vector128.Equals( Unsafe.ReadUnaligned< Vector128< byte > >( _Buf_BasePtr + startIndex ), _SearchByte_Vector128 );
                var mask    = (uint) Sse2.MoveMask( matches );
                if ( mask != 0 )
                {
                    var tzcnt = (uint) BitOperations.TrailingZeroCount( mask );
                    idx = (int) (startIndex + tzcnt);
                    if ( (0 < idx) && (_Buf_BasePtr[ idx - 1 ] == '\r') )
                    {
                        idx--;
                        skip_char_cnt = 1;
                    }
                    else
                    {
                        skip_char_cnt = 0;
                    }
                    return (idx);
                }

                startIndex = next_start;
            }

            for (; ; )
            {
                var next_start_idx = startIndex + _vc.VECTOR64_SIZE_IN_BYTES;
                if ( total_length < next_start_idx )
                {
                    break;
                }

                var matches = Vector64.Equals( Unsafe.ReadUnaligned< Vector64< byte > >( _Buf_BasePtr + startIndex ), _SearchByte_Vector64 );
                var mask    = (uint) Sse2.MoveMask( Vector64.ToVector128( matches ) );
                if ( mask != 0 )
                {
                    var tzcnt = (uint) BitOperations.TrailingZeroCount( mask );
                    idx = (int) (startIndex + tzcnt);
                    if ( (0 < idx) && (_Buf_BasePtr[ idx - 1 ] == '\r') )
                    {
                        idx--;
                        skip_char_cnt = 1;
                    }
                    else
                    {
                        skip_char_cnt = 0;
                    }
                    return (idx);
                }

                startIndex = next_start_idx;
            }

            //---idx = NewLineSearcher._IndexOfNewLine_( _Buf, startIndex, total_length, out skip_char_cnt );
            idx = NewLineSearcher._IndexOfNewLine_( _Buf_BasePtr, startIndex, total_length, out skip_char_cnt );
            return (idx);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    internal sealed class ByteSearcher : ByteSearcher_Base
    {
        private byte _SearchByte;
        public ByteSearcher( byte searchByte ) => _SearchByte = searchByte;
        public override void Dispose() { }

        public override int IndexOf( in ListSegment< byte > line_seg ) => line_seg.IndexOf( _SearchByte, this );
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal sealed class ByteSearcher_v2 : IByteSearcher_v2
    {
        private byte  _SearchByte;
        private byte* _Buf_BasePtr;
        public ByteSearcher_v2( byte searchByte, byte* readBufferPtr )
        {
            _SearchByte  = searchByte;
            _Buf_BasePtr = readBufferPtr;
        }
        public void Dispose() { }

        public int IndexOf( int startIndex, int totalLength )
        {
            for ( byte* ptr = _Buf_BasePtr + startIndex, end_ptr = _Buf_BasePtr + totalLength; ptr < end_ptr; ptr++/*, startIndex++*/ )
            {
                if ( *ptr == _SearchByte )
                {
                    var idx = (int) (ptr - _Buf_BasePtr) - startIndex;
                    return (idx);
                    //return (startIndex);
                }
            }
            return (-1);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class NewLineSearcher : INewLineSearcher
    {
        private byte[] _Buf;
        public NewLineSearcher( byte[] buf ) => _Buf = buf;
        public void Dispose() { }
        public int IndexOfNewLine( int startIndex, int length, out int skip_char_cnt ) => _IndexOfNewLine_( _Buf, startIndex, length, out skip_char_cnt );
        [M(O.AggressiveInlining)] private static int _IndexOfNewLine_( byte[] buf, int startIndex, int length, out int skip_char_cnt )
        {
            for ( int i = startIndex, end = length - 1; i <= end; i++ )
            {
                switch ( buf[ i ] )
                {
                    case (byte) '\r':
                        skip_char_cnt = (i != end) && (buf[ i + 1 ] == '\n') ? 1 : 0;
                        return (i);

                    case (byte) '\n':
                        skip_char_cnt = 0;
                        return (i);
                }
            }
            skip_char_cnt = 0;
            return (-1);
        }
        [M(O.AggressiveInlining)] unsafe public static int _IndexOfNewLine_( byte* buf_ptr, int startIndex, int length, out int skip_char_cnt )
        {
            buf_ptr += startIndex;
            for ( int i = startIndex, end = length - 1; i <= end; i++ )
            {
                switch ( *buf_ptr )
                {
                    case (byte) '\r':
                        skip_char_cnt = (i != end) && (buf_ptr[ 1 ] == '\n') ? 1 : 0;
                        return (i);

                    case (byte) '\n':
                        skip_char_cnt = 0;
                        return (i);
                }
                buf_ptr++;
            }
            skip_char_cnt = 0;
            return (-1);
        }
    }
}