
using UnityEngine;
using UnityEngine.InputSystem;

public class KTH_PlayerHealth : MonoBehaviour
{
public static KTH_PlayerHealth Instance { get; private set; }

[SerializeField] private int maxhp;
private int curhp;

public int MaxHP => maxhp;
public int CurrentHP => curhp;

public event System.Action<int, int> OnHealthChanged;

// 체력이 0 이하로 떨어져 죽음이 확정된 순간(부활 소진 이후) 딱 1번 호출된다 - 사망 연출(LDY_PlayerDeathSequence)이 이걸 구독한다.
public event System.Action OnPlayerDied;

[Header("개발자 테스트용 - D키를 누르면 데미지 1을 입는다")]
[SerializeField] private bool devDamageKeyEnabled = true;

// 토성의 반지 - 현재체력이 0 이하가 되는 순간을 1회에 한해 막아주는 부활 횟수.
public int reviveCharges;

public bool IsDead { get; private set; }

    private void Awake()
    {
        // 맵 <-> 전투 씬을 오가도 체력이 유지되고 UI가 계속 떠 있도록, 이미 하나 있으면(이전 씬에서
        // 넘어온 것) 이번에 새로 생긴 쪽을 파괴하고, 처음이면 파괴되지 않게 표시해둔다.
        if (Instance != null && Instance != this)
        {
            // 이 씬에 체력 UI가 다시 배치되어 있다는 건 실제 게임플레이 씬(맵/전투)에 들어왔다는 뜻이다.
            // 죽어서 타이틀로 갈 때 SetActive(false)로 꺼뒀을 수 있으니 여기서 다시 켜준다.
            Instance.transform.root.gameObject.SetActive(true);
            Destroy(transform.root.gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad는 "최상위(루트)" 오브젝트에만 걸 수 있다 - 이 스크립트가 UI 캔버스 등
        // 자식에 붙어있으면 gameObject 그대로는 예외가 나서(그 뒤 초기화가 전부 씹힘) transform.root를 쓴다.
        DontDestroyOnLoad(transform.root.gameObject);

        curhp=maxhp;
        OnHealthChanged?.Invoke(curhp, maxhp);
    }

    private void Update()
    {
        if (!devDamageKeyEnabled || Keyboard.current == null) return;
        if (Keyboard.current[Key.D].wasPressedThisFrame) TakeDamage(1);
    }

    public void Heal(int heal)
    {
        curhp+=heal;

        if (curhp >= maxhp) curhp=maxhp;

        OnHealthChanged?.Invoke(curhp, maxhp);
    }
    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        curhp-=damage;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SfxId.Hit);
        }

        if (curhp <= 0 && reviveCharges > 0)
        {
            reviveCharges--;
            curhp = 10;
        }

        if (curhp <= 0) curhp = 0;

        OnHealthChanged?.Invoke(curhp, maxhp);

        // 사망 연출(하트가 튀었다가 떨어지고, 화면이 검게 줄어들며 Defeat 후 씬 이동)은
        // LDY_PlayerDeathSequence가 이 이벤트를 구독해서 재생한다 - 여기서 바로 파괴하지 않는다.
        if (curhp <= 0 && !IsDead)
        {
            IsDead = true;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SfxId.Death);
            }

            OnPlayerDied?.Invoke();
        }
    }
    public void PlusMaxHp(int plushp)=>
        maxhp += plushp;

    // 외계 선인장 - 최대체력을 깎은 뒤 곧바로 현재체력을 그 최대체력만큼 꽉 채운다.
    public void SetToFull()
    {
        curhp = maxhp;
        OnHealthChanged?.Invoke(curhp, maxhp);
    }

    // 중력장 사과 - 현재체력과 최대체력 수치를 맞바꾸고, 그 결과 새 최대체력을 넘어선 만큼(초과분)을 돌려준다.
    // (초과분은 호출한 쪽에서 골드로 환전한다)
    public int SwapCurrentAndMax()
    {
        int oldCur = curhp;
        int oldMax = maxhp;

        maxhp = oldCur;
        curhp = oldMax;

        int overflow = 0;
        if (curhp > maxhp)
        {
            overflow = curhp - maxhp;
            curhp = maxhp;
        }

        OnHealthChanged?.Invoke(curhp, maxhp);
        return overflow;
    }
}
