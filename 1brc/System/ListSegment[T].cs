using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace System
{
    /// <summary>
    /// 
    /// </summary>
    internal struct ListSegment< T > : IList< T >, IReadOnlyList< T >
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class EqualityComparer : IEqualityComparerByRef< ListSegment< T > >, IEqualityComparer< ListSegment< T > >
        {
            public static EqualityComparer Inst { get; } = new EqualityComparer();
            private EqualityComparer() { }

            public bool Equals( ListSegment< T > x, ListSegment< T > y ) => x.IsEqual( y );
            public int GetHashCode( ListSegment< T > obj ) => obj.GetHashCode();

            public bool Equals( in ListSegment< T > x, in ListSegment< T > y ) => x.IsEqual( y );
            public int GetHashCode( in ListSegment< T > obj ) => obj.GetHashCode();
        }


        private static EqualityComparer< T > T_EqualityComparer;
        static ListSegment() => T_EqualityComparer = EqualityComparer< T >.Default;

        private T[] _Array;
        private int _Offset;
        private int _Count;

        [M(O.AggressiveInlining)] public ListSegment( DirectAccessList< T > lst )
        {
            _Array  = lst.GetInnerArray() ?? throw (new ArgumentNullException());
            _Offset = 0;
            _Count  = lst.Count;
        }
        [M(O.AggressiveInlining)] public ListSegment( DirectAccessList< T > lst, int offset, int count )
        {            
            if ( lst == null || (uint) offset > (uint) lst.Count || (uint) count > (uint) (lst.Count - offset) ) throw (new ArgumentException());

            _Array  = lst.GetInnerArray();
            _Offset = offset;
            _Count  = count;
        }
        [M(O.AggressiveInlining)] public ListSegment( T[] array )
        {
            _Array  = array ?? throw (new ArgumentNullException());
            _Offset = 0;
            _Count  = array.Length;
        }
        [M(O.AggressiveInlining)] public ListSegment( T[] array, int offset, int count )
        {            
            if ( array == null || (uint) offset > (uint) array.Length || (uint) count > (uint) (array.Length - offset) ) throw (new ArgumentException());

            _Array  = array;
            _Offset = offset;
            _Count  = count;
        }


        public T[] Array  { [M(O.AggressiveInlining)] get => _Array; }
        public int Offset { [M(O.AggressiveInlining)] get => _Offset; }
        public int Count  { [M(O.AggressiveInlining)] get => _Count; }

        public T this[ int index ]
        {
            get
            {
                if ( (uint) index >= (uint) _Count ) throw (new ArgumentOutOfRangeException());

                return _Array[ _Offset + index ];
            }
            set
            {
                if ( (uint) index >= (uint) _Count ) throw (new ArgumentOutOfRangeException());

                _Array[ _Offset + index ] = value;
            }
        }

        //private const uint Prime1 = 2654435761U;
        private const uint Prime2 = 2246822519U;
        private const uint Prime3 = 3266489917U;
        private const uint Prime4 = 668265263U;
        private const uint Prime5 = 374761393U;
       // private const uint STEP_1 = 3219471443U; // uint hash = Prime5 + 8; QueueRound( hash, hash/*hc1*/ );
        private int _Hash;
        public override int GetHashCode() 
        {
            if ( _Hash == 0 )
            {
                var span = this.AsSpan();
                uint hash = Prime5 + 8;
                for ( var i = _Count - 1; 0 <= i; i-- )
                {
                    //_Hash ^= span[ i ].GetHashCode(); //T_EqualityComparer.GetHashCode( span[ i ] ); //


                    //_Hash = HashCode.Combine( _Hash, span[ i ].GetHashCode() );

                    //var hc1 = hash;
                    var hc2 = (uint) span[ i ].GetHashCode();

                    hash = QueueRound( hash, hash/*hc1*/ );
                    hash = QueueRound( hash, hc2 );

                    hash = MixFinal( hash );

                    [M(O.AggressiveInlining)] static uint QueueRound( uint hash, uint queuedValue ) => BitOperations.RotateLeft( hash + queuedValue * Prime3, 17 ) * Prime4;
                    [M(O.AggressiveInlining)] static uint MixFinal( uint hash )
                    {
                        hash ^= hash >> 15;
                        hash *= Prime2;
                        hash ^= hash >> 13;
                        hash *= Prime3;
                        hash ^= hash >> 16;
                        return (hash);
                    }
                }
                _Hash = (int) hash;
            }
            return (_Hash);
        }        
        [M(O.AggressiveInlining)] public bool IsEqual( in ListSegment< T > other )
        {
            if ( _Count != other._Count ) return (false);

            var span_1 = this.AsSpan();
            var span_2 = other.AsSpan();
            for ( var i = _Count - 1; 0 <= i; i-- )
            {
                if ( !T_EqualityComparer.Equals( span_1[ i ], span_2[ i ] ) )
                {
                    return (false);
                }
            }
            return (true);
        }

        public void CopyTo( T[] destination ) => CopyTo( destination, 0 );
        public void CopyTo( T[] destination, int destinationIndex ) => this.AsSpan().CopyTo( new Span< T >( destination, destinationIndex, _Count ) );

        public void CopyTo( ListSegment< T > destination ) => throw (new NotSupportedException());

        public override bool Equals( object obj ) => (obj is ListSegment< T > other) && Equals( other );
        public bool Equals( ListSegment< T > obj ) => (obj._Array == _Array) && (obj._Offset == _Offset) && (obj._Count == _Count);

        [M(O.AggressiveInlining)] public ListSegment< T > Slice( int index )
        {
#if DEBUG
            if ( (uint) index > (uint) _Count ) throw (new ArgumentOutOfRangeException()); 
#endif
            return (new ListSegment< T >( _Array, _Offset + index, _Count - index ));
        }
        [M(O.AggressiveInlining)] public ListSegment< T > Slice( int index, int count )
        {
#if DEBUG
            if ( (uint) index > (uint) _Count || (uint) count > (uint) (_Count - index) ) throw (new ArgumentOutOfRangeException());
#endif
            return (new ListSegment< T >( _Array, _Offset + index, count ));
        }
        [M(O.AggressiveInlining)] public Span< T > AsSpan() => new Span< T >( _Array, _Offset, _Count );
        [M(O.AggressiveInlining)] public Memory< T > AsMemory() => new Memory< T >( _Array, _Offset, _Count );

        [M(O.AggressiveInlining)] public T[] ToArray()
        {
            var array = new T[ _Count ];
            this.AsSpan().CopyTo( array );
            return (array);
        }
        [M(O.AggressiveInlining)] public int IndexOf( T t, IEqualityComparer< T > comp )
        {
            var span = this.AsSpan();
            for ( var i = 0; i < _Count; i++ )
            {
                if ( comp.Equals( span[ i ], t ) )
                {
                    return (i);
                }
            }
            return (-1);
        }
        [M(O.AggressiveInlining)] public int IndexOf( T t, int startIndex, IEqualityComparer< T > comp )
        {
            var span = this.AsSpan();
            for ( ; startIndex < _Count; startIndex++ )
            {
                if ( comp.Equals( span[ startIndex ], t ) )
                {
                    return (startIndex);
                }
            }
            return (-1);
        }
        [M(O.AggressiveInlining)] public int IndexOf( T t, Func< T, T, bool > comp )
        {
            var span = this.AsSpan();
            for ( var i = 0; i < _Count; i++ )
            {
                if ( comp( span[ i ], t ) )
                {
                    return (i);
                }
            }
            return (-1);
        }

        public static bool operator ==( ListSegment< T > a, ListSegment< T > b ) => a.Equals( b );
        public static bool operator !=( ListSegment< T > a, ListSegment< T > b ) => !(a == b);

        public static implicit operator ListSegment< T >( DirectAccessList< T > lst ) => (lst != null) ? new ListSegment< T >( lst ) : default;
        public static implicit operator ListSegment< T >( T[] array ) => (array != null) ? new ListSegment< T >( array ) : default;

        #region IList< T >
        T IList< T >.this[ int index ]
        {
            get
            {
                if ( index < 0 || index >= _Count ) throw (new ArgumentOutOfRangeException());

                return _Array[ _Offset + index ];
            }

            set
            {
                if ( index < 0 || index >= _Count ) throw (new ArgumentOutOfRangeException());

                _Array[ _Offset + index ] = value;
            }
        }
        int IList< T >.IndexOf( T item ) => throw (new NotSupportedException());
        void IList< T >.Insert( int index, T item ) => throw (new NotSupportedException());
        void IList< T >.RemoveAt( int index ) => throw (new NotSupportedException());
        #endregion

        #region IReadOnlyList< T >
        T IReadOnlyList< T >.this[ int index ]
        {
            get
            {
                if ( index < 0 || index >= _Count ) throw (new ArgumentOutOfRangeException());

                return _Array[ _Offset + index ];
            }
        }
        #endregion

        #region ICollection< T >
        bool ICollection< T >.IsReadOnly => true;

        void ICollection< T >.Add( T item ) => throw (new NotSupportedException());
        void ICollection< T >.Clear() => throw (new NotSupportedException());

        bool ICollection< T >.Contains( T item ) => throw (new NotSupportedException());
        //{
        //    int index = System.Array.IndexOf( _List, item, _Offset, _Count );
        //    Debug.Assert( index < 0 || (index >= _Offset && index < _Offset + _Count) );
        //    return index >= 0;
        //}

        bool ICollection< T >.Remove( T item ) => throw (new NotSupportedException());
        #endregion

        #region IEnumerable< T >
        IEnumerator< T > IEnumerable< T >.GetEnumerator() => new Enumerator( this );
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator( this );
        #endregion


        /// <summary>
        /// 
        /// </summary>
        public struct Enumerator : IEnumerator< T >
        {
            private readonly T[] _Array;
            private readonly int _Start;
            private readonly int _End; 
            private int _Idx;

            internal Enumerator( in ListSegment< T > seg )
            {
                Debug.Assert( seg._Array != null );
                Debug.Assert( seg._Offset >= 0 );
                Debug.Assert( seg._Count >= 0 );
                Debug.Assert( seg._Offset + seg._Count <= seg._Array.Length );

                _Array = seg._Array;
                _Start = seg._Offset;
                _End   = seg._Offset + seg._Count;
                _Idx   = seg._Offset - 1;
            }

            public bool MoveNext()
            {
                if ( _Idx < _End )
                {
                    _Idx++;
                    return (_Idx < _End);
                }
                return (false);
            }

            public T Current
            {
                get
                {
#if DEBUG
                    if ( _Idx < _Start ) throw (new InvalidOperationException());
                    if ( _Idx >= _End ) throw (new InvalidOperationException());
#endif
                    return (_Array[ _Idx ]);
                }
            }

            object IEnumerator.Current => Current;
            void IEnumerator.Reset() => _Idx = _Start - 1;
            public void Dispose() { }
        }
        
        public override string ToString()
        {
            if ( typeof(T) == typeof(byte) )
            {
                return (Encoding.UTF8.GetString( this.Select( t => Convert.ToByte( t ) ).ToArray() ));
            }
            return ($"Offset={_Offset}, Count={_Count}");
        }
    }
}