using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Gameplay;
using Unity.FPS.Game;
using TMPro;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    /// <summary>
    /// WeaponController(����)�� Ammo
    /// </summary>
    public class AmmoCount : MonoBehaviour
    {
        #region Variables
        private PlayerWeaponsManager playerWeaponsManager;

        private WeaponController weaponController;
        private int weaponIndex;

        // UI
        public TextMeshProUGUI weaponIndexText;

        public Image ammoFillImage;                 // ammo rate�� ���� ������

        [SerializeField] private float ammoFillSharpness = 10f;      // ������ ä���(����) �ӵ�
        private float weaponSwitchSharpness = 10f;                   // ���� ��ü�� UI�� �ٲ�� �ӵ�

        public CanvasGroup canvasGroup;
        [SerializeField] [Range(0,1)] private float unSelectedOpacity = 0.5f;
        private Vector3 unSelectedScale = Vector3.one * 0.8f;

        // �������� �� ����

        #endregion

        // AmmoCount UI �� �ʱ�ȭ
        public void Initialize(WeaponController weapon, int _weaponIndex)
        {
            weaponController = weapon;
            weaponIndex = _weaponIndex;

            // ���� �ε���
            weaponIndexText.text = (weaponIndex + 1).ToString();

            //����
            playerWeaponsManager = GameObject.FindObjectOfType<PlayerWeaponsManager>();
        }

        private void Update()
        {
            float currentFillRate = weaponController.CurrentAmmoRatio;
            ammoFillImage.fillAmount = Mathf.Lerp(ammoFillImage.fillAmount, currentFillRate, Time.deltaTime);

            // ��Ƽ�� ���� ����
            bool isActiveWeapon = (weaponController == playerWeaponsManager.GetActiveWeapon());
            float currentOpacity = isActiveWeapon ? 1.0f : unSelectedOpacity;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, currentOpacity, weaponSwitchSharpness * Time.deltaTime);
            Vector3 currentScale = isActiveWeapon ? Vector3.one : unSelectedScale;
            transform.localScale = Vector3.Lerp(transform.localScale, currentScale, weaponSwitchSharpness * Time.deltaTime);
        }
    }
}

