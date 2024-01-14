using System.Diagnostics.Contracts;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace System.Collections
{
    /// <summary>
    /// 
    /// </summary>
    internal static class HashHelpers
    {
        private const int HASH_PRIME = 101;
        private const int MAX_PRIME_ARRAY_LENGTH = 0x7FEFFFFD;

        public static readonly int[] _Primes =
        {
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
        };

        [M(O.AggressiveInlining)] public static bool IsPrime( int candidate )
        {
            if ( (candidate & 1) != 0 )
            {
                var limit = (int) Math.Sqrt( candidate );
                for ( int divisor = 3; divisor <= limit; divisor += 2 )
                {
                    if ( (candidate % divisor) == 0 )
                    {
                        return (false);
                    }
                }
                return (true);
            }
            return (candidate == 2);
        }

        [M(O.AggressiveInlining)] public static int GetPrime( int min )
        {
            if ( min < 0 ) throw (new ArgumentException( "CapacityOverflow" ));

            for ( int i = 0; i < _Primes.Length; i++ )
            {
                var prime = _Primes[ i ];
                if ( prime >= min ) return (prime);
            }

            for ( int i = (min | 1); i < int.MaxValue; i += 2 )
            {
                if ( IsPrime( i ) && ((i - 1) % HASH_PRIME != 0) )
                {
                    return (i);
                }
            }
            return (min);
        }

        [M(O.AggressiveInlining)] public static int ExpandPrime( int oldSize )
        {
            var newSize = 2 * oldSize;

            if ( ((uint) newSize > MAX_PRIME_ARRAY_LENGTH) && (MAX_PRIME_ARRAY_LENGTH > oldSize) )
            {
                Contract.Assert( MAX_PRIME_ARRAY_LENGTH == GetPrime( MAX_PRIME_ARRAY_LENGTH ), "Invalid MaxPrimeArrayLength" );
                return (MAX_PRIME_ARRAY_LENGTH);
            }

            return (GetPrime( newSize ));
        }
    }
}