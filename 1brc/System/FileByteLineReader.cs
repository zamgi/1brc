using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace System
{
    /// <summary>
    /// 
    /// </summary>
    unsafe internal static class FileByteLineReader
    {
        private const int INNER_BUFFER_CAPACITY = 4_096;

        public  static IEnumerable< ListSegment< byte > > ReadByLine( string filePath, (long startIndex, long length)? section = null, int innerBufferCapacity = INNER_BUFFER_CAPACITY )
        {
            if ( section.HasValue )
            {
                return (ReadByLine_Section( filePath, section.Value, innerBufferCapacity ));
            }
            return (ReadByLine_Full( filePath, innerBufferCapacity ));
        }
        private static IEnumerable< ListSegment< byte > > ReadByLine_Full( string filePath, int innerBufferCapacity )
        {
            using var fs = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0, FileOptions.SequentialScan );

            var buf       = new byte[ innerBufferCapacity ];
            var remainder = new DirectAccessList< byte >( innerBufferCapacity );
            var tmp       = new DirectAccessList< byte >( innerBufferCapacity );
#if DEBUG
            var line_num = 0L;
            var read_num = 0L; 
#endif
            for (; ; )
            {
#if DEBUG
                read_num++; 
#endif
                var read_cnt = fs.Read( buf, 0, buf.Length );
                if ( read_cnt <= 0 ) break;

                var startIndex = 0;
                if ( 0 < remainder.Count )
                {
                    var i = buf.IndexOfNewLine( 0/*startIndex*/, read_cnt, out var skip_char_cnt );
                    if ( i != -1 )
                    {
#if DEBUG
                        line_num++; 
#endif
                        tmp.AddRange( remainder ); remainder.Clear();
                        var seg = new ListSegment< byte >( buf, 0/*startIndex*/, i );
                        tmp.AddRange( seg );
                        seg = new ListSegment< byte >( tmp );
                        yield return (seg);
                        tmp.Clear();

                        startIndex = i + 1 + skip_char_cnt;
                    }
                }
                
                for (; ; )
                {
                    var i = buf.IndexOfNewLine( startIndex, read_cnt, out var skip_char_cnt );
                    if ( i == -1 )
                    {
                        break;
                    }
#if DEBUG
                    line_num++; 
#endif
                    var seg = new ListSegment< byte >( buf, startIndex, i - startIndex );
                    yield return (seg);

                    startIndex = i + 1 + skip_char_cnt;
                }

                var rem_len = read_cnt - startIndex;
                if ( 0 < rem_len )
                {
                    var seg = new ListSegment< byte >( buf, startIndex, rem_len );
                    remainder.AddRange( seg );
                }
            }

            if ( 0 < remainder.Count )
            {
#if DEBUG
                line_num++; 
#endif
                var seg = new ListSegment< byte >( remainder );
                yield return (seg);
            }
        }
        private static IEnumerable< ListSegment< byte > > ReadByLine_Section( string filePath, (long startIndex, long length) section, int innerBufferCapacity )
        {
            using var fs = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0, FileOptions.SequentialScan );

            fs.Seek( section.startIndex, SeekOrigin.Begin );

            var buf       = new byte[ innerBufferCapacity ];
            var remainder = new DirectAccessList< byte >( innerBufferCapacity );
            var tmp       = new DirectAccessList< byte >( innerBufferCapacity );
#if DEBUG
            var line_num = 0;
            var read_num = 0; 
#endif
            for ( var total_read_cnt = 0L; ; )
            {
#if DEBUG
                read_num++; 
#endif
                var read_cnt = fs.Read( buf, 0, buf.Length );
                if ( read_cnt <= 0 ) break;

                #region [.end of section.]
                total_read_cnt += read_cnt;
                var d = total_read_cnt - section.length;
                if ( 0 < d )
                {
                    read_cnt -= (int) d;
                    if ( read_cnt == 0 )
                    {
                        break;
                    }
                }
                #endregion

                var startIndex = 0;
                if ( 0 < remainder.Count )
                {
                    var i = buf.IndexOfNewLine( 0/*startIndex*/, read_cnt, out var skip_char_cnt );
                    if ( i != -1 )
                    {
#if DEBUG
                        line_num++; 
#endif
                        tmp.AddRange( remainder ); remainder.Clear();
                        var seg = new ListSegment< byte >( buf, 0/*startIndex*/, i );
                        tmp.AddRange( seg );
                        seg = new ListSegment< byte >( tmp );
                        yield return (seg);
                        tmp.Clear();

                        startIndex = i + 1 + skip_char_cnt;
                    }
                }
                
                for (; ; )
                {
                    var i = buf.IndexOfNewLine( startIndex, read_cnt, out var skip_char_cnt );
                    if ( i == -1 )
                    {
                        break;
                    }
#if DEBUG
                    line_num++; 
#endif
                    var seg = new ListSegment< byte >( buf, startIndex, i - startIndex );
                    yield return (seg);

                    startIndex = i + 1 + skip_char_cnt;
                }

                var rem_len = read_cnt - startIndex;
                if ( 0 < rem_len )
                {
                    var seg = new ListSegment< byte >( buf, startIndex, rem_len );
                    remainder.AddRange( seg );
                }

                if ( 0 < d )
                {
                    break;
                }
            }

            if ( 0 < remainder.Count )
            {
#if DEBUG
                line_num++; 
#endif
                var seg = new ListSegment< byte >( remainder );
                yield return (seg);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public interface IReadByLineCallback
        {
            void Callback( in ListSegment< byte > line_seg );
        }
        public  static void ReadByLine( string filePath, IReadByLineCallback readByLineCallback, (long startIndex, long length)? section = null, int innerBufferCapacity = INNER_BUFFER_CAPACITY )
        {
            if ( readByLineCallback == null ) throw (new ArgumentNullException( nameof(readByLineCallback) ));

            if ( section.HasValue )
            {
                ReadByLine_Section( filePath, readByLineCallback, section.Value, innerBufferCapacity );
            }
            else
            {
                ReadByLine_Full( filePath, readByLineCallback, innerBufferCapacity );
            }
        }
        private static void ReadByLine_Full( string filePath, IReadByLineCallback readByLineCallback, int innerBufferCapacity )
        {
            using var fs = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0, FileOptions.SequentialScan );

            var buf       = new byte[ innerBufferCapacity ];
            var remainder = new DirectAccessList< byte >( innerBufferCapacity );
            var tmp       = new DirectAccessList< byte >( innerBufferCapacity );
#if DEBUG
            var line_num = 0L;
            var read_num = 0L; 
#endif
            for (; ; )
            {
#if DEBUG
                read_num++; 
#endif
                var read_cnt = fs.Read( buf, 0, buf.Length );
                if ( read_cnt <= 0 ) break;

                var startIndex = 0;
                if ( 0 < remainder.Count )
                {
                    var i = buf.IndexOfNewLine( 0/*startIndex*/, read_cnt, out var skip_char_cnt );
                    if ( i != -1 )
                    {
#if DEBUG
                        line_num++; 
#endif
                        tmp.AddRange( remainder ); remainder.Clear();
                        var seg = new ListSegment< byte >( buf, 0/*startIndex*/, i );
                        tmp.AddRange( seg );
                        seg = new ListSegment< byte >( tmp );
                        readByLineCallback.Callback( seg );
                        tmp.Clear();

                        startIndex = i + 1 + skip_char_cnt;
                    }
                }
                
                for (; ; )
                {
                    var i = buf.IndexOfNewLine( startIndex, read_cnt, out var skip_char_cnt );
                    if ( i == -1 )
                    {
                        break;
                    }
#if DEBUG
                    line_num++; 
#endif
                    var seg = new ListSegment< byte >( buf, startIndex, i - startIndex );
                    readByLineCallback.Callback( seg );

                    startIndex = i + 1 + skip_char_cnt;
                }

                var rem_len = read_cnt - startIndex;
                if ( 0 < rem_len )
                {
                    var seg = new ListSegment< byte >( buf, startIndex, rem_len );
                    remainder.AddRange( seg );
                }
            }

            if ( 0 < remainder.Count )
            {
#if DEBUG
                line_num++; 
#endif
                var seg = new ListSegment< byte >( remainder );
                readByLineCallback.Callback( seg );
            }
        }
        private static void ReadByLine_Section( string filePath, IReadByLineCallback readByLineCallback, (long startIndex, long length) section, int innerBufferCapacity )
        {
            using var fs = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0, FileOptions.SequentialScan );

            fs.Seek( section.startIndex, SeekOrigin.Begin );

            var buf       = new byte[ innerBufferCapacity ];
            var remainder = new DirectAccessList< byte >( innerBufferCapacity );
            var tmp       = new DirectAccessList< byte >( innerBufferCapacity );
#if DEBUG
            var line_num = 0;
            var read_num = 0; 
#endif
            for ( var total_read_cnt = 0L; ; )
            {
#if DEBUG
                read_num++; 
#endif
                var read_cnt = fs.Read( buf, 0, buf.Length );
                if ( read_cnt <= 0 ) break;

                #region [.end of section.]
                total_read_cnt += read_cnt;
                var d = total_read_cnt - section.length;
                if ( 0 < d )
                {
                    read_cnt -= (int) d;
                    if ( read_cnt == 0 )
                    {
                        break;
                    }
                }
                #endregion

                var startIndex = 0;
                if ( 0 < remainder.Count )
                {
                    var i = buf.IndexOfNewLine( 0/*startIndex*/, read_cnt, out var skip_char_cnt );
                    if ( i != -1 )
                    {
#if DEBUG
                        line_num++; 
#endif
                        tmp.AddRange( remainder ); remainder.Clear();
                        var seg = new ListSegment< byte >( buf, 0/*startIndex*/, i );
                        tmp.AddRange( seg );
                        seg = new ListSegment< byte >( tmp );
                        readByLineCallback.Callback( seg );
                        tmp.Clear();

                        startIndex = i + 1 + skip_char_cnt;
                    }
                }
                
                for (; ; )
                {
                    var i = buf.IndexOfNewLine( startIndex, read_cnt, out var skip_char_cnt );
                    if ( i == -1 )
                    {
                        break;
                    }
#if DEBUG
                    line_num++; 
#endif
                    var seg = new ListSegment< byte >( buf, startIndex, i - startIndex );
                    readByLineCallback.Callback( seg );

                    startIndex = i + 1 + skip_char_cnt;
                }

                var rem_len = read_cnt - startIndex;
                if ( 0 < rem_len )
                {
                    var seg = new ListSegment< byte >( buf, startIndex, rem_len );
                    remainder.AddRange( seg );
                }

                #region [.end of section.]
                if ( 0 < d )
                {
                    break;
                }
                #endregion
            }

            if ( 0 < remainder.Count )
            {
#if DEBUG
                line_num++; 
#endif
                var seg = new ListSegment< byte >( remainder );
                readByLineCallback.Callback( seg );
            }
        }


        public static void ReadByLine( string filePath, IReadByLineCallback readByLineCallback, object readFileLock, (long startIndex, long length)? section = null, int innerBufferCapacity = INNER_BUFFER_CAPACITY )
        {
            if ( readByLineCallback == null ) throw (new ArgumentNullException( nameof(readByLineCallback) ));
            if ( readFileLock       == null ) throw (new ArgumentNullException( nameof(readFileLock) ));

            if ( section.HasValue )
            {
                ReadByLine_Section( filePath, readByLineCallback, readFileLock, section.Value, innerBufferCapacity );
            }
            else
            {
                ReadByLine_Full( filePath, readByLineCallback, readFileLock, innerBufferCapacity );
            }
        }
        private static void ReadByLine_Full( string filePath, IReadByLineCallback readByLineCallback, object readFileLock, int innerBufferCapacity )
        {
            using var fs = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0, FileOptions.SequentialScan );

            var buf       = new byte[ innerBufferCapacity ];
            var remainder = new DirectAccessList< byte >( innerBufferCapacity );
            var tmp       = new DirectAccessList< byte >( innerBufferCapacity );
