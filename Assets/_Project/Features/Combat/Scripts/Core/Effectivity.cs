using System;
using UnityEngine;

/// <summary>
/// Tabla de efectividad cromática. Las filas representan al atacante y las
/// columnas al defensor.
/// </summary>
public static class Effectivity
{
    private const int ColorCount = 12;

    private static readonly float[,] Multipliers =
    {
        // Def.: Azul Rojo Amar. Púrp. Verde Naran. Azarc. Ámbar Lima Virid. Índigo Carmesí
        { 1f, 2f, .5f, 2f, .5f, 1f, .5f, 1f, 2f, 1f, 1f, 2f }, // Azul
        { .5f, 1f, 2f, 1f, 2f, 2f, 1f, 2f, .5f, 2f, .5f, 1f }, // Rojo
        { 2f, .5f, 1f, .5f, 2f, 1f, 2f, 1f, 1f, 2f, 2f, .5f }, // Amarillo
        { .5f, 1f, 2f, 1f, .5f, 2f, .5f, 1f, 1f, 1f, 2f, 1f }, // Púrpura
        { 2f, .5f, 1f, 2f, 1f, .5f, 1f, 2f, 1f, 1f, 1f, .5f }, // Verde
        { 1f, 2f, .5f, .5f, 2f, 1f, 2f, 1f, 2f, .5f, .5f, 1f }, // Naranja
        { .5f, 1f, 2f, 1f, 2f, 1f, 1f, 2f, .5f, 2f, .5f, 2f }, // Azarcón
        { 2f, .5f, 1f, 1f, 1f, 2f, .5f, 1f, 2f, 1f, 1f, .5f }, // Ámbar
        { 1f, .5f, 1f, 1f, 1f, 2f, .5f, 2f, 1f, 2f, .5f, 2f }, // Lima
        { 1f, 1f, .5f, 2f, 1f, .5f, 2f, 1f, 1f, 1f, 2f, 2f }, // Viridián
        { 1f, .5f, 2f, 1f, 2f, 1f, .5f, 2f, .5f, 1f, 1f, 1f }, // Índigo
        { .5f, 2f, .5f, 1f, 1f, 2f, 2f, .5f, .5f, 1f, 1f, 1f } // Carmesí
    };

    public static float GetMultiplier(BattleColor attacker, BattleColor defender)
    {
        int attackerIndex = (int)attacker;
        int defenderIndex = (int)defender;

        if (attackerIndex < 0 || attackerIndex >= ColorCount)
        {
            throw new ArgumentOutOfRangeException("attacker", attacker, "Color atacante inválido.");
        }

        if (defenderIndex < 0 || defenderIndex >= ColorCount)
        {
            throw new ArgumentOutOfRangeException("defender", defender, "Color defensor inválido.");
        }

        return Multipliers[attackerIndex, defenderIndex];
    }

    public static int CalculateDamage(
        float basePower,
        BattleColor attacker,
        BattleColor defender)
    {
        if (basePower < 0f)
        {
            throw new ArgumentOutOfRangeException("basePower", "El poder base no puede ser negativo.");
        }

        if (Mathf.Approximately(basePower, 0f))
        {
            return 0;
        }

        float multiplier = GetMultiplier(attacker, defender);
        return Mathf.Max(1, Mathf.RoundToInt(basePower * multiplier));
    }
}
