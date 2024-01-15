using System.Diagnostics;
using System.IO;

using static System.Collections.Specialized.BitVector32;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace System
{
    /// <summary>
    /// 
    /// </summary>
    unsafe internal static class FileByteBufferReader
    {
        /// <summary>
        /// 
        /// </summary>
        public interface IReadBufferCallback
        {
            void Callback( int readByteCount );
        }

        public static void Read( string filePath, IReadBufferCallback readBufferCallback, object readFileLock, byte[] readBuffer, in (long startIndex, long length)? section = null )
        {
            if ( readBufferCallback == null ) throw (new ArgumentNullException( nameof(readBufferCallback) ));
            if ( readFileLock       == null ) throw (new ArgumentNullException( nameof(readFileLock) ));

            if ( section.HasValue )
            {
                Read_Section( filePath, readBufferCallback, readFileLock, readBuffer, section.Value );
            }
            else
            {
                Read_Full( filePath, readBufferCallback, readFileLock, readBuffer );
            }
        }
        private static void Read_Full( string filePath, IReadBufferCallback readBufferCallback, object readFileLock, byte[] readBuffer )
        {
            using var fileHandle = File.OpenHandle( filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, FileOptions.SequentialScan, 0 );

            var fileOffset     = 0L;
            var readBufferSpan = readBuffer.AsSpan();
#if DEBUG
            var read_num       = 0;
            var total_read_cnt = 0L;
#endif
            for (; ; )
            {
#if DEBUG
                read_num++;
#endif
                var read_cnt = RandomAccess.Read( fileHandle, readBufferSpan, fileOffset );
                if ( read_cnt <= 0 ) break;
                fileOffset += read_cnt;
#if DEBUG
                total_read_cnt += read_cnt;
#endif
                var idx = readBuffer.LastIndexOfNewLine( read_cnt ); Debug.Assert( 0 <= idx );
                var rem_len = read_cnt - (idx + 1);
                if ( 0 < rem_len )
                {
                    fileOffset -= rem_len;
#if DEBUG
                    total_read_cnt -= rem_len;
#endif
                }
                readBufferCallback.Callback( idx /*read_cnt*/ );
            }
        }
        private static void Read_Section( string filePath, IReadBufferCallback readBufferCallback, object readFileLock, byte[] readBuffer, in (long startIndex, long length) section )
        {
            using var fileHandle = File.OpenHandle( filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, FileOptions.SequentialScan, 0 );

            var fileOffset     = section.startIndex;
            var section_length = section.length;
            var readBufferSpan = readBuffer.AsSpan();
#if DEBUG
            var read_num = 0;
#endif
            for ( var total_read_cnt = 0L; ; )
            {
#if DEBUG
                read_num++;
#endif
                int read_cnt;
                lock ( readFileLock )
                {
                    read_cnt = RandomAccess.Read( fileHandle, readBufferSpan, fileOffset );
                }
                if ( read_cnt <= 0 ) break;
                fileOffset += read_cnt;

                #region [.end of section.]
                total_read_cnt += read_cnt;
                var d = total_read_cnt - section_length;
                if ( 0 < d )
                {
                    read_cnt -= (int) d;
                    if ( 0 < read_cnt )
                    {
                        readBufferCallback.Callback( read_cnt );
                    }
                    break;
                }
                else
                {
                    var idx = readBuffer.LastIndexOfNewLine( read_cnt ); Debug.Assert( 0 <= idx );
                    var rem_len = read_cnt - (idx + 1);
                    if ( 0 < rem_len )
                    {
                        fileOffset     -= rem_len; 
                        total_read_cnt -= rem_len;
                    }
                    readBufferCallback.Callback( idx /*read_cnt*/ );
                }
                #endregion
            }
        }


        [M(O.AggressiveInlining)] private static int LastIndexOfNewLine( this byte[] buf, int length )
        {
            var span = buf.AsSpan( 0, length );
            for ( int i = length - 1; 0 <= i; i-- )
            {
                switch ( span[ i ] )
                {
                    case (byte) '\r':
                        return (i);

                    case (byte) '\n':
                        if  ( (0 < i) && (span[ i - 1 ] == '\r') )
                        {
                            i--;
                        }
                        return (i);
                }
            }
            return (-1);
        }


        [M(O.AggressiveInlining)] private static int Read_WithLock( this FileStream fs, object readFileLock, byte[] buffer )//, int offset, int count )
        {
            lock ( readFileLock )
            {
                return (fs.Read( buffer, 0/*offset*/, buffer.Length/*count*/ ));
            }
        }
    }
}