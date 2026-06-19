using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the flame Image of a UI torch to get a random flicker effect.
/// Works in unscaled time so it keeps animating on pause screens.
/// </summary>
public class TorchFlicker : MonoBehaviour
{
    [Header("References")]
    public Image flameImage;

    [Header("Flicker settings")]
    public float minAlpha     = 0.55f;
    public float maxAlpha     = 1.0f;
    public float flickerSpeed = 6f;
    [Tooltip("Random phase offset so multiple torches don't sync.")]
    public float phaseOffset  = 0f;

    private float _timer;
    private float _targetAlpha;
    private float _currentAlpha;

    private void Start()
    {
        if (flameImage == null) flameImage = GetComponent<Image>();
        _currentAlpha = Random.Range(minAlpha, maxAlpha);
        _targetAlpha  = _currentAlpha;
        _timer        = phaseOffset;
    }

    private void Update()
    {
        _timer += Time.unscaledDeltaTime * flickerSpeed;
        if (_timer >= 1f)
        {
            _timer       = 0f;
            _targetAlpha = Random.Range(minAlpha, maxAlpha);
        }

        _currentAlpha = Mathf.Lerp(_currentAlpha, _targetAlpha,
            Time.unscaledDeltaTime * flickerSpeed * 2f);

        if (flameImage != null)
        {
            Color c = flameImage.color;
            c.a = _currentAlpha;
            flameImage.color = c;
        }
    }
}
