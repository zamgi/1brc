﻿using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

            public bool Equals( ListSegment< T > x, ListSegment< T > y ) => HashHelper.IsEqualByBytes( x.AsSpan(), y.AsSpan() );
            public int GetHashCode( ListSegment< T > obj ) => obj.GetHashCode(); //HashHelper.Calc( obj.AsSpan() ); //

            public bool Equals( in ListSegment< T > x, in ListSegment< T > y ) => HashHelper.IsEqualByBytes( x.AsSpan(), y.AsSpan() );
            public int GetHashCode( in ListSegment< T > obj ) => obj.GetHashCode(); //HashHelper.Calc( obj.AsSpan() ); //
        }

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
            if ( array == null ) throw (new ArgumentException( "array == null" ));
            if ( (uint) offset > (uint) array.Length ) throw (new ArgumentException( $"[(uint) offset > (uint) array.Length], offset={(uint) offset}, array.Length={(uint) array.Length}" ));
            if ( (uint) count > (uint) (array.Length - offset) ) throw (new ArgumentException( $"[(uint) count > (uint) (array.Length - offset)], count={count}, array.Length={array.Length}, offset={offset}, (uint)(array.Length - offset)={(uint) (array.Length - offset)}" ));

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

        private int _Hash;
        public override int GetHashCode() 
        {
            if ( _Hash == 0 ) _Hash = HashHelper.Calc( this.AsSpan() );
            return (_Hash);
        }        

        public void CopyTo( T[] destination ) => CopyTo( destination, 0 );
        public void CopyTo( T[] destination, int destinationIndex ) => this.AsSpan().CopyTo( new Span< T >( destination, destinationIndex, _Count ) );
        public void CopyTo( ListSegment< T > destination ) => throw (new NotSupportedException());

        public override bool Equals( object obj ) => (obj is ListSegment< T > other) && Equals( in other );
        public bool Equals( ListSegment< T > other ) => HashHelper.IsEqualByBytes( this.AsSpan(), other.AsSpan() ); //(obj._Array == _Array) && (obj._Offset == _Offset) && (obj._Count == _Count);
        public bool Equals( in ListSegment< T > other ) => HashHelper.IsEqualByBytes( this.AsSpan(), other.AsSpan() );

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
        //[M(O.AggressiveInlining)] public Memory< T > AsMemory() => new Memory< T >( _Array, _Offset, _Count );

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

        public static bool operator ==( in ListSegment< T > a, in ListSegment< T > b ) => a.Equals( in b );
        public static bool operator !=( in ListSegment< T > a, in ListSegment< T > b ) => !(a == b);

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