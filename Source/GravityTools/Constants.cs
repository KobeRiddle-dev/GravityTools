#if USE_LARGE_WORLDS
using Real = System.Double;
using Mathr = FlaxEngine.Mathd;
#else
using Real = System.float;
using Mathr = FlaxEngine.Mathf;
#endif

namespace GravityTools;

/// <summary>
/// Constants for GravityTools
/// </summary>
public class Constants
{
    /// <summary>
    /// The Newtonian Constant of Gravitation G, in units cm^3/(kg*s^2)
    /// </summary>
    public const Real GRAVITATIONAL_CONSTANT_CENTIMETERS = 6.674E-5;

    /// <summary>
    /// The Newtonian Constant of Gravitation G, in standard SI units m^3/(kg*s^2)
    /// </summary>
    public const Real GRAVITATIONAL_CONSTANT = 6.67408E-11;
}