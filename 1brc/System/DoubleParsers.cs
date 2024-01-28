using System.Collections.Generic;
using System.Linq;
using System.Text;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace System
{
    /// <summary>
    /// 
    /// </summary>
    internal static class DoubleParser_FSM /*DFA*/
    {
        /// <summary>
        /// 
        /// </summary>
        [Flags] private enum CharType : int
        {
            __UNDEFINED__ = 0,

            Minus         = 1,
            Number        = 1 << 1,
            Dot           = 1 << 2,

            //MAX = Dot,
        }

        /// <summary>
        /// 
        /// </summary>
        [Flags] private enum StateType : int
        {
            __UNDEFINED__ = 0,

            Minus         = 1,
            Number        = 1 << 1,
            Dot           = 1 << 2,

            Number_After_Dot = 1 << 3,

            Fail = 1 << 4,

            //MAX = Fail,
        }

        private static StateType[/*current char type*/][/*current state*/] _TransMap; /*allowed state*/
        //private static Dictionary< CharType, Dictionary< StateType, StateType > > _TransDict;
        static DoubleParser_FSM()
        {
            _TransMap = new StateType[ GetMaxInt32Value< CharType >() + 1 ][];
            for ( int size = GetMaxInt32Value< StateType >() + 1, i = 0; i < _TransMap.Length; i++ )
            {
                _TransMap[ i ] = new StateType[ size ];
                Array.Fill( _TransMap[ i ], StateType.Fail );
            }

            _TransMap[ (int) CharType.Minus ][ (int) StateType.__UNDEFINED__ ] = StateType.Number | StateType.Dot;

            _TransMap[ (int) CharType.Number ][ (int) StateType.__UNDEFINED__            ] = StateType.Number | StateType.Dot;
            _TransMap[ (int) CharType.Number ][ (int) StateType.Number                   ] = StateType.Number | StateType.Dot;
            _TransMap[ (int) CharType.Number ][ (int) (StateType.Number | StateType.Dot) ] = StateType.Number | StateType.Dot;
            _TransMap[ (int) CharType.Number ][ (int) StateType.Number_After_Dot         ] = StateType.Number_After_Dot;

            _TransMap[ (int) CharType.Dot ][ (int) StateType.__UNDEFINED__            ] = StateType.Number_After_Dot;
            _TransMap[ (int) CharType.Dot ][ (int) (StateType.Number | StateType.Dot) ] = StateType.Number_After_Dot;

            //--------------------------------------------------------------//
            /*
            _TransDict = new Dictionary< CharType, Dictionary< StateType, StateType > >
            {
                { CharType.Minus , new Dictionary< StateType, StateType > 
                                   { 
                                        { StateType.__UNDEFINED__, StateType.Number | StateType.Dot } 
                                   } 
                },

                { CharType.Number, new Dictionary< StateType, StateType > 
                                   { 
                                        { StateType.__UNDEFINED__         , StateType.Number | StateType.Dot },
                                        { StateType.Number                , StateType.Number | StateType.Dot },
                                        { StateType.Number | StateType.Dot, StateType.Number | StateType.Dot },
                                        { StateType.Number_After_Dot      , StateType.Number_After_Dot }
                                   } 
                },

                { CharType.Dot,    new Dictionary< StateType, StateType > 
                                   { 
                                        { StateType.__UNDEFINED__         , StateType.Number_After_Dot },
                                        { StateType.Number | StateType.Dot, StateType.Number_After_Dot },
                                   } 
                },
            };
            //*/
        }

        [M(O.AggressiveInlining)] private static int GetMaxInt32Value< T >() where T : struct, Enum => Enum.GetValues< T >().Select( t => Convert.ToInt32( t ) ).Max();
        //[M(O.AggressiveInlining)] private static int i( this CharType e ) => (int) e;
        //[M(O.AggressiveInlining)] private static int i( this StateType e ) => (int) e;
        //public static implicit operator int( CharType e ) => (int) e;

        [M(O.AggressiveInlining)] private static CharType GetCharType( this char c ) => c switch
        {
            '-' => CharType.Minus,
            '0' => CharType.Number,
            '1' => CharType.Number,
            '2' => CharType.Number,
            '3' => CharType.Number,
            '4' => CharType.Number,
            '5' => CharType.Number,
            '6' => CharType.Number,
            '7' => CharType.Number,
            '8' => CharType.Number,
            '9' => CharType.Number,
            '.' => CharType.Dot,
             _  => CharType.__UNDEFINED__,
        };

        [M(O.AggressiveInlining)] public static bool TryParse( string text, out double d, out int finitaPos )
        {
            var sign       = 1;
            var integer    = 0;
            var fractional = 0;
            var fractional_order = 0;

            var pos = 0;
            var ct  = CharType.__UNDEFINED__;
            for ( var next_state = StateType.__UNDEFINED__; pos < text.Length; pos++ )
            {
                var c  = text[ pos ];
                    ct = c.GetCharType();

                var states = _TransMap[ (int) ct ];
                var __next_state__ = states[ (int) next_state ];
                if ( __next_state__ == StateType.Fail )
                {
                    d = default;
                    finitaPos = pos;
                    return (false);
                }
                next_state = __next_state__;

                switch ( ct )
                {
                    case CharType.Number:
                        if ( 0 < fractional_order )
                        {
                            fractional = fractional * 10 + (c - '0');
                            fractional_order *= 10;
                        }
                        else
                        {
                            integer = integer * 10 + (c - '0');
                        }
                        break;
                    case CharType.Minus: sign = -1; break;
                    case CharType.Dot  : fractional_order = 1; break;
                }
            }

            finitaPos = pos;

            switch ( ct )
            {
                case CharType.Number:
                    d = sign * integer + ((0 < fractional_order) ? sign * fractional / (1.0 * fractional_order) : 0);
                    return (true);

                default:
                    d = default;
                    return (false);
            }
        }
        [M(O.AggressiveInlining)] public static bool TryParse( IEnumerable< byte > text, out double d, out int finitaPos )
        {
            var sign       = 1;
            var integer    = 0;
            var fractional = 0;
            var fractional_order = 0;

            var pos = 0;
            var ct  = CharType.__UNDEFINED__;
            var next_state = StateType.__UNDEFINED__;
            foreach ( var b in text )
            {
                var c  = (char) b;
                    ct = c.GetCharType();

                var states = _TransMap[ (int) ct ];                
                var __next_state__ = states[ (int) next_state ];
                if ( __next_state__ == StateType.Fail )
                {
                    d = default;
                    finitaPos = pos;
                    return (false);
                }
                next_state = __next_state__;

                switch ( ct )
                {
                    case CharType.Number:
                        if ( 0 < fractional_order )
                        {
                            fractional = fractional * 10 + (c - '0');
                            fractional_order *= 10;
                        }
                        else
                        {
                            integer = integer * 10 + (c - '0');
                        }
                        break;
                    case CharType.Minus: sign = -1; break;
                    case CharType.Dot  : fractional_order = 1; break;
                }

                pos++;
            }

            finitaPos = pos;

            switch ( ct )
            {
                case CharType.Number:
                    d = sign * integer + ((0 < fractional_order) ? sign * fractional / (1.0 * fractional_order) : 0);
                    return (true);

                default:
                    d = default;
                    return (false);
            }
        }

        [M(O.AggressiveInlining)] public static bool TryParse( in ListSegment< byte > seg, out double d, out int finitaPos )
        {
            var sign    = 1;
            var integer = 0L;
            var fract_order = 0;

            var pos = 0;
            var ct  = CharType.__UNDEFINED__;
            
            var span  = seg.AsSpan();
            var count = span.Length;
            for ( var next_state = StateType.__UNDEFINED__; pos < count; pos++ )
            {
                var c  = (char) span[ pos ];
                    ct = c.GetCharType();

                var states = _TransMap[ (int) ct ];
                var __next_state__ = states[ (int) next_state ];
                if ( __next_state__ == StateType.Fail )
                {
                    d = default;
                    finitaPos = pos;
                    return (false);
                }
                next_state = __next_state__;

                switch ( ct )
                {
                    case CharType.Number:
                        integer = integer * 10 + (c - '0');
                        fract_order *= 10;
                        break;
                    case CharType.Minus: sign = -1; break;
                    case CharType.Dot  : fract_order = 1; break;
                }
            }

            finitaPos = pos;

            switch ( ct )
            {
                case CharType.Number:
                    d = sign * integer / ((0 < fract_order) ? (double) fract_order : 1);
                    return (true);

                default:
                    d = default;
                    return (false);
            }
        }
        [M(O.AggressiveInlining)] public static bool TryParse( in Span< byte > span, out double d, out int finitaPos )
        {
            var sign    = 1;
            var integer = 0L;
            var fract_order = 0;

            var pos = 0;
            var ct  = CharType.__UNDEFINED__;
            
            var count = span.Length;
            for ( var next_state = StateType.__UNDEFINED__; pos < count; pos++ )
            {
                var c  = (char) span[ pos ];
                    ct = c.GetCharType();

                var states = _TransMap[ (int) ct ];
                var __next_state__ = states[ (int) next_state ];
                if ( __next_state__ == StateType.Fail )
                {
                    d = default;
                    finitaPos = pos;
                    return (false);
                }
                next_state = __next_state__;

                switch ( ct )
                {
                    case CharType.Number:
                        integer = integer * 10 + (c - '0');
                        fract_order *= 10;
                        break;
                    case CharType.Minus: sign = -1; break;
                    case CharType.Dot  : fract_order = 1; break;
                }
            }

            finitaPos = pos;

            switch ( ct )
            {
                case CharType.Number:
                    d = sign * integer / ((0 < fract_order) ? (double) fract_order : 1);
                    return (true);

                default:
                    d = default;
                    return (false);
            }
        }
        
        [M(O.AggressiveInlining)] public static bool TryParse_ByNewLine( in Span< byte > span, out double d, out int finitaPos )
        {
            var sign    = 1;
            var integer = 0L;
            var fract_order = 0;

            var pos = 0;
            var ct  = CharType.__UNDEFINED__;
            
            var end = span.Length  -1;
            var prev_ct = CharType.__UNDEFINED__;
            for ( var next_state = StateType.__UNDEFINED__; pos <= end; pos++ )
            {
                var ch = (char) span[ pos ];
                    ct = ch.GetCharType();

                var states = _TransMap[ (int) ct ];                
                var __next_state__ = states[ (int) next_state ];
                if ( __next_state__ == StateType.Fail )
                {
                    switch ( ch )
                    {
                        case '\r': 
                            if ( (pos != end) && (span[ pos + 1 ] == '\n') )
                            {
                                pos++;
                            }
                            ct = prev_ct;
                            goto EXIT;

                        case '\n':
                            ct = prev_ct;
                            goto EXIT;

                        default:
                            d = default;
                            finitaPos = pos;
                            return (false);
                    }
                }
                next_state = __next_state__;

                switch ( ct )
                {
                    case CharType.Number:
                        integer = integer * 10 + (ch - '0');
                        fract_order *= 10;
                        break;
                    case CharType.Minus: sign = -1; break;
                    case CharType.Dot  : fract_order = 1; break;
                }

                prev_ct = ct;
            }
        EXIT:
            finitaPos = pos;

            switch ( ct )
            {
                case CharType.Number:
                    d = sign * integer / ((0 < fract_order) ? (double) fract_order : 1);
                    return (true);

                default:
                    d = default;
                    return (false);
            }
        }
        [M(O.AggressiveInlining)] public static bool TryParse2Decimal_ByNewLine( in Span< byte > span, out decimal d, out int finitaPos )
        {
            var sign    = 1;
            var integer = 0L;
            var fract_order = 0;

            var pos = 0;
            var ct  = CharType.__UNDEFINED__;
            
            var end = span.Length  -1;
            var prev_ct = CharType.__UNDEFINED__;
            for ( var next_state = StateType.__UNDEFINED__; pos <= end; pos++ )
            {
                var ch = (char) span[ pos ];
                    ct = ch.GetCharType();

                var states = _TransMap[ (int) ct ];                
                var __next_state__ = states[ (int) next_state ];
                if ( __next_state__ == StateType.Fail )
                {
                    switch ( ch )
                    {
                        case '\r': 
                            if ( (pos != end) && (span[ pos + 1 ] == '\n') )
                            {
                                pos++;
                            }
                            ct = prev_ct;
                            goto EXIT;

                        case '\n':
                            ct = prev_ct;
                            goto EXIT;

                        default:
                            d = default;
                            finitaPos = pos;
                            return (false);
                    }                    
                }
                next_state = __next_state__;

                switch ( ct )
                {
                    case CharType.Number:
                        integer = integer * 10 + (ch - '0');
                        fract_order *= 10;
                        break;
                    case CharType.Minus: sign = -1; break;
                    case CharType.Dot  : fract_order = 1; break;
                }

                prev_ct = ct;
            }
        EXIT:
            finitaPos = pos;

            switch ( ct )
            {
                case CharType.Number:
                    d = sign * integer;
                    if ( 0 < fract_order ) d /= fract_order;
                    return (true);

                default:
                    d = default;
                    return (false);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class DoubleParser
    {
        public static bool TryParse( string s, out double d ) => TryParse( Encoding.UTF8.GetBytes( s ), out d );
        public static bool TryParse( byte[] chars, out double d ) => TryParse( chars, 0, chars.Length, out d );
        public static bool TryParse( byte[] chars, int offset, int count, out double d )
        {
            d = 0;
            int offsetMax = offset + count;
            var negative = false;
            if ( (offset < offsetMax) && (chars[ offset ] == '-') )
            {
                negative = true;
                offset++;
                count--;
            }
            if ( count < 1 || 10 < count )
            {
                return (false);
            }

            int value = 0;
            int ch;
            while ( offset < offsetMax )
            {
                ch = (chars[ offset ] - '0');
                if ( ch == ('.' - '0') )
                {
                    offset++;
                    int pow10 = 1;
                    while ( offset < offsetMax )
                    {
                        ch = chars[ offset ] - '0';
                        if ( ((uint) ch) >= 10 )
                        {
                            return (false);
                        }
                        pow10 *= 10;
                        value = value * 10 + ch;
                        offset++;
                    }
                    d = (negative ? -1 : 1) * (double) value / pow10;
                    return (true);
                }
                else if ( ((uint) ch) >= 10 )
                {
                    return (false);
                }
                value = value * 10 + ch;
                offset++;
            }
            // Ten digits w/out a decimal point might have overflowed the int
            if ( count == 10 )
            {
                return (false);
            }
            d = (negative ? -1 : 1) * value;
            return (true);
        }

        public static bool TryParse( IList< byte > chars, out double d ) => TryParse( chars, 0, chars.Count, out d );
        public static bool TryParse( IList< byte > chars, int offset, int count, out double d )
        {
            d = 0;
            int offsetMax = offset + count;
            var negative = false;
            if ( (offset < offsetMax) && (chars[ offset ] == '-') )
            {
                negative = true;
                offset++;
                count--;
            }
            if ( count < 1 || 10 < count )
            {
                return (false);
            }

            int value = 0;
            int ch;
            while ( offset < offsetMax )
            {
                ch = (chars[ offset ] - '0');
                if ( ch == ('.' - '0') )
                {
                    offset++;
                    int pow10 = 1;
                    while ( offset < offsetMax )
                    {
                        ch = chars[ offset ] - '0';
                        if ( ((uint) ch) >= 10 )
                        {
                            return (false);
                        }
                        pow10 *= 10;
                        value = value * 10 + ch;
                        offset++;
                    }
                    d = (negative ? -1 : 1) * (double) value / pow10;
                    return (true);
                }
                else if ( ((uint) ch) >= 10 )
                {
                    return (false);
                }
                value = value * 10 + ch;
                offset++;
            }
            // Ten digits w/out a decimal point might have overflowed the int
            if ( count == 10 )
            {
                return (false);
            }
            d = (negative ? -1 : 1) * value;
            return (true);
        }

        public static bool TryParse( in Span< byte > span, out double d )
        {
            d = 0;
            var count    = span.Length;
            var negative = false;
            var pos      = 0;
            if ( (0 < count) && (span[ pos ] == '-') )
            {
                negative = true;
                pos++;
                count--;
            }
            if ( count < 1 || 10 < count )
            {
                return (false);
            }

            int value = 0;
            int ch;
            while ( pos < count )
            {
                ch = (span[ pos ] - '0');
                if ( ch == ('.' - '0') )
                {
                    pos++;
                    int pow10 = 1;
                    while ( pos < count )
                    {
                        ch = span[ pos ] - '0';
                        if ( ((uint) ch) >= 10 )
                        {
                            return (false);
                        }
                        pow10 *= 10;
                        value = value * 10 + ch;
                        pos++;
                    }
                    d = (negative ? -1 : 1) * (double) value / pow10;
                    return (true);
                }
                else if ( ((uint) ch) >= 10 )
                {
                    return (false);
                }
                value = value * 10 + ch;
                pos++;
            }
            // Ten digits w/out a decimal point might have overflowed the int
            if ( count == 10 )
            {
                return (false);
            }
            d = (negative ? -1 : 1) * value;
            return (true);
        }
        public static bool TryParse_ByNewLine( in Span< byte > span, out double d, out int finitaPos )
        {            
            var count    = span.Length;
            var negative = false;
            var pos      = 0;
            if ( (0 < count) && (span[ pos ] == '-') )
            {
                negative = true;
                pos++;
            }

            int value = 0;
            while ( pos < count )
            {
                int ch = (span[ pos ] - '0');
                if ( ch == ('.' - '0') )
                {
                    pos++;
                    int pow10 = 1;
                    while ( pos < count )
                    {
                        ch = span[ pos ] - '0';
                        if ( ((uint) ch) >= 10 )
                        {
                            switch ( ch )
                            {
                                case ('\r' - '0'):
                                    if ( (pos < (count - 1)) && (span[ pos + 1 ] == '\n') ) pos++;
                                    goto EXIT_1;

                                case ('\n' - '0'):
                                    goto EXIT_1;

                                default:
                                    d = default;
                                    finitaPos = pos;
                                    return (false);
                            }
                        }
                        pow10 *= 10;
                        value  = value * 10 + ch;
                        pos++;
                    }
               EXIT_1:
                    // Ten digits w/out a decimal point might have overflowed the int
                    //if ( 10 + (negative ? 1 : 0) <= pos )
                    //{
                    //    d = default;
                    //    finitaPos = pos;
                    //    return (false);
                    //}
                    d = (negative ? -1 : 1) * (double) value / pow10;
                    finitaPos = pos;
                    return (true);
                }
                else if ( ((uint) ch) >= 10 )
                {
                    switch ( ch )
                    {
                        case ('\r' - '0'):
                            if ( (pos < (count - 1)) && (span[ pos + 1 ] == '\n') ) pos++;
                            goto EXIT_2;

                        case ('\n' - '0'):
                            goto EXIT_2;

                        default:
                            d = default;
                            finitaPos = pos;
                            return (false);
                    }
                }
                value = value * 10 + ch;
                pos++;
            }
      EXIT_2:
            // Ten digits w/out a decimal point might have overflowed the int
            //if ( 10 + (negative ? 1 : 0) <= pos )
            //{
            //    d = default;
            //    finitaPos = pos;
            //    return (false);
            //}
            d = (negative ? -1 : 1) * value;
            finitaPos = pos;
            return (true);
        }
    }
}