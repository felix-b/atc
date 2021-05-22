using System.Collections.Generic;
using System.Net;

namespace Atc.Data.Primitives
{
    public readonly struct Weight
    {
        private readonly float _value; 
        private readonly WeightUnit _unit;
        
        public Weight(float value, WeightUnit unit) 
        {
            _value = value;
            _unit = unit;
        }

        public float Value => _value;
        public WeightUnit Unit => _unit;

        public float Kg => _unit == WeightUnit.Kg 
            ? _value 
            : _value * _unitConversionMatrix[_unit][WeightUnit.Kg]; 
        
        public float Lbs => _unit == WeightUnit.Lbs 
            ? _value
            : _value * _unitConversionMatrix[_unit][WeightUnit.Lbs]; 

        private static readonly Dictionary<WeightUnit, Dictionary<WeightUnit, float>> _unitConversionMatrix = new() {
            [WeightUnit.Kg] = new() {
                [WeightUnit.Kg] = 1.0f,
                [WeightUnit.Lbs] = 2.20462f
            },
            [WeightUnit.Lbs] = new() {
                [WeightUnit.Kg] = 0.453592f,
                [WeightUnit.Lbs] = 1.0f
            },
        };
    }
}
