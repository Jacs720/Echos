using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum BattlePhase
{
    PlayerSelection,
    ResolvingTurn,
    Victory,
    Defeat
}

[DisallowMultipleComponent]
public sealed class BattleManager : MonoBehaviour
{
    [Header("Combatants")]
    [SerializeField] private BattleUnit[] playerUnits = new BattleUnit[3];
    [SerializeField] private BattleUnit enemy;

    [Header("Input")]
    [Tooltip("Desactívalo si usarás botones o el Input System nuevo.")]
    [SerializeField] private bool readLegacyInput = true;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private LayerMask selectableLayers = ~0;

    [Header("Combo balance")]
    [SerializeField, Min(0.01f)] private float secondaryPowerMultiplier = 1.35f;
    [SerializeField, Min(0.01f)] private float tertiaryPowerMultiplier = 1.70f;

    [Header("Optional presentation")]
    [Tooltip("Usa un sprite blanco; el sistema aplicará el tinte del color.")]
    [SerializeField] private SpriteRenderer resultCircle;
    [SerializeField] private TextMesh combinationLabel;
    [SerializeField] private TextMesh statusLabel;

    private readonly List<BattleUnit> selectedUnits =
        new List<BattleUnit>(ColorCombiner.MaxIngredients);

    private ColorMixResult currentMix = ColorMixResult.Incomplete();
    private string currentMessage = string.Empty;
    private BattlePhase phase;

    public event Action StateChanged;

    public BattlePhase Phase { get { return phase; } }
    public BattleUnit Enemy { get { return enemy; } }
    public int SelectedCount { get { return selectedUnits.Count; } }
    public ColorMixResult CurrentMix { get { return currentMix; } }
    public string CurrentMessage { get { return currentMessage; } }

    private void Awake()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        string configurationError;
        if (!TryValidateConfiguration(out configurationError))
        {
            Debug.LogError(configurationError, this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (enabled)
        {
            RestartBattle();
        }
    }

    private void Update()
    {
        if (!readLegacyInput)
        {
            return;
        }

        if (phase == BattlePhase.Victory || phase == BattlePhase.Defeat)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartBattle();
            }

            return;
        }

