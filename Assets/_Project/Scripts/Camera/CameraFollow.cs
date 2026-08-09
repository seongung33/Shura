using UnityEngine;

namespace Shura.Camera
{
    public class CameraFollow : MonoBehaviour
    {
        private const string ScreenShakeKey = "settings.screenShake";
        private const float ScreenShakeStrengthMultiplier = 0.5f;

        [SerializeField] private Transform target;
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
        private float shakeTime;
        private float shakeStrength;

        public static bool ScreenShakeEnabled
        {
            get => PlayerPrefs.GetInt(ScreenShakeKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(ScreenShakeKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void Shake(float strength, float duration)
        {
            if (!ScreenShakeEnabled)
            {
                shakeStrength = 0f;
                shakeTime = 0f;
                return;
            }

            shakeStrength = Mathf.Max(
                shakeStrength,
                strength * ScreenShakeStrengthMultiplier
            );
            shakeTime = Mathf.Max(shakeTime, duration);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            Vector3 nextPosition = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

            if (!ScreenShakeEnabled)
            {
                shakeStrength = 0f;
                shakeTime = 0f;
            }

            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                nextPosition += (Vector3)(Random.insideUnitCircle * shakeStrength);
                shakeStrength = Mathf.MoveTowards(shakeStrength, 0f, Time.unscaledDeltaTime * 0.8f);
            }
            transform.position = nextPosition;
        }
    }
}
