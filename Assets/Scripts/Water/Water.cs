using Game2DWaterKit;
using UnityEngine;

public class Water : MonoBehaviour
{
    [SerializeField] private Game2DWater _waterPrefab;

    private void Start()
    {
        var instantiated = Instantiate(_waterPrefab, (Vector2)transform.position + new Vector2(0.5f, -0.5f) * (Vector2)transform.parent.localScale, transform.rotation);
        instantiated.transform.parent = transform.parent.parent;
        instantiated.MainModule.SetSize(transform.parent.localScale, true);
    }
}