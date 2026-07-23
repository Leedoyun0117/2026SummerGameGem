
using UnityEngine;
using UnityEngine.InputSystem;

public class KTH_PlayerHealth : MonoBehaviour
{
    // 씬에 플레이어가 하나뿐이라는 전제로, 다른 시스템(적 반사 데미지, 턴 타이머 등)이 참조를 따로
    // 안 받아도 바로 데미지를 줄 수 있게 싱글턴으로 노출한다.
    public static KTH_PlayerHealth Instance { get; private set; }

    [SerializeField]private int maxhp;
    private int curhp;

    public int MaxHP => maxhp;
    public int CurrentHP => curhp;

    // HP가 바뀔 때마다(현재hp, 최대hp) 알려준다 - UI 등에서 매 프레임 폴링하지 않고 이 이벤트만 구독하면 됨.
    public event System.Action<int, int> OnHealthChanged;

    [Header("개발자 테스트용 - D키를 누르면 데미지 1을 입는다")]
    [SerializeField] private bool devDamageKeyEnabled = true;

    private void Awake()
    {
        Instance = this;
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
        curhp-=damage;
        OnHealthChanged?.Invoke(curhp, maxhp);
        if(curhp <= 0) Destroy(gameObject);
    }
}
