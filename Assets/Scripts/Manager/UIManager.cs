using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class UIManager : SingletonBase<UIManager>
{
    [SerializeField] private Canvas Canvas_Root;
    [SerializeField] private RectTransform RectTransform_Background;
    [SerializeField] private RectTransform RectTransform_Scene;
    [SerializeField] private RectTransform RectTransform_Popup;
    [SerializeField] private RectTransform RectTransform_Overlay;
    [SerializeField] private RectTransform RectTransform_System;

    private Dictionary<string, UIBase> _openedUIs = new Dictionary<string, UIBase>();

    public async UniTask<T> OpenUIAsync<T>(string addressKey) where T : UIBase
    {
        UIBase opened;
        if (_openedUIs.TryGetValue(addressKey, out opened))
        {
            opened.gameObject.SetActive(true);
            opened.OnOpen();
            return opened as T;
        }

        GameObject prefab = await Addressables.LoadAssetAsync<GameObject>(addressKey).ToUniTask();
        if (prefab == null)
        {
            Debug.LogError($"[UIManager] UI 프리팹 로드 실패: {addressKey}");
            return null;
        }

        GameObject instance = Instantiate(prefab);
        UIBase ui = instance.GetComponent<UIBase>();
        if (ui == null)
        {
            Debug.LogError($"[UIManager] UIBase가 없습니다: {addressKey}");
            Destroy(instance);
            return null;
        }

        RectTransform layerRoot = GetLayerRoot(ui.Layer);
        instance.transform.SetParent(layerRoot, false);

        _openedUIs.Add(addressKey, ui);
        ui.OnOpen();

        return ui as T;
    }

    public void CloseUI(UIBase ui)
    {
        if (ui == null)
        {
            return;
        }

        ui.OnClose();
        ui.gameObject.SetActive(false);
    }

    public void CloseUI(string addressKey)
    {
        UIBase ui;
        if (_openedUIs.TryGetValue(addressKey, out ui) == false)
        {
            return;
        }
        CloseUI(ui);
    }

    public T GetUI<T>(string addressKey) where T : UIBase
    {
        UIBase ui;
        if (_openedUIs.TryGetValue(addressKey, out ui) == false)
        {
            return null;
        }
        return ui as T;
    }

    private RectTransform GetLayerRoot(UILayer layer)
    {
        switch (layer)
        {
            case UILayer.Background: return RectTransform_Background;
            case UILayer.Scene: return RectTransform_Scene;
            case UILayer.Popup: return RectTransform_Popup;
            case UILayer.Overlay: return RectTransform_Overlay;
            case UILayer.System: return RectTransform_System;
            default: return RectTransform_Scene;
        }
    }
}