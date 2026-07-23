using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class KTH_CloseEye : MonoBehaviour
{
    [SerializeField] private Image _topImage;
    [SerializeField] private Image _bottomImage;

    [SerializeField] private float _closeTime = 0.4f;
    [SerializeField] private float _holdTime = 1.0f;
    [SerializeField] private float _openTime = 0.4f;

    private RectTransform _topRect;
    private RectTransform _bottomRect;

    private Vector2 _topStartPos;
    private Vector2 _bottomStartPos;

    public static KTH_CloseEye Instance;

    private void Awake()
    {
        Instance = this;
        _topRect = _topImage.rectTransform;
        _bottomRect = _bottomImage.rectTransform;

        _topStartPos = _topRect.anchoredPosition;
        _bottomStartPos = _bottomRect.anchoredPosition;
    }

    public void OnEnable()
    {

    }
    public void CloseEye()
    {
        Sequence seq = DOTween.Sequence();

        seq.Append(_topRect.DOAnchorPosY(0, _closeTime));
        seq.Join(_bottomRect.DOAnchorPosY(0, _closeTime));

        seq.AppendInterval(_holdTime);

        seq.Append(_topRect.DOAnchorPos(_topStartPos, _openTime));
        seq.Join(_bottomRect.DOAnchorPos(_bottomStartPos, _openTime));
    }
}
