namespace Atc.Data.Primitives
{
    public readonly struct Frequency
    {
        private readonly int _value; 
        private readonly FrequencyUnit _unit;
        
        public Frequency(int value, FrequencyUnit unit) 
        {
            _value = value;
            _unit = unit;
        }

        public int Value => _value;
        public FrequencyUnit Unit => _unit;

        public static Frequency FromKhz(int value)
        {
            return new Frequency(value, FrequencyUnit.Khz);
        }
    }

    public enum FrequencyUnit
    {
        Hz,
        Khz,
        Mhz
    }
}
