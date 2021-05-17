namespace Atc.Data.Primitives
{
    public struct Speed
    {
        private readonly float _value; 
        private readonly SpeedUnit _unit;
        
        public Speed(float value, SpeedUnit unit) 
        {
            _value = value;
            _unit = unit;
        }

        public float Value => _value;
        public SpeedUnit Unit => _unit;
    }

    public enum SpeedUnit
    {   
        Kt,
        Fpm,
        Mph,
        Kmh
    }
}
