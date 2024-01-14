using System.Diagnostics;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class MapDebugView< K, V >
	{
		private IDictionary< K, V > _Dict;
		public MapDebugView( IDictionary< K, V > dictionary ) => _Dict = dictionary ?? throw (new ArgumentNullException());

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public KeyValuePair< K, V >[] Items
		{
			get
			{
				var array = new KeyValuePair< K, V >[ _Dict.Count ];
				_Dict.CopyTo( array, 0 );
				return (array);
			}
		}
	}

    /// <summary>
    /// 
    /// </summary>
    internal sealed class MapCollectionDebugView< K, T >
    {
        private ICollection< K > _Collection;
        public MapCollectionDebugView( ICollection< K > collection ) => _Collection = collection ?? throw (new ArgumentNullException());

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public K[] Items
        {
            get
            {
                var array = new K[ _Collection.Count ];
                _Collection.CopyTo( array, 0 );
                return array;
            }
        }
    }
}