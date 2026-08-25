using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class TargetShowcase : MonoBehaviour
{
    [SerializeField] private Transform Transform_ModelRoot;
    [SerializeField] private float _rotateSpeed = 45f;
    [SerializeField] private float _displaySize = 2.2f;
    [SerializeField] private int _showcaseLayer = 8;   // Showcase 레이어 번호

    private GameObject _currentModel;
    private string _currentTargetId;

    public async UniTask ShowTargetAsync(string targetId)
    {
        if (_currentTargetId == targetId && _currentModel != null)
        {
            return;   // 같은 종류면 교체하지 않음
        }

        AbsorbTargetData data = GameDataManager.Instance.GetData<AbsorbTargetData>(targetId);
        if (data == null)
        {
            return;
        }

        GameObject prefab = await Addressables.LoadAssetAsync<GameObject>(data.PrefabKey).ToUniTask();
        if (prefab == null)
        {
            return;
        }

        ClearCurrentModel();

        _currentModel = Instantiate(prefab, Transform_ModelRoot);
        _currentTargetId = targetId;

        SetupModel(_currentModel);
    }

    private void SetupModel(GameObject model)
    {
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        // 크기와 무관하게 일정한 크기로 보이도록 정규화
        Bounds bounds = CalculateBounds(model);
        float maxSide = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxSide > 0.001f)
        {
            float scale = _displaySize / maxSide;
            model.transform.localScale = Vector3.one * scale;
        }

        SetLayerRecursive(model, _showcaseLayer);
        DisableComponents(model);
    }

    private Bounds CalculateBounds(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    private void DisableComponents(GameObject model)
    {
        // 전시용이므로 물리·게임 로직 컴포넌트 제거
        Collider[] colliders = model.GetComponentsInChildren<Collider>();
        foreach (Collider bodyCollider in colliders)
        {
            bodyCollider.enabled = false;
        }

        Rigidbody[] bodies = model.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody body in bodies)
        {
            Destroy(body);
        }

        AbsorbableObject[] absorbables = model.GetComponentsInChildren<AbsorbableObject>();
        foreach (AbsorbableObject absorbable in absorbables)
        {
            Destroy(absorbable);
        }
    }

    private void SetLayerRecursive(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    private void ClearCurrentModel()
    {
        if (_currentModel != null)
        {
            Destroy(_currentModel);
            _currentModel = null;
        }
    }

    private void Update()
    {
        if (Transform_ModelRoot == null)
        {
            return;
        }
        Transform_ModelRoot.Rotate(Vector3.up, _rotateSpeed * Time.deltaTime, Space.Self);
    }
}