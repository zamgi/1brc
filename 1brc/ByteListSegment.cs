using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace _1brc
{
    /// <summary>
    /// 
    /// </summary>
    internal struct ByteListSegment : IList< byte >, IReadOnlyList< byte >
    {
        /// <summary>
        /// 
        /// </summary>
        public struct Enumerator : IEnumerator< byte >
        {
            private readonly byte[] _Array;
            private readonly int _Start;
            private readonly int _End; 
            private int _Idx;

            internal Enumerator( in ByteListSegment seg )
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

            public byte Current
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

        private byte[] _Array;
        private int _Offset;
        private int _Count;

        [M(O.AggressiveInlining)] public ByteListSegment( DirectAccessList< byte > lst )
        {
            _Array  = lst.GetInnerArray() ?? throw (new ArgumentNullException());
            _Offset = 0;
            _Count  = lst.Count;
        }
        [M(O.AggressiveInlining)] public ByteListSegment( DirectAccessList< byte > lst, int offset, int count )
        {            
            if ( lst == null || (uint) offset > (uint) lst.Count || (uint) count > (uint) (lst.Count - offset) ) throw (new ArgumentException());

            _Array  = lst.GetInnerArray();
            _Offset = offset;
            _Count  = count;
        }
        [M(O.AggressiveInlining)] public ByteListSegment( byte[] array )
        {
            _Array  = array ?? throw (new ArgumentNullException());
            _Offset = 0;
            _Count  = array.Length;
        }
        [M(O.AggressiveInlining)] public ByteListSegment( byte[] array, int offset, int count )
        {            
            if ( array == null ) throw (new ArgumentException( "array == null" ));
            if ( (uint) offset > (uint) array.Length ) throw (new ArgumentException( $"[(uint) offset > (uint) array.Length], offset={(uint) offset}, array.Length={(uint) array.Length}" ));
            if ( (uint) count > (uint) (array.Length - offset) ) throw (new ArgumentException( $"[(uint) count > (uint) (array.Length - offset)], count={count}, array.Length={array.Length}, offset={offset}, (uint)(array.Length - offset)={(uint) (array.Length - offset)}" ));

            _Array  = array;
            _Offset = offset;
            _Count  = count;
        }


        public byte[] Array  { [M(O.AggressiveInlining)] get => _Array; }
        public int    Offset { [M(O.AggressiveInlining)] get => _Offset; }
        public int    Count  { [M(O.AggressiveInlining)] get => _Count; }

        public byte this[ int index ]
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

        public void CopyTo( byte[] destination ) => CopyTo( destination, 0 );
        public void CopyTo( byte[] destination, int destinationIndex ) => this.AsSpan().CopyTo( new Span< byte >( destination, destinationIndex, _Count ) );
        public void CopyTo( ListSegment< byte > destination ) => throw (new NotSupportedException());

        public override bool Equals( object obj ) => (obj is ByteListSegment other) && Equals( in other );
        public bool Equals( ByteListSegment other ) => HashHelper.IsEqualByBytes( this.AsSpan(), other.AsSpan() ); //(obj._Array == _Array) && (obj._Offset == _Offset) && (obj._Count == _Count);
        public bool Equals( in ByteListSegment other ) => HashHelper.IsEqualByBytes( this.AsSpan(), other.AsSpan() );

        [M(O.AggressiveInlining)] public ByteListSegment Slice( int index )
        {
#if DEBUG
            if ( (uint) index > (uint) _Count ) throw (new ArgumentOutOfRangeException()); 
#endif
            return (new ByteListSegment( _Array, _Offset + index, _Count - index ));
        }
        [M(O.AggressiveInlining)] public ByteListSegment Slice( int index, int count )
        {
#if DEBUG
            if ( (uint) index > (uint) _Count || (uint) count > (uint) (_Count - index) ) throw (new ArgumentOutOfRangeException());
#endif
            return (new ByteListSegment( _Array, _Offset + index, count ));
        }
        [M(O.AggressiveInlining)] public Span< byte > AsSpan() => new Span< byte >( _Array, _Offset, _Count );

        [M(O.AggressiveInlining)] public byte[] ToArray()
        {
            var array = new byte[ _Count ];
            this.AsSpan().CopyTo( array );
            return (array);
        }
        [M(O.AggressiveInlining)] public int IndexOf( byte t )
        {
            var span = this.AsSpan();
            for ( var i = 0; i < _Count; i++ )
            {
                if ( span[ i ] == t )
                {
                    return (i);
                }
            }
            return (-1);
        }
        [M(O.AggressiveInlining)] public int IndexOf( byte t, int startIndex )
        {
            var span = this.AsSpan();
            for ( ; startIndex < _Count; startIndex++ )
            {
                if ( span[ startIndex ] == t )
                {
                    return (startIndex);
                }
            }
            return (-1);
        }

        public static bool operator ==( in ByteListSegment a, in ByteListSegment b ) => a.Equals( in b );
        public static bool operator !=( in ByteListSegment a, in ByteListSegment b ) => !(a == b);

        public static implicit operator ByteListSegment( DirectAccessList< byte > lst ) => (lst != null) ? new ByteListSegment( lst ) : default;
        public static implicit operator ByteListSegment( byte[] array ) => (array != null) ? new ByteListSegment( array ) : default;

        #region IList< T >
        byte IList< byte >.this[ int index ]
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
        int IList< byte >.IndexOf( byte item ) => throw (new NotSupportedException());
        void IList< byte >.Insert( int index, byte item ) => throw (new NotSupportedException());
        void IList< byte >.RemoveAt( int index ) => throw (new NotSupportedException());
        #endregion

        #region IReadOnlyList< T >
        byte IReadOnlyList< byte >.this[ int index ]
        {
            get
            {
                if ( index < 0 || index >= _Count ) throw (new ArgumentOutOfRangeException());

                return _Array[ _Offset + index ];
            }
        }
        #endregion

        #region ICollection< T >
        bool ICollection< byte >.IsReadOnly => true;

        void ICollection< byte >.Add( byte item ) => throw (new NotSupportedException());
        void ICollection< byte >.Clear() => throw (new NotSupportedException());

        bool ICollection< byte >.Contains( byte item ) => throw (new NotSupportedException());
        bool ICollection< byte >.Remove( byte item ) => throw (new NotSupportedException());
        #endregion

        #region IEnumerable< T >
        IEnumerator< byte > IEnumerable< byte >.GetEnumerator() => new Enumerator( this );
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator( this );
        #endregion
        
        public override string ToString() => Encoding.UTF8.GetString( this.ToArray() );
    }

    /// <summary>
    /// 
    /// </summary>
    [DebuggerTypeProxy(typeof(MapDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    internal sealed class Map_ByteListSegment< T > : IDictionary< ByteListSegment, T >, ICollection< KeyValuePair< ByteListSegment, T > >, IReadOnlyDictionary< ByteListSegment, T >, IReadOnlyCollection< KeyValuePair< ByteListSegment, T > >, ICollection, IDictionary
    {
        /// <summary>
        /// 
        /// </summary>
        private struct Entry
        {
            public long HashCode;
            public int  Next;
            public ByteListSegment Key;
            public T    Value;
        }

        /// <summary>
        /// 
        /// </summary>
        public struct Enumerator : IEnumerator< KeyValuePair< ByteListSegment, T > >, IDisposable, IEnumerator, IDictionaryEnumerator
        {
            internal const int DictEntry    = 1;
            internal const int KeyValuePair = 2;

            private Map_ByteListSegment< T > _Map;
            private int _Index;
            private KeyValuePair< ByteListSegment, T > _Current;
            private int _RetTypeOfEnumerator;

            internal Enumerator( Map_ByteListSegment< T > dictionary, int retTypeOfEnumerator )
            {
                _Map   = dictionary;
                _Index = 0;
                _RetTypeOfEnumerator = retTypeOfEnumerator;
                _Current = default;
            }

            public KeyValuePair< ByteListSegment, T > Current => _Current;
            public bool MoveNext()
            {
                while ( (uint) _Index < (uint) _Map._Count )
                {
                    if ( 0 <= _Map._Entries[ _Index ].HashCode )
                    {
                        _Current = new KeyValuePair< ByteListSegment, T >( _Map._Entries[ _Index ].Key, _Map._Entries[ _Index ].Value );
                        _Index++;
                        return (true);
                    }
                    _Index++;
                }
                _Index = _Map._Count + 1;
                _Current = default;
                return (false);
            }
            public void Dispose() { }

            object IEnumerator.Current
            {

                get
                {
                    if ( _Index == 0 || _Index == _Map._Count + 1 ) throw (new InvalidOperationException( "InvalidOperation_EnumOpCantHappen" ));
                    
                    if ( _RetTypeOfEnumerator == 1 )
                    {
                        return (new DictionaryEntry( _Current.Key, _Current.Value ));
                    }
                    return (new KeyValuePair< ByteListSegment, T >( _Current.Key, _Current.Value ));
                }
            }
            DictionaryEntry IDictionaryEnumerator.Entry
            {

                get
                {
                    if ( _Index == 0 || (_Index == _Map._Count + 1) ) throw (new InvalidOperationException( "InvalidOperation_EnumOpCantHappen" ));
                    return (new DictionaryEntry( _Current.Key, _Current.Value ));
                }
            }
            object IDictionaryEnumerator.Key
            {
                get
                {
                    if ( _Index == 0 || _Index == _Map._Count + 1 ) throw (new InvalidOperationException( "InvalidOperation_EnumOpCantHappen" ));
                    return (_Current.Key);
                }
            }
            object IDictionaryEnumerator.Value
            {
                get
                {
                    if ( _Index == 0 || _Index == _Map._Count + 1 ) throw (new InvalidOperationException( "InvalidOperation_EnumOpCantHappen" ));
                    return (_Current.Value);
                }
            }
            void IEnumerator.Reset()
            {
                _Index   = 0;
                _Current = default;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [DebuggerTypeProxy(typeof(MapCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {Count}")]
        public sealed class KeyCollection : ICollection< ByteListSegment >, IEnumerable< ByteListSegment >, IReadOnlyCollection< ByteListSegment >, ICollection
        {
            /// <summary>
            /// 
            /// </summary>
            public struct Enumerator : IEnumerator< ByteListSegment >, IDisposable, IEnumerator
            {
                private Map_ByteListSegment< T > _Map;
                private int _Index;
                private ByteListSegment _CurrentKey;

                public ByteListSegment Current => _CurrentKey;
                object IEnumerator.Current
                {
                    get
                    {
                        if ( _Index == 0 || _Index == _Map._Count + 1 ) throw (new InvalidOperationException( "InvalidOperation_EnumOpCantHappen" ));
                        return (_CurrentKey);
                    }
                }
                internal Enumerator( Map_ByteListSegment< T > dictionary )
                {
                    _Map = dictionary;
                    _Index = 0;
                    _CurrentKey = default;
                }
                public void Dispose() { }

                public bool MoveNext()
                {
                    while ( (uint) _Index < (uint) _Map._Count )
                    {
                        if ( 0 <= _Map._Entries[ _Index ].HashCode )
                        {
                            _CurrentKey = _Map._Entries[ _Index ].Key;
                            _Index++;
                            return (true);
                        }
                        _Index++;
                    }
                    _Index = _Map._Count + 1;
                    _CurrentKey = default;
                    return (false);
                }
                void IEnumerator.Reset()
                {
                    _Index = 0;
                    _CurrentKey = default;
                }
            }

            private Map_ByteListSegment< T > _Map;

            public int Count => _Map.Count;
            bool ICollection< ByteListSegment >.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => ((ICollection) _Map).SyncRoot;

            public KeyCollection( Map_ByteListSegment< T > dictionary ) => _Map = dictionary ?? throw (new ArgumentNullException());
            public Enumerator GetEnumerator() => new Enumerator( _Map );

            public void CopyTo( ByteListSegment[] array, int index )
            {
                if ( array == null ) throw (new ArgumentNullException( "array" ));
                if ( index < 0 || index > array.Length ) throw (new ArgumentOutOfRangeException());
                if ( array.Length - index < _Map.Count ) throw (new ArgumentException());
                
                var count   = _Map._Count;
                var entries = _Map._Entries;
                for ( int i = 0; i < count; i++ )
                {
                    if ( 0 <= entries[ i ].HashCode )
                    {
                        array[ index++ ] = entries[ i ].Key;
                    }
                }
            }

            void ICollection< ByteListSegment >.Add( ByteListSegment item ) => throw (new NotSupportedException());
            void ICollection< ByteListSegment >.Clear() => throw (new NotSupportedException());
            bool ICollection< ByteListSegment >.Contains( ByteListSegment item ) => _Map.ContainsKey( item );
            bool ICollection< ByteListSegment >.Remove( ByteListSegment item ) => throw (new NotSupportedException());

            IEnumerator< ByteListSegment > IEnumerable< ByteListSegment >.GetEnumerator() => new Enumerator( _Map );
            IEnumerator IEnumerable.GetEnumerator() => new Enumerator( _Map );

            void ICollection.CopyTo( Array array, int index )
            {
                if ( array == null )   throw (new ArgumentNullException( "array" ));
                if ( array.Rank != 1 ) throw (new ArgumentException( "Arg_RankMultiDimNotSupported" ));
                if ( array.GetLowerBound( 0 ) != 0 ) throw (new ArgumentException( "Arg_NonZeroLowerBound" ));
                if ( index < 0 || index > array.Length ) throw (new ArgumentOutOfRangeException());
                if ( array.Length - index < _Map.Count ) throw (new ArgumentException( "Arg_ArrayPlusOffTooSmall" ));
                
                if ( array is ByteListSegment[] array2 )
                {
                    CopyTo( array2, index );
                    return;
                }
                
                var array3 = array as object[];
                if ( array3 == null ) throw (new ArgumentException( "Argument_InvalidArrayType" ));
                
                var count   = _Map._Count;
                var entries = _Map._Entries;
                for ( int i = 0; i < count; i++ )
                {
                    if ( 0 <= entries[ i ].HashCode )
                    {
                        array3[ index++ ] = entries[ i ].Key;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>        
        [DebuggerTypeProxy( typeof(MapCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {Count}")]
        public sealed class ValueCollection : ICollection< T >, IEnumerable< T >, IReadOnlyCollection< T >, ICollection
        {
            /// <summary>
            /// 
            /// </summary>
            public struct Enumerator : IEnumerator< T >, IDisposable, IEnumerator
            {
                private Map_ByteListSegment< T > _Map;
                private int _Index;
                private T _CurrentValue;

                public T Current => _CurrentValue;
                object IEnumerator.Current
                {

                    get
                    {
                        if ( _Index == 0 || _Index == _Map._Count + 1 ) throw (new InvalidOperationException( "InvalidOperation_EnumOpCantHappen" ));
                        return _CurrentValue;
                    }
                }

                internal Enumerator( Map_ByteListSegment< T > dictionary )
                {
                    _Map = dictionary;
                    _Index = 0;
                    _CurrentValue = default;
                }
                public void Dispose() { }

                public bool MoveNext()
                {
                    while ( (uint) _Index < (uint) _Map._Count )
                    {
                        if ( 0 <= _Map._Entries[ _Index ].HashCode )
                        {
                            _CurrentValue = _Map._Entries[ _Index ].Value;
                            _Index++;
                            return (true);
                        }
                        _Index++;
                    }
                    _Index = _Map._Count + 1;
                    _CurrentValue = default;
                    return (false);
                }
                void IEnumerator.Reset()
                {
                    _Index = 0;
                    _CurrentValue = default;
                }
            }

            private Map_ByteListSegment< T > _Map;
            public int Count => _Map.Count;
            bool ICollection< T >.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => ((ICollection) _Map).SyncRoot;

            public ValueCollection( Map_ByteListSegment< T > dictionary ) => _Map = dictionary ?? throw (new ArgumentNullException());
            public Enumerator GetEnumerator() => new Enumerator( _Map );

            public void CopyTo( T[] array, int index )
            {
                if ( array == null ) throw (new ArgumentNullException( "array" ));
                if ( index < 0 || index > array.Length ) throw (new ArgumentOutOfRangeException());
                if ( array.Length - index < _Map.Count ) throw (new ArgumentException( "Arg_ArrayPlusOffTooSmall" ));
                
                var count   = _Map._Count;
                var entries = _Map._Entries;
                for ( int i = 0; i < count; i++ )
                {
                    if ( 0 <= entries[ i ].HashCode )
                    {
                        array[ index++ ] = entries[ i ].Value;
                    }
                }
            }

            void ICollection< T >.Add( T item ) => throw (new NotSupportedException());
            bool ICollection< T >.Remove( T item ) => throw (new NotSupportedException());
            void ICollection< T >.Clear() => throw (new NotSupportedException());
            bool ICollection< T >.Contains( T item ) => _Map.ContainsValue( item );
            IEnumerator< T > IEnumerable< T >.GetEnumerator() => new Enumerator( _Map );
            IEnumerator IEnumerable.GetEnumerator() => new Enumerator( _Map );

            void ICollection.CopyTo( Array array, int index )
            {
                if ( array == null ) throw (new ArgumentNullException( "array" ));
                if ( array.Rank != 1 ) throw (new ArgumentException( "Arg_RankMultiDimNotSupported" ));                
                if ( array.GetLowerBound( 0 ) != 0 ) throw (new ArgumentException( "Arg_NonZeroLowerBound" ));
                if ( index < 0 || index > array.Length ) throw (new ArgumentOutOfRangeException());
                
                if ( array.Length - index < _Map.Count )
                {
                    throw (new ArgumentException( "Arg_ArrayPlusOffTooSmall" ));
                }
                if ( array is T[] array2 )
                {
                    CopyTo( array2, index );
                    return;
                }
                var array3 = array as object[];
                if ( array3 == null ) throw (new ArgumentException( "Argument_InvalidArrayType" ));
                
                var count   = _Map._Count;
                var entries = _Map._Entries;
                for ( int i = 0; i < count; i++ )
                {
                    if ( 0 <= entries[ i ].HashCode )
                    {
                        array3[ index++ ] = entries[ i ].Value;
                    }
                }
            }
        }

        private int[]   _Buckets;
        private Entry[] _Entries;
        private int _Count;
        private int _FreeList;
        private int _FreeCount;

        private KeyCollection   _Keys;
        private ValueCollection _Values;
        private object _SyncRoot;

        public Map_ByteListSegment(): this( 0 ) { }
        public Map_ByteListSegment( int capacity )
        {
            if ( capacity < 0 ) throw (new ArgumentOutOfRangeException( nameof(capacity) ));
            Init( capacity );            
        }
        public Map_ByteListSegment( IDictionary< ByteListSegment, T > dictionary ) : this( dictionary?.Count ?? 0 )
        {
            if ( dictionary == null ) throw (new ArgumentNullException());

            foreach ( var p in dictionary )
            {
                Add( p.Key, p.Value );
            }
        }
        private void Init( int capacity )
        {
            var prime = HashHelpers.GetPrime( capacity );
            _Buckets = new int[ prime ];
            Array.Fill( _Buckets, -1 );
            _Entries = new Entry[ prime ];
            _FreeList = -1;

            _FastModMultiplier = GetFastModMultiplier( (uint) _Buckets.Length );
        }

        public int Count => (_Count - _FreeCount);
        public KeyCollection Keys
        {
            get
            {
                if ( _Keys == null ) _Keys = new KeyCollection( this );
                return (_Keys);
            }
        }
        ICollection< ByteListSegment > IDictionary< ByteListSegment, T >.Keys => this.Keys;
        IEnumerable< ByteListSegment > IReadOnlyDictionary< ByteListSegment, T >.Keys => this.Keys;

        public ValueCollection Values
        {
            get
            {
                if ( _Values == null ) _Values = new ValueCollection( this );
                return (_Values);
            }
        }
        ICollection< T > IDictionary< ByteListSegment, T >.Values => this.Values;
        IEnumerable< T > IReadOnlyDictionary< ByteListSegment, T >.Values => this.Values;

        public T this[ in ByteListSegment key ]
        {
            get
            {
                if ( TryGetValue( key, out var value ) ) return (value);

                throw (new KeyNotFoundException());
            }
            set => Insert( key, value, add: false );
        }
        public T this[ ByteListSegment key ]
        {
            get
            {
                if ( TryGetValue( key, out var value ) ) return (value);

                throw (new KeyNotFoundException());
            }
            set => Insert( key, value, add: false );
        }
        object IDictionary.this[ object key ]
        {
            get
            {
                if ( IsCompatibleKey( key, out var kkey ) )
                {
                    var i = FindEntry( kkey );
                    if ( 0 <= i )
                    {
                        return (_Entries[ i ].Value);
                    }
                }
                return (null);
            }
            set
            {
                if ( key == null ) throw (new ArgumentNullException( "key" ));
                var kkey = ((ByteListSegment) key);
                this[ in kkey ] = (T) value;
            }
        }

        bool ICollection< KeyValuePair< ByteListSegment, T > >.IsReadOnly => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot
        {
            get
            {
                if ( _SyncRoot == null ) Interlocked.CompareExchange( ref _SyncRoot, new object(), null );
                return (_SyncRoot);
            }
        }
        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;
        ICollection IDictionary.Keys => this.Keys;
        ICollection IDictionary.Values => this.Values;

        void ICollection< KeyValuePair< ByteListSegment, T > >.Add( KeyValuePair< ByteListSegment, T > p ) => Add( p.Key, p.Value );
        bool ICollection< KeyValuePair< ByteListSegment, T > >.Contains( KeyValuePair< ByteListSegment, T > p )
        {
            var i = FindEntry( p.Key );
            return ((0 <= i) && EqualityComparer< T >.Default.Equals( _Entries[ i ].Value, p.Value ));
        }
        bool ICollection< KeyValuePair< ByteListSegment, T > >.Remove( KeyValuePair< ByteListSegment, T > p )
        {
            var i = FindEntry( p.Key );
            if ( (0 <= i) && EqualityComparer< T >.Default.Equals( _Entries[ i ].Value, p.Value ) )
            {
                Remove( p.Key );
                return (true);
            }
            return (false);
        }

        void IDictionary.Add( object key, object value ) => Add( (ByteListSegment) key, (T) value );
        bool IDictionary.Contains( object key ) => IsCompatibleKey( key, out var kkey ) && ContainsKey( kkey );
        void IDictionary.Remove( object key )
        {
            if ( IsCompatibleKey( key, out var kkey ) )
            {
                Remove( kkey );
            }
        }

        IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator( this, Enumerator.DictEntry );
        public Enumerator GetEnumerator() => new Enumerator( this, Enumerator.KeyValuePair );
        IEnumerator< KeyValuePair< ByteListSegment, T > > IEnumerable< KeyValuePair< ByteListSegment, T > >.GetEnumerator() => new Enumerator( this, Enumerator.KeyValuePair );
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator( this, Enumerator.KeyValuePair );

        public bool TryAdd( in ByteListSegment key, T value )
        {
            var hash   = InternalGetHashCode( key );
            var bucket = GetBucket( hash );
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref readonly var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && HashHelper.IsEqualByBytes( slot.Key.AsSpan(), key.AsSpan() ) )
                {
                    return (false);
                }
                i = slot.Next;
            }

            int index;
            if ( 0 < _FreeCount )
            {
                index = _FreeList;
                _FreeList = _Entries[ index ].Next;
                _FreeCount--;
            }
            else
            {
                if ( _Count == _Entries.Length )
                {
                    Resize();
                    bucket = GetBucket( hash );
                }
                index = _Count;
                _Count++;
            }
            _Entries[ index ] = new Entry()
            {
                HashCode = hash,
                Key      = key,
                Value    = value,
                Next     = _Buckets[ bucket ],
            };
            _Buckets[ bucket ] = index;

            return (true);
        }
        [M(O.AggressiveInlining)] public bool TryAdd( in ByteListSegment key, T value, out T existsValue )
        {
            #region [.try find exists.]
            var hash   = InternalGetHashCode( key );
            var bucket = GetBucket( hash );
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref readonly var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && HashHelper.IsEqualByBytes( slot.Key.AsSpan(), key.AsSpan() ) )
                {
                    existsValue = slot.Value;
                    return (false);
                }
                i = slot.Next;
            }
            #endregion

            #region [.add new.]
            int index;
            if ( 0 <= _FreeList )
            {
                index = _FreeList;
                ref readonly var slot = ref _Entries[ index ];
                _FreeList = slot.Next;
            }
            else
            {
                if ( _Count == _Entries.Length )
                {
                    Resize();
                    bucket = GetBucket( hash );
                }
                index = _Count;
                _Count++;
            }
            _Entries[ index ] = new Entry() 
            {
                HashCode = hash,
                Value    = value,
                Key      = key,
                Next     = _Buckets[ bucket ],
            };
            _Buckets[ bucket ] = index;
            //_Count++;
            
            existsValue = default;
            return (true);
            #endregion
        }
        [M(O.AggressiveInlining)] public void AddOrUpdate( in ByteListSegment key, T value )
        {
            #region [.try find exists.]
            var hash   = InternalGetHashCode( key );
            var bucket = GetBucket( hash );
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && HashHelper.IsEqualByBytes( slot.Key.AsSpan(), key.AsSpan() ) )
                {
                    slot.Value = value;
                    return;
                }
                i = slot.Next;
            }
            #endregion

            #region [.add new.]
            int index;
            if ( 0 <= _FreeList )
            {
                index = _FreeList;
                ref readonly var slot = ref _Entries[ index ];
                _FreeList = slot.Next;
            }
            else
            {
                if ( _Count == _Entries.Length )
                {
                    Resize();
                    bucket = GetBucket( hash );
                }
                index = _Count;
                _Count++;
            }
            _Entries[ index ] = new Entry() 
            {
                HashCode = hash,
                Value    = value,
                Key      = key,
                Next     = _Buckets[ bucket ],
            };
            _Buckets[ bucket ] = index;
            //_Count++;
            #endregion
        }
        [M(O.AggressiveInlining)] public bool TryUpdate( in ByteListSegment key, T value )
        {
            #region [.try find exists.]
            var hash   = InternalGetHashCode( key );
            var bucket = GetBucket( hash );
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && HashHelper.IsEqualByBytes( slot.Key.AsSpan(), key.AsSpan() ) )
                {
                    slot.Value = value;
                    return (true);
                }
                i = slot.Next;
            }
            return (false);
            #endregion  
        }

        public void Add( in ByteListSegment key, T value ) => Insert( key, value, add: true );
        public void Clear()
        {
            if ( 0 < _Count )
            {
                Array.Fill( _Buckets, -1 );
                Array.Clear( _Entries, 0, _Count );
                _FreeList  = -1;
                _Count     = 0;
                _FreeCount = 0;
            }
        }
        public bool ContainsKey( in ByteListSegment key ) => (0 <= FindEntry( key ));
        public bool ContainsValue( T value )
        {
            if ( value == null )
            {
                for ( int i = 0; i < _Count; i++ )
                {
                    ref readonly var slot = ref _Entries[ i ];
                    if ( (0 <= slot.HashCode) && (slot.Value == null) )
                    {
                        return (true);
                    }
                }
            }
            else
            {
                var comp = EqualityComparer< T >.Default;
                for ( var i = 0; i < _Count; i++ )
                {
                    ref readonly var slot = ref _Entries[ i ];
                    if ( (0 <= slot.HashCode) && comp.Equals( slot.Value, value ) )
                    {
                        return (true);
                    }
                }
            }
            return (false);
        }

        [M(O.AggressiveInlining)] private int FindEntry( in ByteListSegment key )
        {
            var hash   = InternalGetHashCode( key );
            var bucket = GetBucket( hash );
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref readonly var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && HashHelper.IsEqualByBytes( slot.Key.AsSpan(), key.AsSpan() ) )
                {
                    return (i);
                }
                i = slot.Next;
            }
            return (-1);
        }
        [M(O.AggressiveInlining)] private void Insert( in ByteListSegment key, T value, bool add )
        {
            var hash   = InternalGetHashCode( key );
            var bucket = GetBucket( hash );
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];   
                if ( (slot.HashCode == hash) && HashHelper.IsEqualByBytes( slot.Key.AsSpan(), key.AsSpan() ) )
                {
                    if ( add ) throw (new ArgumentException( "Adding Duplicate" ));

                    slot.Value = value;
                    return;
                }
                i = slot.Next;
            }

            int index;
            if ( 0 < _FreeCount )
            {
                index = _FreeList;
                _FreeList = _Entries[ index ].Next;
                _FreeCount--;
            }
            else
            {
                if ( _Count == _Entries.Length )
                {
                    Resize();
                    bucket = GetBucket( hash );
                }
                index = _Count;
                _Count++;
            }
            _Entries[ index ] = new Entry()
            {
                HashCode = hash,
                Key      = key,
                Value    = value,
                Next     = _Buckets[ bucket ],
            };
            _Buckets[ bucket ] = index;
        }

        private void Resize()
        {
            var newSize    = HashHelpers.ExpandPrime( _Count );
            var newBuckets = new int  [ newSize ];
            var newEntries = new Entry[ newSize ];
            Array.Fill( newBuckets, -1 );
            Array.Copy( _Entries, 0, newEntries, 0, _Count );

            for ( var i = 0; i < _Count; i++ )
            {
                ref var slot = ref newEntries[ i ];
                if ( 0 <= slot.HashCode )
                {
                    var bucket = slot.HashCode % newSize;
                    slot.Next = newBuckets[ bucket ];
                    newBuckets[ bucket ] = i;
                }
            }
            _Buckets = newBuckets;
            _Entries = newEntries;

            _FastModMultiplier = GetFastModMultiplier( (uint) _Buckets.Length );
        }

        public bool Remove( in ByteListSegment key )
        {
            var hash   = InternalGetHashCode( key );
            var bucket = GetBucket( hash );
            var last   = -1;
            for ( var i = _Buckets[ bucket ]; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && HashHelper.IsEqualByBytes( slot.Key.AsSpan(), key.AsSpan() ) )
                {
                    if ( last < 0 )
                    {
                        _Buckets[ bucket ] = slot.Next;
                    }
                    else
                    {
                        _Entries[ last ].Next = slot.Next;
                    }
                    slot = new Entry()
                    {
                        HashCode = -1,
                        Next     = _FreeList,
                        //Key      = default,
                        //Value    = default,
                    };
                    _FreeList = i;
                    _FreeCount++;
                    return (true);
                }
                last = i;
                i = slot.Next;
            }

            return (false);
        }
        public bool TryGetValue( in ByteListSegment key, out T value )
        {
            #region [.try find exists.]
            var hash   = InternalGetHashCode( key );
            var bucket = GetBucket( hash );
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && HashHelper.IsEqualByBytes( slot.Key.AsSpan(), key.AsSpan() ) )
                {
                    value = slot.Value;
                    return (true);
                }
                i = slot.Next;
            }

            value = default;
            return (false);
            #endregion 
        }

        public ref T GetValueRefOrAddDefault( in ByteListSegment key, out bool exists )
        {
            #region [.try find exists.]
            var key_span = key.AsSpan();
            var hash   = InternalGetHashCode( key_span );
            var bucket = GetBucket( hash );
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];
#if DEBUG
                //if ( (slot.HashCode == hash) && !HashHelper.IsEqualByBytes( slot.Key.AsSpan(), key.AsSpan() ) )
                //{
                //    Console.WriteLine( "\r\n XZ \r\n" );
                //}
#endif   
                if ( (slot.HashCode == hash) && HashHelper.IsEqualByBytes( slot.Key.AsSpan(), key_span ) )
                {
                    exists = true;
                    return ref slot.Value;
                }
                i = slot.Next;
            }
            #endregion

            #region [.add new.]
            int index;
            if ( 0 <= _FreeList )
            {
                index = _FreeList;
                ref readonly var slot = ref _Entries[ index ];
                _FreeList = slot.Next;
            }
            else
            {
                if ( _Count == _Entries.Length )
                {
                    Resize();
                    bucket = GetBucket( hash );
                }
                index = _Count;
                _Count++;
            }

            ref var eslot = ref _Entries[ index ];
            eslot = new Entry() 
            {
                HashCode = hash,
                Value    = default,
                Key      = key,
                Next     = _Buckets[ bucket ],
            };
            _Buckets[ bucket ] = index;

            exists = false;
            return ref eslot.Value;
            #endregion
        }

        [M(O.AggressiveInlining)] private static ulong GetFastModMultiplier( uint divisor ) => (ulong.MaxValue / divisor) + 1;
        [M(O.AggressiveInlining)] private static uint FastMod( uint value, uint divisor, ulong multiplier )
        {
            Debug.Assert( divisor <= int.MaxValue );
            uint highbits = (uint) ( ( ( ((multiplier * value) >> 32) + 1) * divisor) >> 32);
            Debug.Assert( highbits == (value % divisor) );
            return (highbits);
        }
        //[M(O.AggressiveInlining)] private static uint FastMod( ulong value, uint divisor, ulong multiplier )
        //{
        //    Debug.Assert( divisor <= int.MaxValue );
        //    uint highbits = (uint) ( ( ( ((multiplier * value) >> 32) + 1) * divisor) >> 32);
        //    Debug.Assert( highbits == (value % divisor) );
        //    return (highbits);
        //}

        private ulong _FastModMultiplier;
        public ref T GetValueRefOrAddDefault( in ByteListSegment key )
        {
            #region [.try find exists.]
            var key_span = key.AsSpan();
            var hash   = InternalGetHashCode( key_span );
            var bucket = GetBucket( hash );

            //var x_hash = ((uint) hash) ^ ((uint) (hash >> 32)); 
            //var fastModBucket = FastMod( x_hash, (uint) _Buckets.Length, _FastModMultiplier );
            //Debug.Assert( fastModBucket == bucket );

            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];
#if DEBUG
                if ( (slot.HashCode == hash) && !HashHelper.IsEqualByBytes( slot.Key.AsSpan(), key_span ) )
                {
                    if ( Debugger.IsAttached ) Debugger.Break();
                    else Console.WriteLine( $"hash={hash}, slot.Key={slot.Key}, key={key}" );
                }
#endif
                if ( (slot.HashCode == hash) && HashHelper.IsEqualByBytes( slot.Key.AsSpan(), key_span ) )
                {
                    return ref slot.Value;
                }
                i = slot.Next;
            }
            #endregion

            #region [.add new.]
            int index;
            if ( 0 <= _FreeList )
            {
                index = _FreeList;
                ref readonly var slot = ref _Entries[ index ];
                _FreeList = slot.Next;
            }
            else
            {
                if ( _Count == _Entries.Length )
                {
                    Resize();
                    bucket = GetBucket( hash );
                }
                index = _Count;
                _Count++;
            }

            ref var eslot = ref _Entries[ index ];
            eslot = new Entry() 
            {
                HashCode = hash,
                Value    = default,
                Key      = key.ToArray(),
                Next     = _Buckets[ bucket ],
            };
            _Buckets[ bucket ] = index;

            return ref eslot.Value;
            #endregion
        }

        private void CopyTo( KeyValuePair< ByteListSegment, T >[] array, int index )
        {
            if ( array == null )                     throw (new ArgumentNullException( "array" ));
            if ( index < 0 || index > array.Length ) throw (new ArgumentOutOfRangeException());
            if ( array.Length - index < Count )      throw (new ArgumentException( "Arg_ArrayPlusOffTooSmall" ));
            
            for ( var i = 0; i < _Count; i++ )
            {
                ref readonly var slot = ref _Entries[ i ];
                if ( 0 <= slot.HashCode )
                {
                    array[ index++ ] = new KeyValuePair< ByteListSegment, T >( slot.Key, slot.Value );
                }
            }
        }
        void ICollection< KeyValuePair< ByteListSegment, T > >.CopyTo( KeyValuePair< ByteListSegment, T >[] array, int index ) => CopyTo( array, index );
        void ICollection.CopyTo( Array array, int index )
        {
            if ( array == null )                     throw (new ArgumentNullException( "array" ));
            if ( array.Rank != 1 )                   throw (new ArgumentException( "Arg_RankMultiDimNotSupported" ));
            if ( array.GetLowerBound( 0 ) != 0 )     throw (new ArgumentException( "Arg_NonZeroLowerBound" ));
            if ( index < 0 || index > array.Length ) throw (new ArgumentOutOfRangeException());
            if ( array.Length - index < Count )      throw (new ArgumentException( "Arg_ArrayPlusOffTooSmall" ));
            
            if ( array is KeyValuePair< ByteListSegment, T >[] array2 )
            {
                CopyTo( array2, index );
            }
            else if ( array is DictionaryEntry[] array3 )
            {
                for ( var i = 0; i < _Count; i++ )
                {
                    ref readonly var slot = ref _Entries[ i ];
                    if ( 0 <= slot.HashCode )
                    {
                        array3[ index++ ] = new DictionaryEntry( slot.Key, slot.Value );
                    }
                }
            }
            else if ( array is object[] array5 )
            { 
                for ( int i = 0; i < _Count; i++ )
                {
                    ref readonly var slot = ref _Entries[ i ];
                    if ( 0 <= slot.HashCode )
                    {
                        array5[ index++ ] = new KeyValuePair< ByteListSegment, T >( slot.Key, slot.Value );
                    }
                }
            }
            else
            {
                throw (new ArgumentException( "Argument_InvalidArrayType" ));
            }            
        }

        [M(O.AggressiveInlining)] private int GetBucket( long hash ) => (int) (hash % _Buckets.Length);
        [M(O.AggressiveInlining)] private static long InternalGetHashCode( in Span< byte > key_span ) => HashHelper.CalcLong( key_span ) & 0x7FFF_FFFF__FFFF_FFFF;//key.GetHashCode() & 0x7FFFFFFF;
        [M(O.AggressiveInlining)] private static long InternalGetHashCode( in ByteListSegment key ) => HashHelper.CalcLong( key.AsSpan() ) & 0x7FFF_FFFF__FFFF_FFFF;//key.GetHashCode() & 0x7FFFFFFF;
        [M(O.AggressiveInlining)] private static bool IsCompatibleKey( object key, out ByteListSegment kkey )
        {
            if ( key is ByteListSegment k )
            {
                kkey = k;
                return (true);
            }
            kkey = default;
            return (false);
        }

        public void Add( ByteListSegment key, T value ) => Add( in key, value );
        public bool ContainsKey( ByteListSegment key ) => ContainsKey( in key );
        public bool Remove( ByteListSegment key ) => Remove( in key );
        public bool TryGetValue( ByteListSegment key, out T value ) => TryGetValue( in key, out value );
    }
}