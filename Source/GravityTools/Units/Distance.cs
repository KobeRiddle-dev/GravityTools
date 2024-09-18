#if USE_LARGE_WORLDS
using Real = System.Double;
using Mathr = FlaxEngine.Mathd;
#else
using Real = System.Single;
using Mathr = FlaxEngine.Mathf;
#endif

namespace GravityTools.Units;

/// <summary>
/// A distance
/// </summary>
public struct Distance
{
    private const Real MetersPerKilometer = 1000;
    private const Real CentimetersPerMeter = 100;

    private readonly Real centimeters;

    /// <summary>
    /// This Distance in kilometers
    /// </summary>
    public readonly Real Kilometers { get => this.Meters / MetersPerKilometer; }

    /// <summary>
    /// This Distance in meters
    /// </summary>
    public readonly Real Meters { get => this.centimeters / CentimetersPerMeter; }

    /// <summary>
    /// This Distance in Centimeters
    /// </summary>
    public readonly Real Centimeters { get => this.centimeters; }

    private Distance(Real centimeters)
    {
        this.centimeters = centimeters;
    }


    /// <param name="meters"></param>
    /// <returns>A new Distance</returns>
    public static Distance FromMeters(Real meters)
    {
        return new Distance(meters * CentimetersPerMeter);
    }

    /// <param name="kilometers"></param>
    /// <returns>A new Distance</returns>
    public static Distance FromKilometers(Real kilometers)
    {
        return FromMeters(kilometers * MetersPerKilometer);
    }

    /// <param name="centimeters"></param>
    /// <returns>A new Distance</returns>
    public static Distance FromCentimeters(Real centimeters)
    {
        return new Distance(centimeters);
    }

    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns>The sum of the two Distances</returns>
    public static Distance operator +(Distance left, Distance right)
    {
        return new Distance(left.centimeters + right.centimeters);
    }

    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns>The difference between the two Distances</returns>
    public static Distance operator -(Distance left, Distance right)
    {
        return new Distance(left.centimeters - right.centimeters);
    }

    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns>The product of the two Distances</returns>
    public static Distance operator *(Distance left, Distance right)
    {
        return new Distance(left.centimeters * right.centimeters);
    }

    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns>The quotient of the two Distances</returns>
    public static Distance operator /(Distance left, Distance right)
    {
        return new Distance(left.centimeters / right.centimeters);
    }


    /// <param name="distance"></param>
    /// <returns>The negation of the Distance</returns>
    public static Distance operator -(Distance distance)
    {
        return new Distance(-distance.centimeters);
    }

    /// <param name="obj"></param>
    /// <returns>True if </returns>
    public override readonly bool Equals(object obj)
    {
        if (obj == null || this.GetType() != obj.GetType())
        {
            return false;
        }

        return this.centimeters.Equals(((Distance)obj).centimeters);
    }

    
    public override readonly int GetHashCode()
    {
        return this.centimeters.GetHashCode();
    }

    public static bool operator ==(Distance left, Distance right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Distance left, Distance right)
    {
        return !left.Equals(right);
    }
}
