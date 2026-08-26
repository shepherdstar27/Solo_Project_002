using UnityEngine;

public enum UILayer
{
    Background,
    Scene,
    Popup,
    Overlay,
    System,
}

public class UIBase : MonoBehaviour
{
    [SerializeField] private UILayer _layer = UILayer.Scene;

    public UILayer Layer { get { return _layer; } }

    public virtual void OnOpen()
    {
    }

    public virtual void OnClose()
    {
    }

    public void Close()
    {
        UIManager.Instance.CloseUI(this);
    }
}