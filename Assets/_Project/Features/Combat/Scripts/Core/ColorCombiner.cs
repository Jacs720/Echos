/// <summary>
/// Estado de una mezcla mientras el jugador selecciona colores.
/// </summary>
public enum ColorMixState
{
    Incomplete,
    Valid,
    Invalid
}

/// <summary>
/// Resultado inmutable de evaluar una mezcla.
/// </summary>
public struct ColorMixResult
{
    public ColorMixState State { get; private set; }
    public BattleColor Color { get; private set; }
    public bool HasColor { get { return State == ColorMixState.Valid; } }

    private ColorMixResult(ColorMixState state, BattleColor color)
    {
        State = state;
        Color = color;
    }

    public static ColorMixResult Valid(BattleColor color)
    {
        return new ColorMixResult(ColorMixState.Valid, color);
    }

    public static ColorMixResult Incomplete()
    {
        return new ColorMixResult(ColorMixState.Incomplete, default(BattleColor));
    }

    public static ColorMixResult Invalid()
    {
        return new ColorMixResult(ColorMixState.Invalid, default(BattleColor));
    }
}

/// <summary>
/// Reglas deterministas de combinación. No depende de GameObjects ni de input,
/// por lo que puede probarse de forma aislada.
/// </summary>
public static class ColorCombiner
{
    public const int MaxIngredients = 3;

    public static ColorMixResult Evaluate(BattleColor first)
    {
        if (!first.IsPrimary())
        {
            return ColorMixResult.Invalid();
        }

        return ColorMixResult.Valid(first);
    }

    public static ColorMixResult Evaluate(BattleColor first, BattleColor second)
    {
        if (!first.IsPrimary() || !second.IsPrimary())
        {
            return ColorMixResult.Invalid();
        }

        if (first == second)
        {
            // Dos colores iguales son un intermedio válido para una mezcla
            // terciaria, pero todavía no producen un ataque.
            return ColorMixResult.Incomplete();
        }

        if (ContainsPair(first, second, BattleColor.Rojo, BattleColor.Azul))
        {
            return ColorMixResult.Valid(BattleColor.Purpura);
        }

        if (ContainsPair(first, second, BattleColor.Rojo, BattleColor.Amarillo))
        {
            return ColorMixResult.Valid(BattleColor.Naranja);
        }

        if (ContainsPair(first, second, BattleColor.Azul, BattleColor.Amarillo))
        {
            return ColorMixResult.Valid(BattleColor.Verde);
        }

        return ColorMixResult.Invalid();
    }

    public static ColorMixResult Evaluate(
        BattleColor first,
        BattleColor second,
        BattleColor third)
    {
        if (!first.IsPrimary() || !second.IsPrimary() || !third.IsPrimary())
        {
            return ColorMixResult.Invalid();
        }

        int red = Count(BattleColor.Rojo, first, second, third);
        int blue = Count(BattleColor.Azul, first, second, third);
        int yellow = Count(BattleColor.Amarillo, first, second, third);

        if (red == 2 && yellow == 1)
        {
            return ColorMixResult.Valid(BattleColor.Azarcon);
        }

        if (red == 1 && yellow == 2)
        {
            return ColorMixResult.Valid(BattleColor.Ambar);
        }

        if (blue == 1 && yellow == 2)
        {
            return ColorMixResult.Valid(BattleColor.Lima);
        }

        if (blue == 2 && yellow == 1)
        {
            return ColorMixResult.Valid(BattleColor.Viridian);
        }

        if (red == 1 && blue == 2)
        {
            return ColorMixResult.Valid(BattleColor.Indigo);
        }

        if (red == 2 && blue == 1)
        {
            return ColorMixResult.Valid(BattleColor.Carmesi);
        }

        // Tres iguales o un color de cada tipo no existen en esta rueda.
        return ColorMixResult.Invalid();
    }

    private static bool ContainsPair(
        BattleColor first,
        BattleColor second,
        BattleColor expectedA,
        BattleColor expectedB)
    {
        return (first == expectedA && second == expectedB)
            || (first == expectedB && second == expectedA);
    }

    private static int Count(
        BattleColor expected,
        BattleColor first,
        BattleColor second,
        BattleColor third)
    {
        int result = 0;

        if (first == expected)
        {
            result++;
        }

        if (second == expected)
        {
            result++;
        }

        if (third == expected)
        {
            result++;
        }

        return result;
    }
}
