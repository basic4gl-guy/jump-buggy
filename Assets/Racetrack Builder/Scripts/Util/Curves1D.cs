public interface ICurve1D
{
    float GetPt(float t);
}

public class Bezier1D : ICurve1D
{
    private readonly float[] p;

    public Bezier1D(float p0, float p1, float p2, float p3)
    {
        this.p = new[] { p0, p1, p2, p3 };
    }

    public Bezier1D(float[] p)
    {
        this.p = p;
    }

    /// <summary>
    /// Get point at t, where t [0,1]
    /// </summary>
    public float GetPt(float t)
    {
        float mt = 1.0f - t;
        return mt * mt * mt * p[0] + 3.0f * mt * mt * t * p[1] + 3.0f * mt * t * t * p[2] + t * t * t * p[3];
    }
}

public class Linear1D : ICurve1D
{
    private readonly float p0;
    private readonly float p1;

    public Linear1D(float p0, float p1)
    {
        this.p0 = p0;
        this.p1 = p1;
    }

    public float GetPt(float t)
    {
        return (1.0f - t) * p0 + t * p1;
    }
}
