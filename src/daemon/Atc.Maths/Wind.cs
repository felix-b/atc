namespace Atc.Maths;

public readonly struct Wind
{
    public Wind(SingleOrRange<Bearing>? direction, SingleOrRange<Speed>? speed, Speed? gust)
    {
        Direction = direction;
        Speed = speed;
        Gust = gust;
    }

    public SingleOrRange<Bearing>? Direction { get; init; } // null means variable
    public SingleOrRange<Speed>? Speed { get; init; }       // null means calm
    public Speed? Gust { get; init; }                  // null means no gusts
}