        if (phase != BattlePhase.PlayerSelection)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            SelectUnitUnderPointer();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmAttack();
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            UndoLastSelection();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearSelection();
        }
    }

    public void SelectUnit(BattleUnit unit)
    {
        if (phase != BattlePhase.PlayerSelection || unit == null)
        {
            return;
        }

        if (!IsPlayerUnit(unit) || unit.IsDefeated)
        {
            SetMessage("Esa unidad no puede participar en el ataque.");
            return;
        }

        if (!unit.Color.IsPrimary())
        {
            SetMessage("Solo los colores primarios pueden iniciar una mezcla.");
            return;
        }

        if (selectedUnits.Count >= ColorCombiner.MaxIngredients)
        {
            SetMessage("La mezcla ya tiene el máximo de tres colores.");
            return;
        }

        selectedUnits.Add(unit);
        ColorMixResult candidate = EvaluateSelection();

        if (candidate.State == ColorMixState.Invalid)
        {
            selectedUnits.RemoveAt(selectedUnits.Count - 1);
            SetMessage("Esa combinación no existe en la rueda cromática.");
            RefreshSelectionPresentation();
            return;
        }

        currentMix = candidate;
        currentMessage = candidate.State == ColorMixState.Valid
            ? "Ataque " + candidate.Color.ToDisplayName() + " listo."
            : "Añade un color primario distinto para completar la mezcla.";

        RefreshSelectionPresentation();
        NotifyStateChanged();
    }

    public void ConfirmAttack()
    {
        if (phase != BattlePhase.PlayerSelection)
        {
            return;
        }

        if (!currentMix.HasColor || selectedUnits.Count == 0)
        {
            SetMessage("La mezcla todavía no produce un color de ataque.");
            return;
        }

        phase = BattlePhase.ResolvingTurn;

        BattleColor attackColor = currentMix.Color;
        float basePower = CalculateComboBasePower();
        float playerEffectivity = Effectivity.GetMultiplier(attackColor, enemy.Color);
        int playerDamage = Effectivity.CalculateDamage(basePower, attackColor, enemy.Color);
        int appliedPlayerDamage = enemy.TakeDamage(playerDamage);

        string playerLine = string.Format(
            "Ataque {0}: {1} de daño. {2}",
            attackColor.ToDisplayName(),
            appliedPlayerDamage,
            DescribeEffectivity(playerEffectivity));

        ClearSelectionInternal();
        ShowResultColor(attackColor);

        if (enemy.IsDefeated)
        {
            phase = BattlePhase.Victory;
            currentMessage = playerLine + "\n¡Victoria! Pulsa R para reiniciar.";
            NotifyStateChanged();
            return;
        }

        BattleUnit target = ChooseRandomLivingPlayerUnit();
        float enemyEffectivity = Effectivity.GetMultiplier(enemy.Color, target.Color);
        int enemyDamage = Effectivity.CalculateDamage(
            enemy.AttackPower,
            enemy.Color,
            target.Color);
        int appliedEnemyDamage = target.TakeDamage(enemyDamage);

        string enemyLine = string.Format(
            "El enemigo golpea a {0}: {1} de daño. {2}",
            target.name,
            appliedEnemyDamage,
            DescribeEffectivity(enemyEffectivity));

        if (!HasLivingPlayerUnit())
        {
            phase = BattlePhase.Defeat;
            currentMessage = playerLine + "\n" + enemyLine + "\nDerrota. Pulsa R para reiniciar.";
        }
        else
        {
            phase = BattlePhase.PlayerSelection;
            currentMessage = playerLine + "\n" + enemyLine;
        }

        NotifyStateChanged();
    }

    public void UndoLastSelection()
    {
        if (phase != BattlePhase.PlayerSelection || selectedUnits.Count == 0)
        {
            return;
        }

        selectedUnits.RemoveAt(selectedUnits.Count - 1);
        currentMix = EvaluateSelection();
        currentMessage = selectedUnits.Count == 0
            ? "Selecciona de uno a tres colores."
            : "Última selección eliminada.";

        RefreshSelectionPresentation();
        NotifyStateChanged();
    }

    public void ClearSelection()
    {
        if (phase != BattlePhase.PlayerSelection)
        {
            return;
        }

        ClearSelectionInternal();
        HideResultColor();
        currentMessage = "Selección cancelada.";
        NotifyStateChanged();
    }

    public void RestartBattle()
    {
        ClearSelectionInternal();

        for (int i = 0; i < playerUnits.Length; i++)
        {
            if (playerUnits[i] != null)
            {
                playerUnits[i].ResetForBattle();
            }
        }

        enemy.ResetForBattle();
        phase = BattlePhase.PlayerSelection;
        currentMessage = "Selecciona de uno a tres colores y confirma el ataque.";
        HideResultColor();
        NotifyStateChanged();
    }

    private void SelectUnitUnderPointer()
    {
        if (gameplayCamera == null)
        {
            SetMessage("Asigna una cámara para seleccionar unidades con el ratón.");
            return;
        }

        Vector3 worldPoint = gameplayCamera.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(
            new Vector2(worldPoint.x, worldPoint.y),
            selectableLayers);

        if (hit == null)
        {
            return;
        }

        BattleUnit unit = hit.GetComponentInParent<BattleUnit>();
        SelectUnit(unit);
    }

    private float CalculateComboBasePower()
    {
        float totalPower = 0f;

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            totalPower += selectedUnits[i].AttackPower;
        }

        float averagePower = totalPower / selectedUnits.Count;

        if (selectedUnits.Count == 2)
        {
            return averagePower * secondaryPowerMultiplier;
        }

        if (selectedUnits.Count == 3)
        {
            return averagePower * tertiaryPowerMultiplier;
        }

        return averagePower;
    }

    private ColorMixResult EvaluateSelection()
    {
        switch (selectedUnits.Count)
        {
            case 0:
                return ColorMixResult.Incomplete();
            case 1:
                return ColorCombiner.Evaluate(selectedUnits[0].Color);
            case 2:
                return ColorCombiner.Evaluate(
                    selectedUnits[0].Color,
                    selectedUnits[1].Color);
            case 3:
                return ColorCombiner.Evaluate(
                    selectedUnits[0].Color,
                    selectedUnits[1].Color,
                    selectedUnits[2].Color);
            default:
                return ColorMixResult.Invalid();
        }
    }

    private void ClearSelectionInternal()
    {
        selectedUnits.Clear();
        currentMix = ColorMixResult.Incomplete();
        RefreshSelectionCounts();
    }

    private void RefreshSelectionPresentation()
    {
        RefreshSelectionCounts();

        if (currentMix.HasColor)
        {
            ShowResultColor(currentMix.Color);
        }
        else
        {
            HideResultColor();
        }
    }

    private void RefreshSelectionCounts()
    {
        for (int i = 0; i < playerUnits.Length; i++)
        {
            BattleUnit unit = playerUnits[i];
            if (unit == null)
            {
                continue;
            }

            int count = 0;
            for (int selectionIndex = 0; selectionIndex < selectedUnits.Count; selectionIndex++)
            {
                if (selectedUnits[selectionIndex] == unit)
                {
                    count++;
                }
            }

            unit.SetSelectionCount(count);
        }
    }

    private void ShowResultColor(BattleColor color)
    {
        if (resultCircle == null)
        {
            return;
        }

        resultCircle.enabled = true;
        resultCircle.color = color.ToUnityColor();
    }

    private void HideResultColor()
    {
        if (resultCircle != null)
        {
            resultCircle.enabled = false;
        }
    }

    private BattleUnit ChooseRandomLivingPlayerUnit()
    {
        int livingCount = 0;

        for (int i = 0; i < playerUnits.Length; i++)
        {
            if (playerUnits[i] != null && !playerUnits[i].IsDefeated)
            {
                livingCount++;
            }
        }

        int selectedLivingIndex = UnityEngine.Random.Range(0, livingCount);

        for (int i = 0; i < playerUnits.Length; i++)
        {
            BattleUnit unit = playerUnits[i];
            if (unit == null || unit.IsDefeated)
            {
                continue;
            }

            if (selectedLivingIndex == 0)
            {
                return unit;
            }

            selectedLivingIndex--;
        }

        throw new InvalidOperationException("No hay una unidad viva que pueda recibir el ataque.");
    }

    private bool HasLivingPlayerUnit()
    {
        for (int i = 0; i < playerUnits.Length; i++)
        {
            if (playerUnits[i] != null && !playerUnits[i].IsDefeated)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPlayerUnit(BattleUnit unit)
    {
        for (int i = 0; i < playerUnits.Length; i++)
        {
            if (playerUnits[i] == unit)
            {
                return true;
            }
        }

        return false;
    }

    private string BuildCombinationText()
    {
        if (selectedUnits.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(" + ");
            }

            builder.Append(selectedUnits[i].Color.ToDisplayName());
        }

        if (currentMix.HasColor)
        {
            builder.Append(" = ");
            builder.Append(currentMix.Color.ToDisplayName());
        }
        else
        {
            builder.Append(" + ?");
        }

        return builder.ToString();
    }

    private void SetMessage(string message)
    {
        currentMessage = message;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        if (statusLabel != null)
        {
            statusLabel.text = currentMessage;
        }

        if (combinationLabel != null)
        {
            combinationLabel.text = BuildCombinationText();
        }

        StateChanged?.Invoke();
    }

    private bool TryValidateConfiguration(out string error)
    {
        if (playerUnits == null || playerUnits.Length == 0)
        {
            error = "BattleManager necesita al menos una unidad del jugador.";
            return false;
        }

        for (int i = 0; i < playerUnits.Length; i++)
        {
            if (playerUnits[i] == null)
            {
                error = "BattleManager contiene una referencia vacía en Player Units.";
                return false;
            }
        }

        if (enemy == null)
        {
            error = "BattleManager necesita una unidad enemiga.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static string DescribeEffectivity(float multiplier)
    {
        if (multiplier >= 2f)
        {
            return "¡Súper efectivo!";
        }

        if (multiplier <= 0.5f)
        {
            return "Poco efectivo.";
        }

        return "Efectividad normal.";
    }

    private void OnValidate()
    {
        secondaryPowerMultiplier = Mathf.Max(0.01f, secondaryPowerMultiplier);
        tertiaryPowerMultiplier = Mathf.Max(0.01f, tertiaryPowerMultiplier);
    }
}
