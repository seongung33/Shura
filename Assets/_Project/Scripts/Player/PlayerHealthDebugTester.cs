using UnityEngine;
using UnityEngine.InputSystem;

namespace Shura.Player
{
    // 테스트 전용: Space 키를 누르면 데미지를 준다.
    // 실제 적의 공격 판정이 준비되면 이 컴포넌트는 제거해도 된다.
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerHealthDebugTester : MonoBehaviour
    {
        [SerializeField] private int testDamage = 10;

        private PlayerHealth health;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                health.TakeDamage(testDamage);
            }
        }
    }
}
