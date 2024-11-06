using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.UI;
using Unity.FPS.Gameplay;

namespace Unity.FPS.UI
{
    public class CrosshairManager : MonoBehaviour
    {
        #region Variables
        public Image crosshairImage;            // 크로스헤어 UI 이미지
        public Sprite nullCrosshairSprite;      // 액티브한 무기가 없을 때

        private RectTransform crosshairRectTransform;

        private CrossHairData crosshairDefault;         // 평상시, 기본
        private CrossHairData crosshairTarget;          // 타겟팅 되었을 때

        private CrossHairData crosshairCurrent;         // 실질적으로 그리는 크로스헤어
        [SerializeField] private float crosshairUpdateSharpness = 5.0f;  // Lerp 변수

        private PlayerWeaponsManager weaponsManager;

        private bool wasPointingAtEnemy;
        #endregion

        private void Start()
        {
            // 참조
            weaponsManager = GameObject.FindObjectOfType<PlayerWeaponsManager>();
            // 강제로 호출
            OnWeaponChanged(weaponsManager.GetActiveWeapon());


            weaponsManager.OnSwitchToWeapon += OnWeaponChanged;
        }

        private void Update()
        {
            UpdateCrosshairPointingAtEnemy(false);

            wasPointingAtEnemy = weaponsManager.IsPointingAtEnemy;
        }

        // 크로스헤어 그리기
        void UpdateCrosshairPointingAtEnemy(bool force)
        {
            if (crosshairDefault.CrossHairSprite == null)
                return;

            // 평상시? 타겟팅?
            if ((force || wasPointingAtEnemy == false) && weaponsManager.IsPointingAtEnemy == true)    // 적을 포착하는 순간
            {
                crosshairCurrent = crosshairTarget;
                crosshairImage.sprite = crosshairCurrent.CrossHairSprite;
                crosshairRectTransform.sizeDelta = crosshairCurrent.CrossHairSize * Vector2.one;
            }
            else if ((force || wasPointingAtEnemy == true) && weaponsManager.IsPointingAtEnemy == false)   // 적을 놓치는 순간
            {
                crosshairCurrent = crosshairDefault;
                crosshairImage.sprite = crosshairDefault.CrossHairSprite;
                crosshairRectTransform.sizeDelta = crosshairCurrent.CrossHairSize * Vector2.one;
            }
           
            crosshairCurrent = crosshairDefault;
            //crosshairCurrent = crosshairDefault;

            crosshairImage.sprite = crosshairDefault.CrossHairSprite;

            crosshairImage.color = Color.Lerp(crosshairImage.color, crosshairCurrent.CrossHairColor, crosshairUpdateSharpness * Time.deltaTime);
            crosshairRectTransform.sizeDelta = Mathf.Lerp(crosshairRectTransform.sizeDelta.x, crosshairCurrent.CrossHairSize, crosshairUpdateSharpness * Time.deltaTime)
                * Vector2.one;
        }

        // 무기가 바뀔때마다 crosshairImage를 각각의 무기 Crosshair 이미지로 바꾸기
        void OnWeaponChanged(WeaponController newWeapon)
        {
            if(newWeapon != null)
            {
                crosshairImage.enabled = true;
                crosshairRectTransform = crosshairImage.GetComponent<RectTransform>();
                // 액티브 무기의 크로스헤어 정보 가져오기
                crosshairDefault = newWeapon.crossHairDefalut;
                crosshairTarget = newWeapon.crossHairTargetSight;

                //crosshairImage.sprite = newWeapon.crossHairDefalut.CrossHairSprite;

            }
            else
            {
                if (nullCrosshairSprite)
                {
                    crosshairImage.sprite = nullCrosshairSprite;
                }
                else
                {
                    crosshairImage.enabled = false;
                }

            }

        }
    }
}

