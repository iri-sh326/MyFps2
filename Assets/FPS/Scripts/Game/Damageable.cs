using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// �������� �Դ� �浹ü(hit box)�� �����Ǿ� �������� �����ϴ� Ŭ����
    /// </summary>
    public class Damageable : MonoBehaviour
    {
        #region Variables
        private Health health;

        // ������ ���
        [SerializeField] private float damageMultiplier = 1f;

        // �ڽ��� ���� ������ ���
        [SerializeField] private float sensibilityToSelfDamage = 0.5f;
        #endregion

        private void Awake()
        {
            health = GetComponent<Health>();
            if(health == null)  // GetComponent�� �ߴµ� �� ã�Ҵ�  // �θ𿡰Լ� ������Ʈ ������ ����
            {
                health = GetComponentInParent<Health>();
            }
        }

        public void InflictDamage(float damage, bool isExplosionDamage, GameObject damageSource)    // isExplosionDamage : ���߿� ���� ������ ����
        {
            if (health == null) return;

            // ���� ������ ��
            var totalDamage = damage;

            // ���� ������ üũ - ���� �������� ���� damageMultiplier�� ������� �ʴ´�
            if(isExplosionDamage == false)
            {
                totalDamage *= damageMultiplier;
            }

            // �ڽ��� ���� ��������
            if(health.gameObject == damageSource)
            {
                totalDamage *= sensibilityToSelfDamage;
            }



            // ������ ������
            health.TakeDamage(totalDamage, damageSource);
        }
    }
}

