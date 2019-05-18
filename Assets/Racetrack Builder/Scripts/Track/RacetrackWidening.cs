using System;

[Serializable]
public struct RacetrackWidening
{    
    public float Left;
    public float Right;

    public RacetrackWidening(float left, float right)
    {
        Left = left;
        Right = right;
    }

    public static RacetrackWidening operator+(RacetrackWidening a, RacetrackWidening b)
    {
        return new RacetrackWidening(a.Left + b.Left, a.Right + b.Right);
    }

    public static RacetrackWidening operator -(RacetrackWidening a, RacetrackWidening b)
    {
        return new RacetrackWidening(a.Left - b.Left, a.Right - b.Right);
    }

    public static RacetrackWidening operator *(RacetrackWidening a, float f)
    {
        return new RacetrackWidening(a.Left * f, a.Right * f);
    }

    public static RacetrackWidening operator /(RacetrackWidening a, float f)
    {
        return new RacetrackWidening(a.Left / f, a.Right / f);
    }

    public static readonly RacetrackWidening zero = new RacetrackWidening(0.0f, 0.0f);
}
