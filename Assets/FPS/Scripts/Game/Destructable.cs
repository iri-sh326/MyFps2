using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// �׾��� �� Health�� ���� ������Ʈ�� ų�ϴ� Ŭ����
    /// </summary>
    public class Destructable : MonoBehaviour
    {
        #region Variables
        private Health health;
        #endregion

        private void Start()
        {
            health = GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, Destructable>(health, this, gameObject);

            // UnityAction �Լ� ���
            health.OnDie += OnDie;  // Health ��ũ��Ʈ�� ����� OnDie ����Ƽ �Լ��� ���    // null�̸� �ȵǱ� ������ ����ڵ尡 ���� �͸��� �ƴ�
            health.OnDamaged += OnDamaged;

        }

        void OnDamaged(float damage, GameObject damageSource)
        {
            // TODO : ������ ȿ�� ����
        }
        void OnDie()
        {
            // ų
            Destroy(gameObject);
        }
    }
}

