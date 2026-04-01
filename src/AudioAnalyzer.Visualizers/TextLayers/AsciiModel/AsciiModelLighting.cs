using System.Numerics;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Resolves AsciiModel directional light from <see cref="AsciiModelSettings"/>.</summary>
public static class AsciiModelLighting
{
    private static readonly Vector3 s_classicDir = Vector3.Normalize(new Vector3(0.35f, 0.55f, 0.78f));

    /// <summary>Returns a unit vector in the same space as rotated mesh normals (world space after model rotation).</summary>
    public static Vector3 GetLightDirection(
        AsciiModelLightingPreset preset,
        double azimuthDegrees,
        double elevationDegrees)
    {
        return preset switch
        {
            AsciiModelLightingPreset.Classic => s_classicDir,
            AsciiModelLightingPreset.Headlight => new Vector3(0f, 0f, 1f),
            AsciiModelLightingPreset.Custom => DirectionFromAzimuthElevation(azimuthDegrees, elevationDegrees),
            _ => s_classicDir
        };
    }

    /// <summary>Lambert term plus ambient floor: <paramref name="ambient"/> + (1 - <paramref name="ambient"/>) × max(<paramref name="diffuse"/>, 0).</summary>
    public static float CombineDiffuseAndAmbient(float diffuse, float ambient)
    {
        float a = Math.Clamp(ambient, 0f, 1f);
        float d = Math.Clamp(diffuse, 0f, 1f);
        return a + (1f - a) * d;
    }

    /// <summary>Azimuth in XY from +X toward +Y; elevation from XY plane toward +Z.</summary>
    private static Vector3 DirectionFromAzimuthElevation(double azimuthDegrees, double elevationDegrees)
    {
        double az = azimuthDegrees * (Math.PI / 180.0);
        double el = elevationDegrees * (Math.PI / 180.0);
        double ce = Math.Cos(el);
        float x = (float)(ce * Math.Cos(az));
        float y = (float)(ce * Math.Sin(az));
        float z = (float)Math.Sin(el);
        var v = new Vector3(x, y, z);
        if (v.LengthSquared() > 1e-20f)
        {
            return Vector3.Normalize(v);
        }

        return new Vector3(0f, 0f, 1f);
    }
}
