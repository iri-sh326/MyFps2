using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Game
{
    /// <summary>
    /// ü���� �����ϴ� Ŭ����
    /// </summary>
    public class Health : MonoBehaviour
    {
        #region Variables
        [SerializeField] private float maxHealth = 100f;    // �ִ� HP
        public float CurrentHealth {  get; private set; }   // ���� HP    // ��� ������ ������ �ʰ� �ڵ� �Ӽ�(property)���� ����ó�� ��� ����
        private bool isDeath = false;                       // ���� üũ

        public UnityAction<float, GameObject> OnDamaged;
        public UnityAction OnDie;
        public UnityAction<float> OnHeal;

        // ���� ü�� ��� ����
        [SerializeField] private float criticalHealthRatio = 0.3f;      
        
        // ����
        public bool Invincible {  get; private set; }
        #endregion

        // �� �������� ���� �� �ִ��� üũ
        public bool CanPickUp() => CurrentHealth < maxHealth;   // Ǯ�� ����
        // UI HP ������ ��(���� ü�� ���� ���ϱ�)
        public float GetRatio() => CurrentHealth / maxHealth;
        // ���� üũ
        public bool IsCritical() => GetRatio() <= criticalHealthRatio;  // ü���� 30�ۼ�Ʈ ���ϸ� ���� ���

        private void Start()
        {
            // �ʱ�ȭ
            CurrentHealth = maxHealth;
            Invincible = false;
        }

        // ��
        public void Heal(float amount)
        {
            float beforeHealth = CurrentHealth;
            CurrentHealth += amount;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);

            // real Heal ���ϱ�
            float realHeal = CurrentHealth - beforeHealth;
            if (realHeal > 0f)
            {
                OnHeal?.Invoke(realHeal);
            }
        }

        // damageSource: �������� �ִ� ��ü
        public void TakeDamage(float damage, GameObject damageSource)
        {
            // ���� üũ
            if (Invincible) return;

            float beforeHealth = CurrentHealth;
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);

            // real Damage ���ϱ�
            float realDamage = beforeHealth - CurrentHealth;
            if(realDamage > 0f)
            {
                // ������ ����
                OnDamaged?.Invoke(realDamage, damageSource);    // null �� �ƴϸ� �κ�ũ �Լ� ȣ��
            }

            // ���� ó��
            HandleDeath();
        }

        // ���� ó��
        private void HandleDeath()
        {
            // ���� üũ // �ι� ���̱� ����
            if (isDeath) return;

            if (CurrentHealth <= 0f)
            {
                isDeath = true;

                // ���� ����..
                OnDie?.Invoke();
            }
        }
    }
}

