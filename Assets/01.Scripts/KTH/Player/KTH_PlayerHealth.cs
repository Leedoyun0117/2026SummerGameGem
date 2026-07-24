
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
    public void PlusMaxHp(int plushp)=>
        maxhp += plushp;
    
}
