using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drains sanity when the character is outside the vision circle,
/// regenerates it when inside. Calls GameManager.TriggerLose on depletion.
/// </summary>
public class SanityManager : MonoBehaviour
{
    [Header("References")]
    public Transform       player;
    public VisionController vision;
    public Slider          sanitySlider;

    [Header("Parameters")]
    public float maxSanity  = 100f;
    public float drainRate  = 15f;
    public float regenRate  =  8f;

    public float CurrentSanity    { get; private set; }
    public bool  IsPlayerInSight  { get; private set; }

    private bool _depletedFired = false;

    private void Start()
    {
        CurrentSanity = maxSanity;
        UpdateUI();
    }

    private void Update()
    {
        if (player == null || vision == null) return;

        float dist = Vector2.Distance(player.position, vision.WorldPosition);
        IsPlayerInSight = dist <= vision.Radius;

        CurrentSanity += (IsPlayerInSight ? regenRate : -drainRate) * Time.deltaTime;
        CurrentSanity  = Mathf.Clamp(CurrentSanity, 0f, maxSanity);

        UpdateUI();

        if (CurrentSanity <= 0f && !_depletedFired)
        {
            _depletedFired = true;
            GameManager.Instance?.TriggerLose(LoseCause.Sanity);
        }
    }

    private void UpdateUI()
    {
        if (sanitySlider != null)
            sanitySlider.value = CurrentSanity / maxSanity;
    }
}
