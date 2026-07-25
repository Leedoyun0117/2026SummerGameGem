using System.Collections;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public enum Type
{
    None,
    Gold,
    Gambling,
}

public class KTH_EventSystem : MonoBehaviour
{
    [System.Serializable]
    public class Event
    {
        public Sprite pic;
        [TextArea]
        public string ex;
        public Type type;
        public int per; // 가중치 (확률 비율)
    }

    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Event[] eventI;

    [Header("도박 이벤트 설정")]
    [Range(0f, 1f)]
    [SerializeField] private float gamblingWinChance = 0.5f;

    [Header("텍스트 연출")]
    [Tooltip("텍스트가 다 나오기까지 걸리는 시간(초)")]
    [SerializeField] private float textRevealDuration = 1.5f;

    [Tooltip("결과 텍스트가 다 나온 뒤, 창이 닫히기 전까지 대기하는 시간(초)")]
    [SerializeField] private float resultHoldDuration = 1f;


    private int check;
    // 지금 화면에 떠 있는 이벤트 (OnYes/OnNo에서 참조)
    private Event currentEvent;
    private Coroutine typingCoroutine;

    // 맵에서 상점처럼 팝업으로 껐다 켰다 재사용되므로, Start()가 아니라 OnEnable()에서 매번 새로 뽑아야
    // 두 번째로 이벤트 노드에 들어갔을 때도 이전 결과가 아니라 새 이벤트가 나온다.
    private void OnEnable()
    {
        check = 0;
        ShowRandomType();
    }

    private void ShowRandomType()
    {
        currentEvent = GetRandomAdByWeight();
        if (currentEvent == null) return;

        image.sprite = currentEvent.pic;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(currentEvent.ex, textRevealDuration));
    }

    /// <summary>
    /// duration초에 걸쳐 fullText를 한 글자씩 채워 넣습니다.
    /// </summary>
    private IEnumerator TypeText(string fullText, float duration)
    {
        text.text = "";

        if (string.IsNullOrEmpty(fullText))
            yield break;

        float delayPerChar = duration / fullText.Length;
        var sb = new StringBuilder();

        for (int i = 0; i < fullText.Length; i++)
        {
            sb.Append(fullText[i]);
            text.text = sb.ToString();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SfxId.Typing);
            }

            yield return new WaitForSeconds(delayPerChar);
        }
    }

    /// <summary>
    /// 결과 메시지를 타이핑으로 보여준 다음, resultHoldDuration만큼 더 기다렸다가 이벤트 창을 닫습니다.
    /// </summary>
    private IEnumerator ShowResultThenClose(string resultMessage)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(resultMessage, 0.7f));

        // 타이핑이 끝날 시간(0.7초) + 다 읽을 시간(resultHoldDuration)만큼 대기
        yield return new WaitForSeconds(0.7f + resultHoldDuration);

        StartCoroutine(CloseEvent());
    }

    private Event GetRandomAdByWeight()
    {
        int totalWeight = 0;
        foreach (var item in eventI)
        {
            totalWeight += item.per;
        }

        if (totalWeight <= 0) return null;

        int randomValue = Random.Range(0, totalWeight); // 0 ~ totalWeight-1
        int cumulative = 0;

        foreach (var item in eventI)
        {
            cumulative += item.per;
            if (randomValue < cumulative)
            {
                return item;
            }
        }

        // 혹시 못 찾을 경우 마지막 아이템 반환 (안전장치)
        return eventI[eventI.Length - 1];
    }

    public void OnYes()
    {
        if (check == 0)
        {

            if (currentEvent == null)
            {
                StartCoroutine(CloseEvent());
                return;
            }

            switch (currentEvent.type)
            {
                case Type.Gold:
                    HandleGold(2);
                    break;

                case Type.Gambling:
                    HandleGambling(2,3,1);
                    break;

                case Type.None:
                default:
                    StartCoroutine(TypeText(" 아무 일도 일어나지 않았다...", 0.7f));
                    break;
            }
            StartCoroutine(CloseEvent());

            check++;
        }
    }

    public void OnNo()
    {
        if (check == 0)
        {

            StartCoroutine(TypeText("무시하고 지나갔다", 0.7f));
        // 선택 안 함 - 효과 없이 그냥 닫기
        StartCoroutine(CloseEvent());
        check++;
           }
    }

    private void HandleGold(int amount)
    {
        StartCoroutine(TypeText("이건 나의 작은 선물이라네 (별의 조각 +2)", 0.7f));
        // StarPieceManager(JCY)는 JCY_TestScene에만 놓여있는 래퍼라 맵에서 곧바로 이벤트로 들어오면
        // instance가 없어 NullReferenceException이 났다 - 실제 재화를 들고 있는 LDY_StarPieceManager를 직접 부른다.
        LDY_StarPieceManager.Instance?.NotifyPieceCollected(amount);
    }


    private void HandleGambling(int startP, int winP,int loseP)
    {
        LDY_StarPieceManager.Instance?.Spend(startP);

        bool win = Random.value < gamblingWinChance;

        if (win)
        {
            StartCoroutine(TypeText("덜컹! 자판기에서 별의 조각 꾸러미가 떨어졌다! (별의 조각 +3)",0.7f));
            LDY_StarPieceManager.Instance?.NotifyPieceCollected(winP);
        }
        else
        {
            StartCoroutine(TypeText("화면에 ‘상품 준비 중’이라는 문구만 계속 깜빡인다. 아무 일도 일어나지 않았다. 경고음과 함께 자판기가 별의 조각을 삼켜버렸다. 환불 버튼은 고장 난 것 같다. (별의 조각 1 소실)", 0.7f));
            LDY_StarPieceManager.Instance?.Spend(loseP);
        }
    }

    // 맵의 상점처럼 팝업으로 쓰이므로, 씬을 넘기지 않고 팝업만 닫아서 맵으로 돌아간다.
    // KTH_EventSystem이 실제 팝업 루트(맵의 LDY_EventUIOpener가 켜고 끄는 eventUI)의 자식에 붙어있을
    // 수도 있어서, 자기 자신만 끄면 부모(진짜 보이는 패널)는 계속 켜진 채로 남는다 - 그래서 진짜 루트를
    // 아는 LDY_EventUIOpener 쪽 Close()를 대신 호출한다. 맵이 아니라 KTH_TestScene처럼 오프너 없이
    // 단독으로 쓰는 경우를 위해, 오프너가 없으면 예전처럼 자기 자신을 끈다.
    private IEnumerator CloseEvent()
    {
        yield return new WaitForSeconds(3f);

        LDY_EventUIOpener opener = FindObjectOfType<LDY_EventUIOpener>();
        if (opener != null)
            opener.Close();
        else
            gameObject.SetActive(false);
    }

   
}
