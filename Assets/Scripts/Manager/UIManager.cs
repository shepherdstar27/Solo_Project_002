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

    private HashSet<string> _loadingKeys = new HashSet<string>();

    public async UniTask<T> OpenUIAsync<T>(string addressKey) where T : UIBase
    {
        UIBase opened;
        if (_openedUIs.TryGetValue(addressKey, out opened))
        {
            opened.gameObject.SetActive(true);
            opened.OnOpen();

            T cachedUI = opened as T;
            if (cachedUI == null)
            {
                Debug.LogError($"[UIManager] 캐시된 UI 타입 불일치: {addressKey} / 실제 {opened.GetType().Name} / 요청 {typeof(T).Name}");
            }
            return cachedUI;
        }

        // 로딩 중 중복 호출 방지
        if (_loadingKeys.Contains(addressKey))
        {
            while (_loadingKeys.Contains(addressKey))
            {
                await UniTask.Yield();
            }

            if (_openedUIs.TryGetValue(addressKey, out opened))
            {
                opened.gameObject.SetActive(true);
                opened.OnOpen();
                return opened as T;
            }

            Debug.LogError($"[UIManager] 중복 로딩 대기 후에도 UI가 없습니다: {addressKey}");
            return null;
        }

        _loadingKeys.Add(addressKey);

        // Addressables는 등록되지 않은 주소를 넘기면 null을 돌려주는 게 아니라 예외를 던진다.
        // 잡지 않으면 호출한 쪽의 await 뒷부분이 통째로 실행되지 않아 게임이 멈춘 것처럼 보인다.
        GameObject prefab = null;
        try
        {
            prefab = await Addressables.LoadAssetAsync<GameObject>(addressKey).ToUniTask();
        }
        catch (System.Exception exception)
        {
            _loadingKeys.Remove(addressKey);
            Debug.LogError($"[UIManager] UI 주소를 찾을 수 없습니다: {addressKey} ({exception.GetType().Name})");
            return null;
        }

        if (prefab == null)
        {
            _loadingKeys.Remove(addressKey);
            Debug.LogError($"[UIManager] UI 프리팹 로드 실패: {addressKey}");
            return null;
        }

        GameObject instance = Instantiate(prefab);

        // 루트에서 요청한 타입을 직접 찾는다.
        // GetComponent<UIBase>()로 받으면 루트에 다른 UIBase 파생 컴포넌트가 먼저 붙어 있을 때
        // 캐스팅이 조용히 null이 되어 원인을 알 수 없다.
        T ui = instance.GetComponent<T>();
        if (ui == null)
        {
            _loadingKeys.Remove(addressKey);
            Debug.LogError($"[UIManager] {typeof(T).Name} 컴포넌트가 프리팹 루트에 없습니다: {addressKey}");
            Destroy(instance);
            return null;
        }

        RectTransform layerRoot = GetLayerRoot(ui.Layer);
        instance.transform.SetParent(layerRoot, false);

        _openedUIs.Add(addressKey, ui);
        _loadingKeys.Remove(addressKey);

        ui.OnOpen();
        return ui;
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