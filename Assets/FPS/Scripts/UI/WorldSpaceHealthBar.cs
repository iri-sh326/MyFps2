using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class WorldSpaceHealthBar : MonoBehaviour
    {
        #region Variables
        public Health health;
        public Image healthBarImage;

        public Transform healthBarPivot;

        [SerializeField] private bool hideFullHealthBar = true;
        #endregion

        private void Update()
        {
            healthBarImage.fillAmount = health.GetRatio();

            // UI가 플레이어를 바라보도록 한다
            healthBarPivot.LookAt(Camera.main.transform.position);

            // HP가 풀이면 healthBar를 숨긴다
            if (hideFullHealthBar)
            {
                healthBarPivot.gameObject.SetActive(healthBarImage.fillAmount != 1f);
            }

        }
    }
}

