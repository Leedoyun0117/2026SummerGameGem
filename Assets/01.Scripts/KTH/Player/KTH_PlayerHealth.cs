
using UnityEngine;

public class KTH_PlayerHealth : MonoBehaviour
{

    [SerializeField]private int maxhp;
    private int curhp;

    private void Awake()
    {
        curhp=maxhp;
    }

    public void Heal(int heal)
    {
        curhp+=heal;

        if (curhp >= maxhp) curhp=maxhp;

    }
    public void TakeDamage(int damage)
    {
        curhp-=damage;
        if(curhp <= 0) Destroy(gameObject);
    }
}
