using UnityEngine;
using DG.Tweening;
using UnityEngine.Serialization;

public class CopiableObjectIndicator : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    private Material _material;
    private Tweener _glowTween;

    private void Awake()
    {
        if(!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            _material = spriteRenderer.material;
        }
    }

    /// <summary>
    /// Animates the sprite's glow from 0 to 10.
    /// </summary>
    public void OnHighlight()
    {
        if (_material == null) return;

        _glowTween?.Kill();
        _glowTween = _material.DOFloat(10f, "_Glow", 0.2f);
    }

    /// <summary>
    /// Animates the sprite's glow from 10 to 0.
    /// </summary>
    public void OnDishighlight()
    {
        if (_material == null) return;

        _glowTween?.Kill();
        _glowTween = _material.DOFloat(0f, "_Glow", 0.2f);
    }
}