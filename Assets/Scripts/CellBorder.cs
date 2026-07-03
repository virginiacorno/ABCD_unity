using UnityEngine;

public class MatchParentVisibility : MonoBehaviour
{
    public Renderer parentRenderer;
    private Renderer[] _renderers;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        bool visible = parentRenderer != null && parentRenderer.enabled;
        foreach (var r in _renderers)
        {
            r.enabled = visible;
        }
    }
}