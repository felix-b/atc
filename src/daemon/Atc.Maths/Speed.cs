namespace Atc.Maths;

public struct Speed
{
    private readonly float _value; 
    private readonly SpeedUnit _unit;
        
    public Speed(float value, SpeedUnit unit) 
    {
        _value = value;
        _unit = unit;
    }

    public float GetValueInUnit(SpeedUnit unit)
    {
        return _unit == unit
            ? _value
            : throw new NotImplementedException(); //_value * _unitConversionMatrix[_unit][unit];
    }

    public float Value => _value;
    public SpeedUnit Unit => _unit;

    public float Knots => GetValueInUnit(SpeedUnit.Kt); 
    public float KilometersPerHour => GetValueInUnit(SpeedUnit.Kmh); 
    public float MilePerHour => GetValueInUnit(SpeedUnit.Mph); 
    public float FeerPerMinute => GetValueInUnit(SpeedUnit.Fpm); 

    public static Speed FromKnots(float value)
    {
        return new Speed(value, SpeedUnit.Kt);
    }

    public static Speed FromFpm(float value)
    {
        return new Speed(value, SpeedUnit.Fpm);
    }
}
