using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleUnit : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private BattleColor color = BattleColor.Rojo;
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(1)] private int attackPower = 20;

    [Header("Optional presentation")]
    [Tooltip("Objeto hijo que se enciende cuando la unidad participa en la mezcla.")]
    [SerializeField] private GameObject selectionMarker;
    [Tooltip("Texto opcional para indicar que una unidad fue elegida más de una vez.")]
    [SerializeField] private TextMesh selectionCountLabel;
    [SerializeField] private TextMesh healthLabel;
    [Tooltip("Su escala X representa la vida restante. Coloca el pivote a la izquierda.")]
    [SerializeField] private Transform healthBarFill;

    private int currentHealth;
    private Vector3 initialHealthBarScale = Vector3.one;

    public event Action<BattleUnit> HealthChanged;
    public event Action<BattleUnit> Defeated;

    public BattleColor Color { get { return color; } }
    public int MaxHealth { get { return maxHealth; } }
    public int CurrentHealth { get { return currentHealth; } }
    public int AttackPower { get { return attackPower; } }
    public bool IsDefeated { get { return currentHealth <= 0; } }

    private void Awake()
    {
        if (healthBarFill != null)
        {
            initialHealthBarScale = healthBarFill.localScale;
        }

        ResetForBattle();
    }

    public void ResetForBattle()
    {
        currentHealth = maxHealth;
        SetSelectionCount(0);
        RefreshHealthPresentation();
        gameObject.SetActive(true);
        HealthChanged?.Invoke(this);
    }

    public int TakeDamage(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException("amount", "El daño no puede ser negativo.");
        }

        if (IsDefeated || amount == 0)
        {
            return 0;
        }

        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        int appliedDamage = previousHealth - currentHealth;

        RefreshHealthPresentation();
        HealthChanged?.Invoke(this);

        if (IsDefeated)
        {
            SetSelectionCount(0);
            Defeated?.Invoke(this);
        }

        return appliedDamage;
    }

    public void SetSelectionCount(int count)
    {
        int safeCount = Mathf.Max(0, count);

        if (selectionMarker != null)
        {
            selectionMarker.SetActive(safeCount > 0);
        }

        if (selectionCountLabel != null)
        {
            selectionCountLabel.gameObject.SetActive(safeCount > 1);
            selectionCountLabel.text = safeCount > 1 ? "x" + safeCount : string.Empty;
        }
    }

    private void RefreshHealthPresentation()
    {
        if (healthLabel != null)
        {
            healthLabel.text = currentHealth + " / " + maxHealth;
        }

        if (healthBarFill != null)
        {
            float normalizedHealth = maxHealth > 0
                ? (float)currentHealth / maxHealth
                : 0f;

            healthBarFill.localScale = new Vector3(
                initialHealthBarScale.x * normalizedHealth,
                initialHealthBarScale.y,
                initialHealthBarScale.z);
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        attackPower = Mathf.Max(1, attackPower);
    }
}
