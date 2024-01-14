using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEqualityComparerByRef< T > //: IEqualityComparer< T >
    {
        bool Equals( in T x, in T y );
        int GetHashCode( in T obj );
    }

    /// <summary>
    /// 
    /// </summary>
    [DebuggerTypeProxy(typeof(MapDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    [ComVisible(false)]
    public class Map< K, T > : IDictionary< K, T >, ICollection< KeyValuePair< K, T > >, IReadOnlyDictionary< K, T >, IReadOnlyCollection< KeyValuePair< K, T > >, ICollection, IDictionary
    {
        /// <summary>
        /// 
        /// </summary>
        private struct Entry
        {
            public int HashCode;
            public int Next;
            public K   Key;
            public T   Value;
        }

        /// <summary>
        /// 
        /// </summary>
        public struct Enumerator : IEnumerator< KeyValuePair< K, T > >, IDisposable, IEnumerator, IDictionaryEnumerator
        {
            internal const int DictEntry    = 1;
            internal const int KeyValuePair = 2;

            private Map< K, T > _Map;
            private int _Index;
            private KeyValuePair< K, T > _Current;
            private int _RetTypeOfEnumerator;

            internal Enumerator( Map< K, T > dictionary, int retTypeOfEnumerator )
            {
                _Map = dictionary;
                _Index = 0;
                _RetTypeOfEnumerator = retTypeOfEnumerator;
                _Current = default;
            }

            public KeyValuePair< K, T > Current => _Current;
            public bool MoveNext()
            {
                while ( (uint) _Index < (uint) _Map._Count )
                {
                    if ( 0 <= _Map._Entries[ _Index ].HashCode )
                    {
                        _Current = new KeyValuePair< K, T >( _Map._Entries[ _Index ].Key, _Map._Entries[ _Index ].Value );
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
                        return new DictionaryEntry( _Current.Key, _Current.Value );
                    }
                    return new KeyValuePair< K, T >( _Current.Key, _Current.Value );
                }
            }
            DictionaryEntry IDictionaryEnumerator.Entry
            {

                get
                {
                    if ( _Index == 0 || (_Index == _Map._Count + 1) )
                    {
                        throw (new InvalidOperationException( "InvalidOperation_EnumOpCantHappen" ));
                    }
                    return new DictionaryEntry( _Current.Key, _Current.Value );
                }
            }
            object IDictionaryEnumerator.Key
            {
                get
                {
                    if ( _Index == 0 || _Index == _Map._Count + 1 )
                    {
                        throw (new InvalidOperationException( "InvalidOperation_EnumOpCantHappen" ));
                    }
                    return _Current.Key;
                }
            }
            object IDictionaryEnumerator.Value
            {
                get
                {
                    if ( _Index == 0 || _Index == _Map._Count + 1 )
                    {
                        throw (new InvalidOperationException( "InvalidOperation_EnumOpCantHappen" ));
                    }
                    return _Current.Value;
                }
            }
            void IEnumerator.Reset()
            {
                _Index = 0;
                _Current = default;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [DebuggerTypeProxy(typeof(MapCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {Count}")]
        public sealed class KeyCollection : ICollection< K >, IEnumerable< K >, IReadOnlyCollection< K >, ICollection
        {
            /// <summary>
            /// 
            /// </summary>
            public struct Enumerator : IEnumerator< K >, IDisposable, IEnumerator
            {
                private Map< K, T > _Map;
                private int _Index;
                private K _CurrentKey;

                public K Current => _CurrentKey;
                object IEnumerator.Current
                {
                    get
                    {
                        if ( _Index == 0 || _Index == _Map._Count + 1 ) throw (new InvalidOperationException( "InvalidOperation_EnumOpCantHappen" ));
                        return _CurrentKey;
                    }
                }
                internal Enumerator( Map< K, T > dictionary )
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

            private Map< K, T > _Map;

            public int Count => _Map.Count;
            bool ICollection< K >.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => ((ICollection) _Map).SyncRoot;

            public KeyCollection( Map< K, T > dictionary ) => _Map = dictionary ?? throw (new ArgumentNullException());
            public Enumerator GetEnumerator() => new Enumerator( _Map );

            public void CopyTo( K[] array, int index )
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

            void ICollection< K >.Add( K item ) => throw (new NotSupportedException());
            void ICollection< K >.Clear() => throw (new NotSupportedException());
            bool ICollection< K >.Contains( K item ) => _Map.ContainsKey( item );
            bool ICollection< K >.Remove( K item ) => throw (new NotSupportedException());

            IEnumerator< K > IEnumerable< K >.GetEnumerator() => new Enumerator( _Map );
            IEnumerator IEnumerable.GetEnumerator() => new Enumerator( _Map );

            void ICollection.CopyTo( Array array, int index )
            {
                if ( array == null )   throw (new ArgumentNullException( "array" ));
                if ( array.Rank != 1 ) throw (new ArgumentException( "Arg_RankMultiDimNotSupported" ));
                if ( array.GetLowerBound( 0 ) != 0 ) throw (new ArgumentException( "Arg_NonZeroLowerBound" ));
                if ( index < 0 || index > array.Length ) throw (new ArgumentOutOfRangeException());
                if ( array.Length - index < _Map.Count ) throw (new ArgumentException( "Arg_ArrayPlusOffTooSmall" ));
                
                if ( array is K[] array2 )
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
                private Map< K, T > _Map;
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

                internal Enumerator( Map< K, T > dictionary )
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

            private Map< K, T > _Map;
            public int Count => _Map.Count;
            bool ICollection< T >.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => ((ICollection) _Map).SyncRoot;

            public ValueCollection( Map< K, T > dictionary ) => _Map = dictionary ?? throw (new ArgumentNullException());
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

        private IEqualityComparerByRef< K > _Comparer;
        private KeyCollection   _Keys;
        private ValueCollection _Values;
        private object _SyncRoot;

        public Map( IEqualityComparerByRef< K > comparer ): this( 0, comparer ) { }
        public Map( int capacity, IEqualityComparerByRef< K > comparer )
        {
            if ( capacity < 0 ) throw (new ArgumentOutOfRangeException( nameof(capacity) ));
            _Comparer = comparer ?? throw (new ArgumentNullException( nameof(comparer) ));

            Init( capacity );            
        }
        public Map( IDictionary< K, T > dictionary ): this( dictionary, null ) { }
        public Map( IDictionary< K, T > dictionary, IEqualityComparerByRef< K > comparer ) : this( dictionary?.Count ?? 0, comparer )
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
        }

        public IEqualityComparerByRef< K > Comparer => _Comparer;
        public int Count => (_Count - _FreeCount);
        public KeyCollection Keys
        {
            get
            {
                if ( _Keys == null ) _Keys = new KeyCollection( this );
                return _Keys;
            }
        }
        ICollection< K > IDictionary< K, T >.Keys => this.Keys;
        IEnumerable< K > IReadOnlyDictionary< K, T >.Keys => this.Keys;

        public ValueCollection Values
        {
            get
            {
                if ( _Values == null ) _Values = new ValueCollection( this );
                return _Values;
            }
        }
        ICollection< T > IDictionary< K, T >.Values => this.Values;
        IEnumerable< T > IReadOnlyDictionary< K, T >.Values => this.Values;

        public T this[ K key ]
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
                this[ (K) key ] = (T) value;
            }
        }

        bool ICollection< KeyValuePair< K, T > >.IsReadOnly => false;
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

        void ICollection< KeyValuePair< K, T > >.Add( KeyValuePair< K, T > p ) => Add( p.Key, p.Value );
        bool ICollection< KeyValuePair< K, T > >.Contains( KeyValuePair< K, T > p )
        {
            var i = FindEntry( p.Key );
            return ((0 <= i) && EqualityComparer< T >.Default.Equals( _Entries[ i ].Value, p.Value ));
        }
        bool ICollection< KeyValuePair< K, T > >.Remove( KeyValuePair< K, T > p )
        {
            var i = FindEntry( p.Key );
            if ( (0 <= i) && EqualityComparer< T >.Default.Equals( _Entries[ i ].Value, p.Value ) )
            {
                Remove( p.Key );
                return (true);
            }
            return (false);
        }

        void IDictionary.Add( object key, object value ) => Add( (K) key, (T) value );
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
        IEnumerator< KeyValuePair< K, T > > IEnumerable< KeyValuePair< K, T > >.GetEnumerator() => new Enumerator( this, Enumerator.KeyValuePair );
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator( this, Enumerator.KeyValuePair );

        public bool TryAdd( in K key, T value )
        {
            var hash   = InternalGetHashCode( key );
            var bucket = hash % _Buckets.Length;
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref readonly var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && _Comparer.Equals( slot.Key, key ) )
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
                    bucket = hash % _Buckets.Length;
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
        [M(O.AggressiveInlining)] public bool TryAdd( in K key, T value, out T existsValue )
        {
            #region [.try find exists.]
            var hash   = InternalGetHashCode( key );
            var bucket = hash % _Buckets.Length;
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref readonly var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && _Comparer.Equals( slot.Key, key ) )
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
                    bucket = hash % _Buckets.Length;
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
        [M(O.AggressiveInlining)] public void AddOrUpdate( in K key, T value )
        {
            #region [.try find exists.]
            var hash   = InternalGetHashCode( key );
            var bucket = hash % _Buckets.Length;
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && _Comparer.Equals( slot.Key, key ) )
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
                    bucket = hash % _Buckets.Length;
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
        [M(O.AggressiveInlining)] public bool TryUpdate( in K key, T value )
        {
            #region [.try find exists.]
            var hash   = InternalGetHashCode( key );
            var bucket = hash % _Buckets.Length;
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && _Comparer.Equals( slot.Key, key ) )
                {
                    slot.Value = value;
                    return (true);
                }
                i = slot.Next;
            }
            return (false);
            #endregion  
        }

        public void Add( in K key, T value ) => Insert( key, value, add: true );
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
        public bool ContainsKey( in K key ) => (0 <= FindEntry( key ));
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

        private int FindEntry( in K key )
        {
            var hash   = InternalGetHashCode( key );
            var bucket = hash % _Buckets.Length;
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref readonly var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && _Comparer.Equals( slot.Key, key ) )
                {
                    return (i);
                }
                i = slot.Next;
            }
            return (-1);
        }
        private void Insert( in K key, T value, bool add )
        {
            var hash   = InternalGetHashCode( key );
            var bucket = hash % _Buckets.Length;
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];   
                if ( (slot.HashCode == hash) && _Comparer.Equals( slot.Key, key ) )
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
                    bucket = hash % _Buckets.Length;
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
        }

        public bool Remove( in K key )
        {
            var hash   = InternalGetHashCode( key );
            var bucket = hash % _Buckets.Length;
            var last   = -1;
            for ( var i = _Buckets[ bucket ]; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && _Comparer.Equals( slot.Key, key ) )
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
        public bool TryGetValue( in K key, out T value )
        {
            #region [.try find exists.]
            var hash   = InternalGetHashCode( key );
            var bucket = hash % _Buckets.Length;
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];
                if ( (slot.HashCode == hash) && _Comparer.Equals( slot.Key, key ) )
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

        public ref T GetValueRefOrAddDefault( in K key, out bool exists )
        {
            #region [.try find exists.]
            var hash   = InternalGetHashCode( key );
            var bucket = hash % _Buckets.Length;
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];
#if DEBUG
                //if ( (slot.HashCode == hash) && !_Comparer.Equals( slot.Key, key ) )
                //{
                //    Console.WriteLine( "\r\n XZ \r\n" );
                //}
#endif   
                if ( (slot.HashCode == hash) && _Comparer.Equals( slot.Key, key ) )
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
                    bucket = hash % _Buckets.Length;
                }
                index = _Count;
                _Count++;
            }

            _Entries[ index ] = new Entry() 
            {
                HashCode = hash,
                Value    = default,
                Key      = key,
                Next     = _Buckets[ bucket ],
            };
            _Buckets[ bucket ] = index;

            exists = false;
            return ref _Entries[ index ].Value;
            #endregion
        }
        public ref T GetValueRefOrAddDefault( in K key, Func< K, K > getNewKeyFunc, out bool exists )
        {
            #region [.try find exists.]
            var hash   = InternalGetHashCode( key );
            var bucket = hash % _Buckets.Length;
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];
                //if ( (slot.HashCode == hash) && !_Comparer.Equals( slot.Key, key ) )
                //{
                //    Console.WriteLine( "XZ" );
                //}
                if ( (slot.HashCode == hash) && _Comparer.Equals( slot.Key, key ) )
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
                    bucket = hash % _Buckets.Length;
                }
                index = _Count;
                _Count++;
            }

            _Entries[ index ] = new Entry() 
            {
                HashCode = hash,
                Value    = default,
                Key      = getNewKeyFunc( key ),
                Next     = _Buckets[ bucket ],
            };
            _Buckets[ bucket ] = index;

            exists = false;
            return ref _Entries[ index ].Value;
            #endregion
        }
        public ref T GetValueRefOrAddDefault( in K key, Func< K, K > getNewKeyFunc )
        {
            #region [.try find exists.]
            var hash   = InternalGetHashCode( key );
            var bucket = hash % _Buckets.Length;
            for ( var i = _Buckets[ bucket ]/* - 1*/; 0 <= i; /*i = _Entries[ i ].Next*/ )
            {
                ref var slot = ref _Entries[ i ];
                //if ( (slot.HashCode == hash) && !_Comparer.Equals( slot.Key, key ) )
                //{
                //    Console.WriteLine( "XZ" );
                //}
                if ( (slot.HashCode == hash) && _Comparer.Equals( slot.Key, key ) )
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
                    bucket = hash % _Buckets.Length;
                }
                index = _Count;
                _Count++;
            }

            _Entries[ index ] = new Entry() 
            {
                HashCode = hash,
                Value    = default,
                Key      = getNewKeyFunc( key ),
                Next     = _Buckets[ bucket ],
            };
            _Buckets[ bucket ] = index;

            return ref _Entries[ index ].Value;
            #endregion
        }

        private void CopyTo( KeyValuePair< K, T >[] array, int index )
        {
            if ( array == null )                     throw (new ArgumentNullException( "array" ));
            if ( index < 0 || index > array.Length ) throw (new ArgumentOutOfRangeException());
            if ( array.Length - index < Count )      throw (new ArgumentException( "Arg_ArrayPlusOffTooSmall" ));
            
            for ( var i = 0; i < _Count; i++ )
            {
                ref readonly var slot = ref _Entries[ i ];
                if ( 0 <= slot.HashCode )
                {
                    array[ index++ ] = new KeyValuePair< K, T >( slot.Key, slot.Value );
                }
            }
        }
        void ICollection< KeyValuePair< K, T > >.CopyTo( KeyValuePair< K, T >[] array, int index ) => CopyTo( array, index );
        void ICollection.CopyTo( Array array, int index )
        {
            if ( array == null )                     throw (new ArgumentNullException( "array" ));
            if ( array.Rank != 1 )                   throw (new ArgumentException( "Arg_RankMultiDimNotSupported" ));
            if ( array.GetLowerBound( 0 ) != 0 )     throw (new ArgumentException( "Arg_NonZeroLowerBound" ));
            if ( index < 0 || index > array.Length ) throw (new ArgumentOutOfRangeException());
            if ( array.Length - index < Count )      throw (new ArgumentException( "Arg_ArrayPlusOffTooSmall" ));
            
            if ( array is KeyValuePair< K, T >[] array2 )
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
                        array5[ index++ ] = new KeyValuePair< K, T >( slot.Key, slot.Value );
                    }
                }
            }
            else
            {
                throw (new ArgumentException( "Argument_InvalidArrayType" ));
            }            
        }
        
        [M(O.AggressiveInlining)] private int InternalGetHashCode( in K key ) => _Comparer.GetHashCode( key ) & 0x7FFFFFFF;
        [M(O.AggressiveInlining)] private static bool IsCompatibleKey( object key, out K kkey )
        {
            if ( key is K k )
            {
                kkey = k;
                return (true);
            }
            kkey = default;
            return (false);
        }

        public void Add( K key, T value ) => Add( in key, value );
        public bool ContainsKey( K key ) => ContainsKey( in key );
        public bool Remove( K key ) => Remove( in key );
        public bool TryGetValue( K key, out T value ) => TryGetValue( in key, out value );
    }
}