#if DEBUG
            var line_num = 0L;
            var read_num = 0L; 
#endif
            for (; ; )
            {
#if DEBUG
                read_num++; 
#endif
                var read_cnt = fs.Read_WithLock( readFileLock, buf );
                if ( read_cnt <= 0 ) break;

                var startIndex = 0;
                if ( 0 < remainder.Count )
                {
                    var i = buf.IndexOfNewLine( 0/*startIndex*/, read_cnt, out var skip_char_cnt );
                    if ( i != -1 )
                    {
#if DEBUG
                        line_num++; 
#endif
                        tmp.AddRange( remainder ); remainder.Clear();
                        var seg = new ListSegment< byte >( buf, 0/*startIndex*/, i );
                        tmp.AddRange( seg );
                        seg = new ListSegment< byte >( tmp );
                        readByLineCallback.Callback( seg );
                        tmp.Clear();

                        startIndex = i + 1 + skip_char_cnt;
                    }
                }
                
                for (; ; )
                {
                    var i = buf.IndexOfNewLine( startIndex, read_cnt, out var skip_char_cnt );
                    if ( i == -1 )
                    {
                        break;
                    }
#if DEBUG
                    line_num++; 
#endif
                    var seg = new ListSegment< byte >( buf, startIndex, i - startIndex );
                    readByLineCallback.Callback( seg );

                    startIndex = i + 1 + skip_char_cnt;
                }

                var rem_len = read_cnt - startIndex;
                if ( 0 < rem_len )
                {
                    var seg = new ListSegment< byte >( buf, startIndex, rem_len );
                    remainder.AddRange( seg );
                }
            }

            if ( 0 < remainder.Count )
            {
#if DEBUG
                line_num++; 
#endif
                var seg = new ListSegment< byte >( remainder );
                readByLineCallback.Callback( seg );
            }
        }
        private static void ReadByLine_Section( string filePath, IReadByLineCallback readByLineCallback, object readFileLock, (long startIndex, long length) section, int innerBufferCapacity )
        {
            using var fs = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0, FileOptions.SequentialScan );

            fs.Seek( section.startIndex, SeekOrigin.Begin );

            var buf       = new byte[ innerBufferCapacity ];
            var remainder = new DirectAccessList< byte >( innerBufferCapacity );
            var tmp       = new DirectAccessList< byte >( innerBufferCapacity );
