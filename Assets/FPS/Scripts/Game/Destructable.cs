using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 죽었을 때 Health를 가진 오브젝트를 킬하는 클래스
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

            // UnityAction 함수 등록
            health.OnDie += OnDie;  // Health 스크립트에 써놓은 OnDie 유니티 함수에 등록    // null이면 안되기 때문에 방어코드가 좋은 것만은 아님
            health.OnDamaged += OnDamaged;

        }

        void OnDamaged(float damage, GameObject damageSource)
        {
            // TODO : 데미지 효과 구현
        }
        void OnDie()
        {
            // 킬
            Destroy(gameObject);
        }
    }
}

