using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 크로스헤어를 그리기 위한 데이터
    /// </summary>
    [System.Serializable]
    public struct CrossHairData
    {
        public Sprite CrossHairSprite;
        public float CrossHairSize;
        public Color CrossHairColor;
    }

    /// <summary>
    /// 무기 슛 타입
    /// </summary>
    public enum WeaponShootType
    {
        Manual,
        Automatic,
        Charge,
        Sniper
    }

    /// <summary>
    /// 무기(총기)를 관리하는 클래스
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        #region Variables
        // 무기 활성화, 비활성화
        public GameObject weaponRoot;

        //
        public GameObject Owner { get; set; }           // 무기 주인
        public GameObject SourcePrefab { get; set; }    // 무기를 생성한 오리지널 프리팹
        public bool IsWeaponActive { get; private set; }// 무기 활성화 여부

        private AudioSource shootAudioSource;
        public AudioClip switchWeaponSfx;

        // shooting
        public WeaponShootType shootType;

        [SerializeField] private float maxAmmo = 8f;    // 장전할 수 있는 최대 총알 개수
        private float currentAmmo;

        [SerializeField] private float delayBetweenShots = 0.5f;    // 슛 딜레이
        private float lastTimeShot;                                 // 마지막으로 슛한 시간

        // Vfx, Sfx
        public Transform weaponMuzzle;                              // 총구 위치
        public GameObject muzzleFlashPrefab;                        // 총구 발사 이펙트
        public AudioClip shootSfx;                                  // 총 발사 사운드

        // CrossHair
        public CrossHairData crossHairDefalut;          // 기본, 평상시
        public CrossHairData crossHairTargetSight;      // 적을 포착했을 때, 타겟팅 되었을 때

        // 조준
        public float aimZoomRatio = 1f;                 // 조준시 줌인 설정값
        public Vector3 aimOffset;                       // 조준시 무기 위치 조정값

        // 반동
        public float recoilForce = 0.5f;

        // Projectile
        public ProjectileBase projectilePrefab;

        //
        public Vector3 MuzzleWorldVelocity { get; private set; }             // 현재 프레임에서의 총구 속도
        private Vector3 lastMuzzlePosition;
       

        [SerializeField] private int bulletsPershot = 1;                     // 한번 슛하는데 발사되는 탄환의 개수
        [SerializeField] private float bulletSpreadAngle = 0f;               // 불렛이 퍼져 나가는 각도

        // Charge : 발사 버튼을 누르고 있으면 발사체의 데미지, 속도가 일정값까지 커진다
        public float CurrentCharge { get; private set; }
        public bool IsCharging { get; private set; }

        [SerializeField] private float ammoUseOnStartCharge = 1f;    // 충전 시작 버튼을 누르기 위해 필요한 ammo량
        [SerializeField] private float ammoUsegeRateWhileCharging = 1f;  // 차지하고 있는동안 소비되는 ammo량
        private float maxChargeDuration = 2f;                            // 최대 충전 시간

        public float lastChargeTriggerTimeStamp;                         // 충전 시작 시간

        // Reload : 재장전
        [SerializeField] private float ammoReloadRate = 1f;              // 초당 재장전되는 양
        [SerializeField] private float ammoReloadDelay = 2f;             // 슛한 다음 ammoReloadDelay가 지난 후에 재장전
        
        [SerializeField] private bool automaticReload = true;                             // 자동, 수동 구분
        #endregion

        public float CurrentAmmoRatio => currentAmmo / maxAmmo;

        private void Awake()
        {
            // 참조
            shootAudioSource = this.GetComponent<AudioSource>();
        }

        private void Start()
        {
            // 초기화
            currentAmmo = maxAmmo;
            lastTimeShot = Time.time;
            lastMuzzlePosition = weaponMuzzle.position;
        }

        private void Update()
        {
            // 충전
            UpdateCharge();
            UpdateAmmo();

            // MuzzleWorldVelocity
            if(Time.deltaTime > 0f)
            {
                MuzzleWorldVelocity = (weaponMuzzle.position - lastMuzzlePosition) / Time.deltaTime;

                lastMuzzlePosition = weaponMuzzle.position;
            }
        }

        // Reload - Auto
        void UpdateAmmo()
        {
            if (automaticReload && currentAmmo < maxAmmo && IsCharging == false && lastTimeShot + ammoReloadDelay < Time.time)
            {
                currentAmmo += ammoReloadRate * Time.deltaTime;         // 초당 ammoReloadRate만큼 재장전
                currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
            }
           
        }

        // Reload - 수동
        public void Reload()
        {
            if(automaticReload || currentAmmo >= maxAmmo || IsCharging)
            {
                return;
            }

            currentAmmo = maxAmmo;
        }

        // 충전
        void UpdateCharge()
        {
            if (IsCharging)
            {
                if(CurrentCharge < 1f)
                {
                    // 현재 남아있는 충전량
                    float chargeLeft = 1f - CurrentCharge;
                        
                    float chargeAdd = 0f;           // 이번 프레임에 충전할 양
                    if(maxChargeDuration <= 0f)
                    {
                        chargeAdd = chargeLeft;     // 한번에 풀 충전
                    }
                    else
                    {
                        chargeAdd = (1f / maxChargeDuration) * Time.deltaTime;
                    }
                    chargeAdd = Mathf.Clamp(chargeAdd, 0f, chargeLeft);         // 남아있는 충전량보다 작아야 한다

                    // charAdd 만큼 Ammo 소비량을 구한다
                    float ammoThisChargeRequire = chargeAdd * ammoUsegeRateWhileCharging;
                    if(ammoThisChargeRequire <= currentAmmo)
                    {
                        UseAmmo(ammoThisChargeRequire);
                        CurrentCharge = Mathf.Clamp01(CurrentCharge + chargeAdd);
                    }
                }
            }
        }

        // 무기 활성화, 비활성화
        public void ShowWeapon(bool show)
        {
            weaponRoot.SetActive(show);
            
            // this 무기를 active 무기로 변경
            if(show == true && switchWeaponSfx != null)
            {
                // 무기 변경 효과음 플레이
                shootAudioSource.PlayOneShot(switchWeaponSfx);
            }
            IsWeaponActive = show;
        }

        // 키 입력에 따른 슛 타입 구현
        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp) // 누를 때, 누르고 있을 때, 뗄 때
        {
            switch (shootType)
            {
                case WeaponShootType.Manual:
                    if (inputDown)
                    {
                        return TryShoot();
                    }
                    break;
                case WeaponShootType.Automatic:
                    if (inputHeld)
                    {
                        return TryShoot();
                    }
                    break;
                case WeaponShootType.Charge:
                    if (inputHeld)
                    {
                        // 충전 시작
                        TryBeginCharge();
                    }
                    if (inputUp)
                    {
                        // 충전 끝
                        return TryReleaseCharge();
                    }
                    break;
                case WeaponShootType.Sniper:
                    if (inputDown)
                    {
                        return TryShoot();
                    }
                    break;
            }

            return false;
        }

        // 충전 시작
        void TryBeginCharge()
        {
            if(IsCharging == false && currentAmmo >= ammoUseOnStartCharge && (lastTimeShot + delayBetweenShots) < Time.time)
            {
                UseAmmo(ammoUseOnStartCharge);

                lastChargeTriggerTimeStamp = Time.time;
                IsCharging = true;
            }
        }

        // 충전 끝 - 발사
        bool TryReleaseCharge()
        {
            if (IsCharging)
            {
                // 슛
                HandleShoot();

                // 초기화
                CurrentCharge = 0f;
                IsCharging = false;
                return true;
            }

            return false;
        }

        void UseAmmo(float amount)
        {
            currentAmmo = Mathf.Clamp(currentAmmo - ammoUseOnStartCharge, 0f, maxAmmo);
            lastTimeShot = Time.time;
        }

        bool TryShoot()
        {
            //
            if(currentAmmo >= 1f && (lastTimeShot + delayBetweenShots) < Time.time)
            {
                currentAmmo -= 1f;

                HandleShoot();
                return true;
            }
            return false;
        }

        // 슛 연출   
        void HandleShoot()
        {
            // projectile 생성
            for(int i = 0; i < bulletsPershot; i++)
            {
                Vector3 shotDirection = GetShotDirectionWithinSpread(weaponMuzzle);
                ProjectileBase projectileInstance = Instantiate(projectilePrefab, weaponMuzzle.position, Quaternion.LookRotation(shotDirection));
                projectileInstance.Shoot(this);
            }
            
            // Vfx
            if (muzzleFlashPrefab)
            {
                GameObject effectGo = Instantiate(muzzleFlashPrefab, weaponMuzzle.position, weaponMuzzle.rotation, weaponMuzzle);
                Destroy(effectGo, 2f);
            }


            // Sfx
            if (shootSfx)
            {
                shootAudioSource.PlayOneShot(shootSfx);
            }

            // 슛한 시간 저장
            lastTimeShot = Time.time;

        }

        // projectile 날아가는 방향
        Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
        {
            float spreadAngleRatio = bulletSpreadAngle / 180f;
            return Vector3.Lerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);
        }
    }
}

