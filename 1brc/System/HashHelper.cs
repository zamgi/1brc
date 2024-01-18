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
            switch ( span.Length )
            {
                case 0: return (0);
                case 1: return (*ptr);
                case 2: return (*ptr++ | (*ptr++ << 8));
                case 3: return (*ptr++ | (*ptr++ << 8) | (*ptr << 16));
                case 4: return (*(int*) ptr);
                case 5: return (*(int*) ptr ^ *(byte*) Unsafe.Add< byte >( ptr, sizeof(int) ));
                case 6: return (*(int*) ptr ^ *(byte*) Unsafe.Add< byte >( ptr, sizeof(int) ) ^ *(byte*) Unsafe.Add< byte >( ptr, sizeof(int) + 1 ));
                case 7: return (*(int*) ptr ^ *(byte*) Unsafe.Add< byte >( ptr, sizeof(int) ) ^ *(byte*) Unsafe.Add< byte >( ptr, sizeof(int) + 1 ) ^ *(byte*) Unsafe.Add< byte >( ptr, sizeof(int) + 2 ));
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
            switch ( len )
            {
                case 0: return (true);
                case 1: return (*ptr_1 == *ptr_2);
                case 2: return (*ptr_1++ == *ptr_2++) && (*ptr_1 == *ptr_2);
                case 3: return (*ptr_1++ == *ptr_2++) && (*ptr_1++ == *ptr_2++) && (*ptr_1 == *ptr_2);
                case 4: return (*(int*) ptr_1 == *(int*) ptr_2);

                case 5: return (*(int*) ptr_1 == *(int*) ptr_2) && (*(byte*) Unsafe.Add< byte >( ptr_1, sizeof(int) ) == *(byte*) Unsafe.Add< byte >( ptr_2, sizeof(int) ));

                case 6: return (*(int*) ptr_1 == *(int*) ptr_2) && (*(byte*) Unsafe.Add< byte >( ptr_1, sizeof(int)     ) == *(byte*) Unsafe.Add< byte >( ptr_2, sizeof(int)     ))
                                                                && (*(byte*) Unsafe.Add< byte >( ptr_1, sizeof(int) + 1 ) == *(byte*) Unsafe.Add< byte >( ptr_2, sizeof(int) + 1 ));

                case 7: return (*(int*) ptr_1 == *(int*) ptr_2) && (*(byte*) Unsafe.Add< byte >( ptr_1, sizeof(int)     ) == *(byte*) Unsafe.Add< byte >( ptr_2, sizeof(int)     ))
                                                                && (*(byte*) Unsafe.Add< byte >( ptr_1, sizeof(int) + 1 ) == *(byte*) Unsafe.Add< byte >( ptr_2, sizeof(int) + 1 ))
                                                                && (*(byte*) Unsafe.Add< byte >( ptr_1, sizeof(int) + 2 ) == *(byte*) Unsafe.Add< byte >( ptr_2, sizeof(int) + 2 ));
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