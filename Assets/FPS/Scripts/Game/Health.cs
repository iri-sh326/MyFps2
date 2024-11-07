using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 체력을 관리하는 클래스
    /// </summary>
    public class Health : MonoBehaviour
    {
        #region Variables
        [SerializeField] private float maxHealth = 100f;    // 최대 HP
        public float CurrentHealth {  get; private set; }   // 현재 HP    // 멤버 변수로 만들지 않고 자동 속성(property)으로 변수처럼 사용 가능
        private bool isDeath = false;                       // 죽음 체크

        public UnityAction<float, GameObject> OnDamaged;
        public UnityAction OnDie;
        public UnityAction<float> OnHeal;

        // 위험 체력 경계 비율
        [SerializeField] private float criticalHealthRatio = 0.3f;      
        
        // 무적
        public bool Invincible {  get; private set; }
        #endregion

        // 힐 아이템을 먹을 수 있는지 체크
        public bool CanPickUp() => CurrentHealth < maxHealth;   // 풀피 여부
        // UI HP 게이지 값(현재 체력 비율 구하기)
        public float GetRatio() => CurrentHealth / maxHealth;
        // 위험 체크
        public bool IsCritical() => GetRatio() <= criticalHealthRatio;  // 체력이 30퍼센트 이하면 위험 경고

        private void Start()
        {
            // 초기화
            CurrentHealth = maxHealth;
            Invincible = false;
        }

        // 힐
        public void Heal(float amount)
        {
            float beforeHealth = CurrentHealth;
            CurrentHealth += amount;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);

            // real Heal 구하기
            float realHeal = CurrentHealth - beforeHealth;
            if (realHeal > 0f)
            {
                OnHeal?.Invoke(realHeal);
            }
        }

        // damageSource: 데미지를 주는 주체
        public void TakeDamage(float damage, GameObject damageSource)
        {
            // 무적 체크
            if (Invincible) return;

            float beforeHealth = CurrentHealth;
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);

            // real Damage 구하기
            float realDamage = beforeHealth - CurrentHealth;
            if(realDamage > 0f)
            {
                // 데미지 구현
                OnDamaged?.Invoke(realDamage, damageSource);    // null 이 아니면 인보크 함수 호출
            }

            // 죽음 처리
            HandleDeath();
        }

        // 죽음 처리
        private void HandleDeath()
        {
            // 죽음 체크 // 두번 죽이기 방지
            if (isDeath) return;

            if (CurrentHealth <= 0f)
            {
                isDeath = true;

                // 죽음 구현..
                OnDie?.Invoke();
            }
        }
    }
}

