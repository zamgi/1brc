using System.Collections.ObjectModel;
using System.Diagnostics;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class ICollectionDebugView< T >
    {
        private ICollection< T > _Collection;
        public ICollectionDebugView( ICollection< T > collection ) => _Collection = collection ?? throw (new ArgumentNullException( nameof(collection) ));

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var items = new T[ _Collection.Count ];
                _Collection.CopyTo( items, 0 );
                return (items);
            }
        }
    }

	/// <summary>
	/// 
	/// </summary>
    [DebuggerTypeProxy( typeof(ICollectionDebugView<>) ), DebuggerDisplay("Count = {Count}")]
    internal sealed class DirectAccessList< T > : IList< T >, ICollection< T >, IEnumerable< T >, IEnumerable, IList, ICollection, IReadOnlyList< T >, IReadOnlyCollection< T >
    {
        /// <summary>
        /// 
        /// </summary>
        public struct Enumerator : IEnumerator< T >, IDisposable, IEnumerator
        {
            private DirectAccessList< T > _List;
            private int _Index;
            private T _Current;

            public T Current { [M(O.AggressiveInlining)] get => _Current; }
            object IEnumerator.Current
            {
                get
                {
                    if ( _Index == 0 || _Index == _List._Count + 1 ) throw (new InvalidOperationException( "ExceptionResource.InvalidOperation_EnumOpCantHappen" ));
                    return (Current);
                }
            }

            [M(O.AggressiveInlining)] internal Enumerator( DirectAccessList< T > list )
            {
                _List    = list;
                _Index   = 0;                
                _Current = default;
            }
            [M(O.AggressiveInlining)] public void Dispose() { }

            public bool MoveNext()
            {
                if ( (uint) _Index < (uint) _List._Count )
                {
                    _Current = _List._Items[ _Index ];
                    _Index++;
                    return (true);
                }
                return (MoveNextRare());
            }
            private bool MoveNextRare()
            {
                _Index   = _List._Count + 1;
                _Current = default;
                return (false);
            }

            void IEnumerator.Reset()
            {
                _Index   = 0;
                _Current = default;
            }
        }

        private T[] _Items;
        private int _Count;
        private static T[] EMPTY_ARRAY = new T[ 0 ];

        [M(O.AggressiveInlining)] public T[] GetInnerArray() => _Items;

        public int Capacity
        {
            get => _Items.Length;
            set
            {
                if ( value < _Count ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.value", "ExceptionResource.ArgumentOutOfRange_SmallCapacity" ));
              
                if ( value == _Items.Length )
                {
                    return;
                }
                if ( value > 0 )
                {
                    var array = new T[ value ];
                    if ( 0 < _Count )
                    {
                        Array.Copy( _Items, 0, array, 0, _Count );
                    }
                    _Items = array;
                }
                else
                {
                    _Items = EMPTY_ARRAY;
                }
            }
        }
        public int Count{ [M(O.AggressiveInlining)] get => _Count; }

        bool IList.IsFixedSize => false;
        bool ICollection< T >.IsReadOnly => false;
        bool IList.IsReadOnly => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => throw (new NotImplementedException());

        public T this[ int index ]
        {

            [M(O.AggressiveInlining)] get => _Items[ index ];
            [M(O.AggressiveInlining)] set => _Items[ index ] = value;
        }
        object IList.this[ int index ]
        {
            get => this[ index ];
            set => this[ index ] = (T) value;
        }

        public DirectAccessList() => _Items = EMPTY_ARRAY;
        public DirectAccessList( int capacity )
        {
            if ( capacity < 0 ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.capacity", "ExceptionResource.ArgumentOutOfRange_NeedNonNegNum" ));
            _Items = (capacity == 0) ? EMPTY_ARRAY : new T[ capacity ];
        }
        public DirectAccessList( IEnumerable< T > seq )
        {
            if ( seq == null ) throw (new ArgumentNullException( "ExceptionArgument.collection" ));
            
            if ( seq is ICollection< T > coll )
            {
                int count = coll.Count;
                if ( count == 0 )
                {
                    _Items = EMPTY_ARRAY;
                    return;
                }
                _Items = new T[ count ];
                coll.CopyTo( _Items, 0 );
                _Count = count;
            }
            else
            {
                _Count = 0;
                _Items = EMPTY_ARRAY;
                foreach ( var t in seq )
                {
                    Add( t );
                }
            }
        }
        public DirectAccessList( T[] array )
        {
            var count = array.Length;
            if ( count == 0 )
            {
                _Items = EMPTY_ARRAY;
            }
            else
            {
                _Items = new T[ count ];
                array.CopyTo( _Items, 0 );
                _Count = count;
            }
        }
        public DirectAccessList( IList< T > lst )
        {
            var count = lst.Count;
            if ( count == 0 )
            {
                _Items = EMPTY_ARRAY;
            }
            else
            {
                _Items = new T[ count ];
                lst.CopyTo( _Items, 0 );
                _Count = count;
            }
        }

        public void Add( T item )
        {
            if ( _Count == _Items.Length )
            {
                EnsureCapacity( _Count + 1 );
            }
            _Items[ _Count++ ] = item;
        }
        int IList.Add( object item )
        {
            Add( (T) item );
            return (Count - 1);
        }

        public void AddRange( IEnumerable< T > seq ) => InsertRange( _Count, seq );
        public void AddRange( T[] array )
        {
            EnsureCapacity( _Count + array.Length );
            array.CopyTo( _Items, _Count );
            _Count += array.Length;
        }
        public void AddRange( DirectAccessList< T > lst )
        {
            EnsureCapacity( _Count + lst.Count );
            lst.CopyTo( _Items, _Count );
            _Count += lst.Count;
        }
        public void AddRange( IList< T > lst )
        {
            EnsureCapacity( _Count + lst.Count );
            lst.CopyTo( _Items, _Count );
            _Count += lst.Count;
        }

        public ReadOnlyCollection< T > AsReadOnly() => new ReadOnlyCollection< T >( this );

        public int BinarySearch( int index, int count, T item, IComparer< T > comparer )
        {
            if ( index < 0 ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.index", "ExceptionResource.ArgumentOutOfRange_NeedNonNegNum" ));
            if ( count < 0 ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.count", "ExceptionResource.ArgumentOutOfRange_NeedNonNegNum" ));
            if ( _Count - index < count ) throw (new ArgumentException( "ExceptionResource.Argument_InvalidOffLen" ));
            
            return Array.BinarySearch( _Items, index, count, item, comparer );
        }
        public int BinarySearch( T item ) => BinarySearch( 0, Count, item, null );
        public int BinarySearch( T item, IComparer< T > comparer ) => BinarySearch( 0, Count, item, comparer );

        public void Clear()
        {
            if ( 0 < _Count )
            {
                Array.Clear( _Items, 0, _Count );
                _Count = 0;
            }
        }

        public bool Contains( T item )
        {
            if ( item == null )
            {
                for ( int i = 0; i < _Count; i++ )
                {
                    if ( _Items[ i ] == null )
                    {
                        return (true);
                    }
                }
                return (false);
            }
            var @default = EqualityComparer< T >.Default;
            for ( int j = 0; j < _Count; j++ )
            {
                if ( @default.Equals( _Items[ j ], item ) )
                {
                    return (true);
                }
            }
            return (false);
        }
        bool IList.Contains( object item ) => IsCompatibleObject( item )  && Contains( (T) item );

        public void CopyTo( T[] array ) => CopyTo( array, 0 );
        void ICollection.CopyTo( Array array, int arrayIndex )
        {
            if ( array != null && array.Rank != 1 ) throw (new ArgumentException( "ExceptionResource.Arg_RankMultiDimNotSupported" ));

            Array.Copy( _Items, 0, array, arrayIndex, _Count );
        }
        public void CopyTo( int index, T[] array, int arrayIndex, int count )
        {
            if ( _Count - index < count ) throw (new ArgumentException( "ExceptionResource.Argument_InvalidOffLen" ));
            
            Array.Copy( _Items, index, array, arrayIndex, count );
        }
        public void CopyTo( T[] array, int arrayIndex ) => Array.Copy( _Items, 0, array, arrayIndex, _Count );

        private void EnsureCapacity( int min )
        {
            if ( _Items.Length < min )
            {
                int n = ((_Items.Length == 0) ? 4 : (_Items.Length * 2));
                if ( (uint) n > 2146435071u )
                {
                    n = 2146435071;
                }
                if ( n < min )
                {
                    n = min;
                }
                Capacity = n;
            }
        }

        public bool Exists( Predicate< T > match ) => (FindIndex( match ) != -1);
        public T Find( Predicate< T > match )
        {
            for ( var i = 0; i < _Count; i++ )
            {
                if ( match( _Items[ i ] ) )
                {
                    return (_Items[ i ]);
                }
            }
            return (default);
        }
        public List< T > FindAll( Predicate< T > match )
        {
            var list = new List< T >( _Count );
            for ( int i = 0; i < _Count; i++ )
            {
                if ( match( _Items[ i ] ) )
                {
                    list.Add( _Items[ i ] );
                }
            }
            return (list);
        }
        public int FindIndex( Predicate< T > match ) => FindIndex( 0, _Count, match );
        public int FindIndex( int startIndex, Predicate< T > match ) => FindIndex( startIndex, _Count - startIndex, match );
        public int FindIndex( int startIndex, int count, Predicate< T > match )
        {
            if ( (uint) startIndex > (uint) _Count ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.startIndex", "ExceptionResource.ArgumentOutOfRange_Index" ));
            if ( count < 0 || startIndex > _Count - count ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.count", "ExceptionResource.ArgumentOutOfRange_Count" ));
            
            for ( int i = startIndex, end = startIndex + count; i < end; i++ )
            {
                if ( match( _Items[ i ] ) )
                {
                    return (i);
                }
            }
            return (-1);
        }
        public T FindLast( Predicate< T > match )
        {
            for ( var i = _Count - 1; i >= 0; i-- )
            {
                if ( match( _Items[ i ] ) )
                {
                    return (_Items[ i ]);
                }
            }
            return (default);
        }
        public int FindLastIndex( Predicate< T > match ) => FindLastIndex( _Count - 1, _Count, match );
        public int FindLastIndex( int startIndex, Predicate< T > match ) => FindLastIndex( startIndex, startIndex + 1, match );
        public int FindLastIndex( int startIndex, int count, Predicate< T > match )
        {
            if ( (_Count == 0) && (startIndex != -1) )     throw (new ArgumentOutOfRangeException( "ExceptionArgument.startIndex", "ExceptionResource.ArgumentOutOfRange_Index" ));
            if ( (uint) startIndex >= (uint) _Count )      throw (new ArgumentOutOfRangeException( "ExceptionArgument.startIndex", "ExceptionResource.ArgumentOutOfRange_Index" ));
            if ( count < 0 || startIndex - count + 1 < 0 ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.count", "ExceptionResource.ArgumentOutOfRange_Count" ));
            
            for ( int i = startIndex, end = startIndex - count; end < i; i-- )
            {
                if ( match( _Items[ i ] ) )
                {
                    return (i);
                }
            }
            return (-1);
        }
        public void ForEach( Action< T > action )
        {
            for ( var i = 0; i < _Count; i++ )
            {
                action( _Items[ i ] );
            }
        }
        
        [M(O.AggressiveInlining)] IEnumerator< T > IEnumerable< T >.GetEnumerator() => new Enumerator( this );
        public Enumerator GetEnumerator() => new Enumerator( this );
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator( this );

        public int IndexOf( T item ) => Array.IndexOf( _Items, item, 0, _Count );
        int IList.IndexOf( object item ) => IsCompatibleObject( item ) ? IndexOf( (T) item ) : -1;
        public int IndexOf( T item, int index )
        {
            if ( _Count < index ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.index", "ExceptionResource.ArgumentOutOfRange_Index" ));

            return Array.IndexOf( _Items, item, index, _Count - index );
        }
        public int IndexOf( T item, int index, int count )
        {
            if ( index > _Count ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.index", "ExceptionResource.ArgumentOutOfRange_Index" ));
            if ( count < 0 || index > _Count - count ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.count", "ExceptionResource.ArgumentOutOfRange_Count" ));

            return Array.IndexOf( _Items, item, index, count );
        }

        public void Insert( int index, T item )
        {
            if ( (uint) index > (uint) _Count ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.index", "ExceptionResource.ArgumentOutOfRange_ListInsert" ));

            if ( _Count == _Items.Length )
            {
                EnsureCapacity( _Count + 1 );
            }
            if ( index < _Count )
            {
                Array.Copy( _Items, index, _Items, index + 1, _Count - index );
            }
            _Items[ index ] = item;
            _Count++;
        }
        void IList.Insert( int index, object item ) => Insert( index, (T) item );

        public void InsertRange( int index, IEnumerable< T > seq )
        {
            if ( seq == null ) throw (new ArgumentNullException( "ExceptionArgument.collection" ));
            if ( (uint) index > (uint) _Count ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.index", "ExceptionResource.ArgumentOutOfRange_Index" ));

            if ( seq is T[] array )
            {
                var count = array.Length;
                if ( 0 < count )
                {
                    EnsureCapacity( _Count + count );
                    if ( index < _Count ) Array.Copy( _Items, index, _Items, index + count, _Count - index );

                    array.CopyTo( _Items, index );
                    _Count += count;
                }
            }
            else if ( seq is ICollection< T > coll )
            {
                var count = coll.Count;
                if ( 0 < count )
                {
                    EnsureCapacity( _Count + count );
                    if ( index < _Count ) Array.Copy( _Items, index, _Items, index + count, _Count - index );

                    if ( this == coll )
                    {
                        Array.Copy( _Items, 0, _Items, index, index );
                        Array.Copy( _Items, index + count, _Items, index * 2, _Count - index );
                    }
                    else
                    {
                        var tmp = new T[ count ];
                        coll.CopyTo( tmp, 0 );
                        tmp.CopyTo( _Items, index );
                    }
                    _Count += count;
                }
            }
            else
            {
                using var e = seq.GetEnumerator();
                while ( e.MoveNext() )
                {
                    Insert( index++, e.Current );
                }
            }
        }

        public int LastIndexOf( T item ) => (_Count == 0) ? -1 : LastIndexOf( item, _Count - 1, _Count );
        public int LastIndexOf( T item, int index )
        {
            if ( index >= _Count ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.index", "ExceptionResource.ArgumentOutOfRange_Index" ));

            return LastIndexOf( item, index, index + 1 );
        }
        public int LastIndexOf( T item, int index, int count )
        {
            if ( Count != 0 && index < 0 ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.index", "ExceptionResource.ArgumentOutOfRange_NeedNonNegNum" ));
            if ( Count != 0 && count < 0 ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.count", "ExceptionResource.ArgumentOutOfRange_NeedNonNegNum" ));
            if ( _Count == 0 )
            {
                return (-1);
            }
            if ( index >= _Count )   throw (new ArgumentOutOfRangeException( "ExceptionArgument.index", "ExceptionResource.ArgumentOutOfRange_BiggerThanCollection" ));
            if ( count > index + 1 ) throw (new ArgumentOutOfRangeException( "ExceptionArgument.count", "ExceptionResource.ArgumentOutOfRange_BiggerThanCollection" ));

            return Array.LastIndexOf( _Items, item, index, count );
        }
        
        public bool Remove( T item )
        {
            var i = IndexOf( item );
            if ( 0 <= i )
            {
                RemoveAt( i );
                return (true);
            }
            return (false);
        }
        void IList.Remove( object item )
        {
            if ( IsCompatibleObject( item ) )
            {
                Remove( (T) item );
            }
        }
        public void RemoveAt( int index )
        {
            if ( (uint) index >= (uint) _Count ) throw (new ArgumentOutOfRangeException());

            _Count--;
            if ( index < _Count )
            {
                Array.Copy( _Items, index + 1, _Items, index, _Count - index );
            }
            _Items[ _Count ] = default;
        }

        public T[] ToArray()
        {
            var array = new T[ _Count ];
            Array.Copy( _Items, 0, array, 0, _Count );
            return (array);
        }

        private static bool IsCompatibleObject( object value ) => (value is T) || ((value == null) && (default(T) == null));
    }
}
