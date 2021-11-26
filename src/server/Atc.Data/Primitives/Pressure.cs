using System;
using System.Runtime.InteropServices.ComTypes;

namespace Atc.Data.Primitives
{
    public readonly struct Pressure
    {
        private readonly float _value; 
        private readonly PressureUnit _unit;
        
        public Pressure(float value, PressureUnit unit) 
        {
            _value = value;
            _unit = unit;
        }

        public float Value => _value;
        public PressureUnit Unit => _unit;
        public float Hpa => _unit == PressureUnit.Hpa ? _value : throw new NotImplementedException();
        public float InHg => _unit == PressureUnit.InHg ? _value : throw new NotImplementedException();

        public static readonly Pressure Qne = new Pressure(29.92f, PressureUnit.InHg);
        
        public static Pressure FromHpa(float value)
        {
            return new Pressure(value, PressureUnit.Hpa);
        }
    }
}
