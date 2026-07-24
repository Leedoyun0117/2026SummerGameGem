using DG.Tweening;
using UnityEngine;
using Unity.Cinemachine;

public class KTH_CameraMoving : MonoBehaviour
{
    public static KTH_CameraMoving Instance { get; private set; }

    [Header("유니티 6 시네마머신 카메라")]
    [SerializeField] private CinemachineCamera vcam; // 🔥 유니티 6 전용 컴포넌트

    [Header("줌(확대) 설정")]
    [Tooltip("기본 카메라 크기 (원래 뷰)")]
    [SerializeField] private float defaultLensSize = 5f;
    [Tooltip("플레이어 이동 시 확대될 카메라 크기 (작을수록 확대)")]
    [SerializeField] private float zoomInLensSize = 3f;
    [Tooltip("확대/축소에 걸리는 시간")]
    [SerializeField] private float zoomDuration = 0.5f;
    [SerializeField] private Ease zoomEase = Ease.OutQuad;

    private Tween zoomTween;

    private void Awake()
    {
        Instance = this;

        if (vcam == null)
            vcam = GetComponent<CinemachineCamera>();
    }

    private void Start()
    {
        // 시작 시 기본 Lens Size 설정
        if (vcam != null)
        {
            vcam.Lens.OrthographicSize = defaultLensSize;
        }
    }

    /// <summary>
    /// 플레이어 이동 시작 시 호출 (카메라 줌인)
    /// </summary>
    public void ZoomIn()
    {
        if (vcam == null) return;

        zoomTween?.Kill();
        zoomTween = DOTween.To(
            () => vcam.Lens.OrthographicSize,
            x => vcam.Lens.OrthographicSize = x,
            zoomInLensSize,
            zoomDuration
        ).SetEase(zoomEase);
    }

    /// <summary>
    /// 플레이어 이동 완료 / 보스 턴으로 전환 시 호출 (카메라 줌아웃)
    /// </summary>
    public void ZoomOut()
    {
        if (vcam == null) return;

        zoomTween?.Kill();
        zoomTween = DOTween.To(
            () => vcam.Lens.OrthographicSize,
            x => vcam.Lens.OrthographicSize = x,
            defaultLensSize,
            zoomDuration
        ).SetEase(zoomEase);
    }

    private void OnDestroy()
    {
        zoomTween?.Kill();
    }
}
