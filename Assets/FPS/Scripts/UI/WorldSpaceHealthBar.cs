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

            // UI�� �÷��̾ �ٶ󺸵��� �Ѵ�
            healthBarPivot.LookAt(Camera.main.transform.position);

            // HP�� Ǯ�̸� healthBar�� �����
            if (hideFullHealthBar)
            {
                healthBarPivot.gameObject.SetActive(healthBarImage.fillAmount != 1f);
            }

        }
    }
}
