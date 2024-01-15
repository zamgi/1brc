using System.Runtime.CompilerServices;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace _1brc
{
    /// <summary>
    /// 
    /// </summary>
    [SkipLocalsInit]
    public struct SummaryDouble
    {
        private double _Min;
        private double _Max;
        private double _Sum;
        private long   _Count;
#if (DEBUG || CALC_SUM2)
        private long   _Sum2;
#endif
        public double Average => _Sum / _Count;
        public double Min     => _Min;
        public double Max     => _Max;
        public double Sum     => _Sum;
        public long   Count   => _Count;
#if (DEBUG || CALC_SUM2)
        public long   Sum2    => _Sum2;
#endif
        [M(O.AggressiveInlining)] public void Apply( double value, bool existing )
        {
            if ( existing )
            {
                if ( value < _Min ) _Min = value; // _Min = Math.Min( _Min, value );
                if ( _Max < value ) _Max = value; // _Max = Math.Max( _Max, value );
                _Sum += value;
                _Count++;
#if (DEBUG || CALC_SUM2)
                _Sum2 += (long) value;
#endif
            }
            else
            {
                _Min   = value;
                _Max   = value;
                _Sum   = value;
                _Count = 1;
#if (DEBUG || CALC_SUM2)
                _Sum2  = (long) value;
#endif
            }
        }
        [M(O.AggressiveInlining)] public void Apply( double value )
        {
            if ( _Count++ > 0 )
            {
                if ( value < _Min ) _Min = value; // _Min = Math.Min( _Min, value );
                if ( _Max < value ) _Max = value; // _Max = Math.Max( _Max, value );
                _Sum += value;
#if (DEBUG || CALC_SUM2)
                _Sum2 += (long) value;
#endif
            }
            else
            {
                _Min = value;
                _Max = value;
                _Sum = value;
#if (DEBUG || CALC_SUM2)
                _Sum2 = (long) value;
#endif
            }
        }

        [M(O.AggressiveInlining)] public void Merge( in SummaryDouble other )
        {
            if ( other._Min < _Min ) _Min = other._Min;
            if ( _Max < other._Max ) _Max = other._Max;
            _Sum   += other._Sum;
            _Count += other._Count;
#if (DEBUG || CALC_SUM2)
            _Sum2 += other._Sum2;
#endif
        }

        public override string ToString() => $"min={_Min:N1}, avg={Average:N1}, max={_Max:N1}, cnt={_Count}";
    }
}