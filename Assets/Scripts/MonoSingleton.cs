using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>{
    public static T Instance {get;private set;}
    protected virtual void Awake(){
        if(Instance){
            Debug.LogWarning("More than one Instance exist.");
            Destroy(this);
            return;
        }
        Instance = this as T;
    }
}