using System.Numerics;
using System.Runtime.CompilerServices;
#if DEBUG
using System.Text; 
#endif

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace System
{
    /// <summary>
    /// 
    /// </summary>
    unsafe internal static class HashHelper
    {
        //private const uint Prime1 = 2654435761U;
        private const uint Prime2   = 2246822519U;
        private const uint Prime3   = 3266489917U;
        private const uint Prime4   = 668265263U;
        private const uint Prime5   = 374761393U;
        private const uint Prime5_8 = Prime5 + 8;
        // private const uint STEP_1 = 3219471443U; // uint hash = Prime5 + 8; QueueRound( hash, hash/*hc1*/ );

        [M(O.AggressiveInlining)] private static uint QueueRound( uint hash, uint queuedValue ) => BitOperations.RotateLeft( hash + queuedValue * Prime3, 17 ) * Prime4;
        [M(O.AggressiveInlining)] private static uint MixFinal( uint hash )
        {
            hash ^= hash >> 15;
            hash *= Prime2;
            hash ^= hash >> 13;
            hash *= Prime3;
            hash ^= hash >> 16;
            return (hash);
        }
        [M(O.AggressiveInlining)] public static int Calc< T >( in Span< T > span )
        {
            var ptr = (byte*) Unsafe.AsPointer( ref span.GetPinnableReference() );
#if DEBUG
            var s = Encoding.UTF8.GetString( ptr, span.Length );
#endif
            [M(O.AggressiveInlining)] static byte get_hi_byte( byte* ptr, int byteOffset ) => *(byte*) Unsafe.Add< byte >( ptr, sizeof(int) + byteOffset );

            switch ( span.Length )
            {
                case 0: return (0);
                case 1: return (*ptr);
                case 2: return (*ptr++ | (*ptr++ << 8));
                case 3: return (*ptr++ | (*ptr++ << 8) | (*ptr << 16));
                case 4: return (*(int*) ptr);
                case 5: return (*(int*) ptr ^ get_hi_byte( ptr, 0 ));
                case 6: return (*(int*) ptr ^ get_hi_byte( ptr, 0 ) ^ get_hi_byte( ptr, 1 ));
                case 7: return (*(int*) ptr ^ get_hi_byte( ptr, 0 ) ^ get_hi_byte( ptr, 1 ) ^ get_hi_byte( ptr, 2 ));
                case 8: return (*(int*) ptr ^ *(int*) Unsafe.Add< int >( ptr, 1 ));

                default:
                    var hash = Prime5_8;
                    var end  = ptr + span.Length;
                    do
                    {
                        //var hc1 = hash;
                        var hc2 = (uint) *(int*) ptr;
                        ptr += sizeof(int);

                        hash = QueueRound( hash, hash/*hc1*/ );
                        hash = QueueRound( hash, hc2 );

                        hash = MixFinal( hash );
                    }
                    while ( ptr + sizeof(int) <= end );

                    for ( ; ptr < end; )
                    {
                        //var hc1 = hash;
                        var hc2 = (uint) *ptr++;

                        hash = QueueRound( hash, hash/*hc1*/ );
                        hash = QueueRound( hash, hc2 );

                        hash = MixFinal( hash );
                    }

                    return ((int) hash);
            }
        }

        [M(O.AggressiveInlining)] private static ulong QueueRoundLong( ulong hash, ulong queuedValue ) => BitOperations.RotateLeft( hash + queuedValue * Prime3, 17 ) * Prime4;
        [M(O.AggressiveInlining)] private static ulong MixFinalLong( ulong hash )
        {
            hash ^= hash >> 15;
            hash *= Prime2;
            hash ^= hash >> 13;
            hash *= Prime3;
            hash ^= hash >> 16;
            return (hash);
        }
        [M(O.AggressiveInlining)] public static long CalcLong< T >( in Span< T > span )
        {
            var ptr = (byte*) Unsafe.AsPointer( ref span.GetPinnableReference() );
#if DEBUG
            var s = Encoding.UTF8.GetString( ptr, span.Length );
#endif
            [M(O.AggressiveInlining)] static long get_hi( byte* ptr, int byteOffset ) => (((long) *(byte*) Unsafe.Add< byte >( ptr, sizeof(int) + byteOffset )) << (32 + 8 * byteOffset));

            switch ( span.Length )
            {
                case 0: return (0);
                case 1: return (*ptr);
                case 2: return (*ptr++ | (*ptr++ << 8));
                case 3: return (*ptr++ | (*ptr++ << 8) | (*ptr << 16));
                case 4: return (*(int*) ptr);
                case 5: return (*(uint*) ptr | get_hi( ptr, 0 ));
                case 6: return (*(uint*) ptr | get_hi( ptr, 0 ) | get_hi( ptr, 1 ));
                case 7: return (*(uint*) ptr | get_hi( ptr, 0 ) | get_hi( ptr, 1 ) | get_hi( ptr, 2 ));
                case 8: return (*(long*) ptr);

                default:
                    ulong hash = Prime5_8;
                    var   end  = ptr + span.Length;
                    do
                    {
                        //var hc1 = hash;
                        var hc2 = (ulong) *(long*) ptr;
                        ptr += sizeof(long);

                        hash = QueueRoundLong( hash, hash/*hc1*/ );
                        hash = QueueRoundLong( hash, hc2 );

                        hash = MixFinalLong( hash );
                    }
                    while ( ptr + sizeof(long) <= end );

                    if ( ptr + sizeof(int) <= end )
                    {
                        do
                        {
                            //var hc1 = hash;
                            var hc2 = (uint) *(int*) ptr;
                            ptr += sizeof(int);

                            hash = QueueRoundLong( hash, hash/*hc1*/ );
                            hash = QueueRoundLong( hash, hc2 );

                            hash = MixFinalLong( hash );
                        }
                        while ( ptr + sizeof(int) <= end );
                    }

                    for ( ; ptr < end; )
                    {
                        //var hc1 = hash;
                        var hc2 = (uint) *ptr++;

                        hash = QueueRoundLong( hash, hash/*hc1*/ );
                        hash = QueueRoundLong( hash, hc2 );

                        hash = MixFinalLong( hash );
                    }

                    return ((long) hash);
            }
        }

        [M(O.AggressiveInlining)] public static bool IsEqualByBytes< T >( in Span< T > span_1, in Span< T > span_2 )
        {
            var len = span_1.Length;
            if ( len != span_2.Length ) return (false);

            var ptr_1 = (byte*) Unsafe.AsPointer( ref span_1.GetPinnableReference() );
            var ptr_2 = (byte*) Unsafe.AsPointer( ref span_2.GetPinnableReference() );
#if DEBUG
            var s_1 = Encoding.UTF8.GetString( ptr_1, len );
            var s_2 = Encoding.UTF8.GetString( ptr_2, len );
#endif
            [M(O.AggressiveInlining)] static byte get_hi_byte( byte* ptr, int byteOffset ) => *(byte*) Unsafe.Add< byte >( ptr, sizeof(int) + byteOffset );

            switch ( len )
            {
                case 0: return (true);
                case 1: return (*ptr_1 == *ptr_2);
                case 2: return (*ptr_1++ == *ptr_2++) && (*ptr_1 == *ptr_2);
                case 3: return (*ptr_1++ == *ptr_2++) && (*ptr_1++ == *ptr_2++) && (*ptr_1 == *ptr_2);
                case 4: return (*(int*) ptr_1 == *(int*) ptr_2);

                case 5: return (*(int*) ptr_1 == *(int*) ptr_2) && (get_hi_byte( ptr_1, 0 ) == get_hi_byte( ptr_2, 0 ));

                case 6: return (*(int*) ptr_1 == *(int*) ptr_2) && (get_hi_byte( ptr_1, 0 ) == get_hi_byte( ptr_2, 0 ))
                                                                && (get_hi_byte( ptr_1, 1 ) == get_hi_byte( ptr_2, 1 ));

                case 7: return (*(int*) ptr_1 == *(int*) ptr_2) && (get_hi_byte( ptr_1, 0 ) == get_hi_byte( ptr_2, 0 ))
                                                                && (get_hi_byte( ptr_1, 1 ) == get_hi_byte( ptr_2, 1 ))
                                                                && (get_hi_byte( ptr_1, 2 ) == get_hi_byte( ptr_2, 2 ));
                case 8: return (*(long*) ptr_1 == *(long*) ptr_2);
                default:
                    var end_1 = ptr_1 + len;
                    do
                    {
                        if ( *(long*) ptr_1 != *(long*) ptr_2 )
                        {
                            return (false);
                        }
                        ptr_1 += sizeof(long);
                        ptr_2 += sizeof(long);
                    }
                    while ( ptr_1 + sizeof(long) <= end_1 );

                    if ( ptr_1 + sizeof(int) <= end_1 )
                    {
                        do
                        {
                            if ( *(int*) ptr_1 != *(int*) ptr_2 )
                            {
                                return (false);
                            }
                            ptr_1 += sizeof(int);
                            ptr_2 += sizeof(int);
                        }
                        while ( ptr_1 + sizeof(int) <= end_1 );
                    }

                    for ( ; ptr_1 < end_1; )
                    {
                        if ( *ptr_1++ != *ptr_2++ )
                        {
                            return (false);
                        }
                    }

                    return (true);
            }
        }
    }
}