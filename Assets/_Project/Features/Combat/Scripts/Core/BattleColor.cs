using UnityEngine;

/// <summary>
/// Colores que pueden participar en el sistema de combate.
/// El orden coincide con las filas y columnas de la tabla de efectividad.
/// </summary>
public enum BattleColor
{
    Azul = 0,
    Rojo = 1,
    Amarillo = 2,
    Purpura = 3,
    Verde = 4,
    Naranja = 5,
    Azarcon = 6,
    Ambar = 7,
    Lima = 8,
    Viridian = 9,
    Indigo = 10,
    Carmesi = 11
}

public static class BattleColorExtensions
{
    public static bool IsPrimary(this BattleColor color)
    {
        return color == BattleColor.Rojo
            || color == BattleColor.Azul
            || color == BattleColor.Amarillo;
    }

    public static string ToDisplayName(this BattleColor color)
    {
        switch (color)
        {
            case BattleColor.Purpura:
                return "Púrpura";
            case BattleColor.Azarcon:
                return "Azarcón";
            case BattleColor.Ambar:
                return "Ámbar";
            case BattleColor.Viridian:
                return "Viridián";
            case BattleColor.Indigo:
                return "Índigo";
            case BattleColor.Carmesi:
                return "Carmesí";
            default:
                return color.ToString();
        }
    }

    /// <summary>
    /// Tintes de demostración. Usa un sprite blanco para que el color se vea
    /// correctamente en un SpriteRenderer.
    /// </summary>
    public static Color ToUnityColor(this BattleColor color)
    {
        switch (color)
        {
            case BattleColor.Azul:
                return new Color(0.12f, 0.38f, 0.95f);
            case BattleColor.Rojo:
                return new Color(0.93f, 0.10f, 0.14f);
            case BattleColor.Amarillo:
                return new Color(1.00f, 0.82f, 0.05f);
            case BattleColor.Purpura:
                return new Color(0.55f, 0.20f, 0.72f);
            case BattleColor.Verde:
                return new Color(0.12f, 0.62f, 0.28f);
            case BattleColor.Naranja:
                return new Color(1.00f, 0.40f, 0.04f);
            case BattleColor.Azarcon:
                return new Color(0.95f, 0.22f, 0.06f);
            case BattleColor.Ambar:
                return new Color(1.00f, 0.62f, 0.03f);
            case BattleColor.Lima:
                return new Color(0.63f, 0.88f, 0.10f);
            case BattleColor.Viridian:
                return new Color(0.00f, 0.52f, 0.40f);
            case BattleColor.Indigo:
                return new Color(0.27f, 0.20f, 0.65f);
            case BattleColor.Carmesi:
                return new Color(0.78f, 0.04f, 0.24f);
            default:
                return Color.white;
        }
    }
}
