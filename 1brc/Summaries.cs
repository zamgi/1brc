using System;
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

        //public SummaryDouble( double d ) => Init( d );

        public double Average => _Sum / _Count;
        public double Min     => _Min;
        public double Max     => _Max;
        public double Sum     => _Sum;
        public long   Count   => _Count;

        [M(O.AggressiveInlining)] public void Apply( double value, bool existing )
        {
            if ( existing )
                Apply( value );
            else
                Init( value );
        }
        [M(O.AggressiveInlining)] public void Init( double value )
        {
            _Min = value;
            _Max = value;
            _Sum = value;
            _Count = 1;
        }

        [M(O.AggressiveInlining)] public void Apply( double value )
        {
            _Min = Math.Min( _Min, value );
            _Max = Math.Max( _Max, value );
            _Sum += value;
            _Count++;
        }

        [M(O.AggressiveInlining)] public void Merge( in SummaryDouble other )
        {
            if ( other._Min < _Min )
                _Min = other._Min;
            if ( other._Max > _Max )
                _Max = other._Max;
            _Sum   += other._Sum;
            _Count += other._Count;
        }

        public override string ToString() => $"min={_Min:N1}, avg={Average:N1}, max={_Max:N1}, cnt={_Count}";
    }
}