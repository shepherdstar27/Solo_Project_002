using UnityEngine;

public class SingletonBase<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this as T)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            DontDestroyOnLoad(transform.root.gameObject);
        }
    }
}