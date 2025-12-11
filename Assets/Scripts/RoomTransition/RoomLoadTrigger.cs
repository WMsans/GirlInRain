using UnityEngine;
using LDtkUnity; 

public class RoomLoadTrigger : MonoBehaviour
{
    private LDtkComponentLevel _levelComponent;

    private void Awake()
    {
        _levelComponent = GetComponent<LDtkComponentLevel>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object entering is the RoomLoader
        if (other.GetComponent<RoomLoader>() != null)
        {
            RoomLoadManager.Instance.OnEnterRoom(_levelComponent);
        }
    }
}