#if DEBUG
            var line_num = 0;
            var read_num = 0; 
#endif
            for ( var total_read_cnt = 0L; ; )
            {
#if DEBUG
                read_num++; 
#endif
                var read_cnt = fs.Read_WithLock( readFileLock, buf );
                if ( read_cnt <= 0 ) break;

                #region [.end of section.]
                total_read_cnt += read_cnt;
                var d = total_read_cnt - section.length;
                if ( 0 < d )
                {
                    read_cnt -= (int) d;
                    if ( read_cnt == 0 )
                    {
                        break;
                    }
                }
                #endregion

                var startIndex = 0;
                if ( 0 < remainder.Count )
                {
                    var i = buf.IndexOfNewLine( 0/*startIndex*/, read_cnt, out var skip_char_cnt );
                    if ( i != -1 )
                    {
#if DEBUG
                        line_num++; 
#endif
                        tmp.AddRange( remainder ); remainder.Clear();
                        var seg = new ListSegment< byte >( buf, 0/*startIndex*/, i );
                        tmp.AddRange( seg );
                        seg = new ListSegment< byte >( tmp );
                        readByLineCallback.Callback( seg );
                        tmp.Clear();

                        startIndex = i + 1 + skip_char_cnt;
                    }
                }
                
                for (; ; )
                {
                    var i = buf.IndexOfNewLine( startIndex, read_cnt, out var skip_char_cnt );
                    if ( i == -1 )
                    {
                        break;
                    }
#if DEBUG
                    line_num++; 
#endif
                    var seg = new ListSegment< byte >( buf, startIndex, i - startIndex );
                    readByLineCallback.Callback( seg );

                    startIndex = i + 1 + skip_char_cnt;
                }

                var rem_len = read_cnt - startIndex;
                if ( 0 < rem_len )
                {
                    var seg = new ListSegment< byte >( buf, startIndex, rem_len );
                    remainder.AddRange( seg );
                }

                #region [.end of section.]
                if ( 0 < d )
                {
                    break;
                }
                #endregion
            }

            if ( 0 < remainder.Count )
            {
#if DEBUG
                line_num++; 
#endif
                var seg = new ListSegment< byte >( remainder );
                readByLineCallback.Callback( seg );
            }
        }


        [M(O.AggressiveInlining)] private static int IndexOfNewLine( this IList< byte > buf, int startIndex, out int skip_char_cnt )
        {
            for ( int i = startIndex, end = buf.Count - 1; i <= end; i++ )
            {
                switch ( buf[ i ] )
                {
                    case (byte) '\r':
                        skip_char_cnt = (i != end) && (buf[ i + 1 ] == '\n') ? 1 : 0;
                        return (i);

                    case (byte) '\n':
                        skip_char_cnt = 0; 
                        return (i);
                }
            }
            skip_char_cnt = 0;
            return (-1);
        }
        [M(O.AggressiveInlining)] private static int IndexOfNewLine( this IList< byte > buf, int startIndex, int length, out int skip_char_cnt )
        {
            for ( int i = startIndex, end = length - 1; i <= end; i++ )
            {
                switch ( buf[ i ] )
                {
                    case (byte) '\r':
                        skip_char_cnt = (i != end) && (buf[ i + 1 ] == '\n') ? 1 : 0;
                        return (i);

                    case (byte) '\n':
                        skip_char_cnt = 0; 
                        return (i);
                }
            }
            skip_char_cnt = 0;
            return (-1);
        }
        [M(O.AggressiveInlining)] private static int IndexOfNewLine( this byte[] buf, int startIndex, int length, out int skip_char_cnt )
        {
            for ( int i = startIndex, end = length - 1; i <= end; i++ )
            {
                switch ( buf[ i ] )
                {
                    case (byte) '\r':
                        skip_char_cnt = (i != end) && (buf[ i + 1 ] == '\n') ? 1 : 0;
                        return (i);

                    case (byte) '\n':
                        skip_char_cnt = 0; 
                        return (i);
                }
            }
            skip_char_cnt = 0;
            return (-1);
        }


        [M(O.AggressiveInlining)] private static int Read_WithLock( this FileStream fs, object readFileLock, byte[] buffer )//, int offset, int count )
        {
            lock ( readFileLock )
            {
                return (fs.Read( buffer, 0/*offset*/, buffer.Length/*count*/ ));
            }
        }


        public static string AsText( this in ListSegment< byte > seg ) => Encoding.UTF8.GetString( seg.ToArray() );
        public static string AsText( this IList< byte > buf ) => Encoding.UTF8.GetString( buf.ToArray() );
        public static string AsText( this IList< byte > buf, int len ) => Encoding.UTF8.GetString( buf.ToArray(), 0, len );
        public static string AsText( this IList< byte > buf, int i, int len ) => Encoding.UTF8.GetString( buf.ToArray(), i, len );

        public static void Test( string filePath, (long startIndex, long length)? section = null )
        {
            var enc     = Encoding.UTF8;
            var enc_enc = enc.GetEncoder();
            var buf     = new byte[ 4_096 ];

            using var sr = new StreamReader( filePath );
            var line_num = 0L;
            foreach ( var seg in FileByteLineReader.ReadByLine( filePath, section ) )
            {
                if ( (++line_num % 1_000_000) == 0 )
                {
                    Console.WriteLine( $"ln={line_num / 1_000_000} млн." );
                }

                //var text_1 = enc.GetString( seg.ToArray() ); //Console.WriteLine( text_1 );
                var text_2 = sr.ReadLine();

                var span = text_2.AsSpan();
                var cnt = enc_enc.GetByteCount( span, true );
                if ( buf.Length < cnt ) buf = new byte[ cnt ];
                var real_cnt = enc_enc.GetBytes( span, buf, true );
                Debug.Assert( cnt == real_cnt );

                var byte_span = new ArraySegment< byte >( buf, 0, real_cnt );
                var suc = seg.SequenceEqual( byte_span );
#if DEBUG
                Debug.Assert( suc );
#else
                if ( !suc )
                {
                    var text_1 = enc.GetString( seg.ToArray() );
                    Console.WriteLine( $"'{text_1}' != '{text_2}', line_num={line_num}" );
                }
#endif

                /*
                var suc = (text_1 == text_2);
#if DEBUG
                Debug.Assert( suc );
#else
                if ( !suc )
                {
                    Console.WriteLine( $"'{text_1}' != '{text_2}', line_num={line_num}" );
                }
#endif
                //*/
            }
        }
    }
}