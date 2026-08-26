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
        Debug.Log($"[Showcase] 호출됨: {targetId}");

        if (_currentTargetId == targetId && _currentModel != null)
        {
            return;
        }

        AbsorbTargetData data = GameDataManager.Instance.GetData<AbsorbTargetData>(targetId);
        if (data == null)
        {
            Debug.LogError($"[Showcase] 데이터 없음: {targetId}");
            return;
        }

        GameObject prefab = await Addressables.LoadAssetAsync<GameObject>(data.PrefabKey).ToUniTask();
        if (prefab == null)
        {
            Debug.LogError($"[Showcase] 프리팹 로드 실패: {data.PrefabKey}");
            return;
        }

        Debug.Log($"[Showcase] 모델 생성: {data.Name}");

        ClearCurrentModel();

        _currentModel = Instantiate(prefab, Transform_ModelRoot);
        _currentTargetId = targetId;

        SetupModel(_currentModel);
    }

    private void SetupModel(GameObject model)
    {
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        SetLayerRecursive(model, _showcaseLayer);
        DisableComponents(model);

        // 1. 크기 정규화
        Bounds bounds = CalculateBounds(model);
        float maxSide = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        if (maxSide > 0.001f)
        {
            float scale = _displaySize / maxSide;
            model.transform.localScale = Vector3.one * scale;
        }

        // 2. 스케일 적용 후 중심을 다시 계산해 원점으로 보정
        Bounds scaledBounds = CalculateBounds(model);
        Vector3 offset = Transform_ModelRoot.position - scaledBounds.center;
        model.transform.position += offset;
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