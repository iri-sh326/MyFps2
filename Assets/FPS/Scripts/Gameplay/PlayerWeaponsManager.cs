using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// 무기 교체 상태
    /// </summary>
    public enum WeaponSwitchState
    {
        Up,
        Down,
        PutDownPrevious,
        PutUpNew,
    }
    /// <summary>
    /// 플레이어가 가진 무기들을 관리하는 클래스 
    /// </summary>
    public class PlayerWeaponsManager : MonoBehaviour
    {
        #region Variables
        // 무기 지급 - 게임을 시작할 때 처음 유저에게 지급되는 무기 리스트(인벤토리)
        public List<WeaponController> startingWeapons = new List<WeaponController>();

        // 무기 장착
        // 무기를 장착하는 오브젝트
        public Transform weaponParentSocket;

        // 플레이어가 게임 중에 들고 다니는 무기 리스트
        private WeaponController[] weaponSlots = new WeaponController[9];
        // 무기 리스트(슬롯)중 활성화된 무기를 관리하는 인덱스
        public int ActiveWeaponIndex { get; private set; }

        // 무기 교체
        public UnityAction<WeaponController> OnSwitchToWeapon;  // 무기 교체시 등록된 함수 호출

        private WeaponSwitchState weaponSwitchState;        // 무기 교체시 상태

        private PlayerInputHandler playerInputHandler;

        // 무기 교체시 계산되는 최종 위치
        private Vector3 weaponMainLocalPosition;

        public Transform defalutWeaponPosition;
        public Transform downWeaponPosition;
        public Transform aimingWeaponPosition;

        private int weaponSwitchNewIndex;       // 새로 바뀌는 무기 인덱스

        private float weaponSwitchTimeStarted = 0f;
        [SerializeField] private float weaponSwitchDelay = 1f;

        // 적 포착
        public bool IsPointingAtEnemy { get; private set; }         // 적 포작 여부
        public Camera weaponCamera;                                 // weaponCamera에서 Ray로 적 확인

        // 조준
        // 카메라
        private PlayerCharacterController playerCharacterController;
        public float defaultFov = 60f;          // 카메라 기본 FOV 값
        public float weaponFovMultiplier = 1f;       // FOV 연산 계수

        public bool IsAiming {  get; private set; }                 // 무기 조준 여부
        [SerializeField] private float aimingAnimationSpeed = 10f;  // 무기 이동, FOV 연출 속도

        private float weaponAimingLocalPosition;

        // 흔들림
        [SerializeField] private float bobFrequency = 10f;
        [SerializeField] private float bobSharpness = 10f;
        [SerializeField] private float defaultBobAmount = 0.05f;         // 평상시 흔들림 량
        [SerializeField] private float aimingBobAmount = 0.02f;          // 조준중 흔들림 량

        private float weaponBobFactor;          // 흔들림 계수
        private Vector3 lastCharacterPosition;  // 현재 프레임에서의 이동속도를 구하기 위한 변수

        private Vector3 weaponBobLocalPosition; // 흔들림 량 최종 계산값, 이동하지 않으면 0

        // 반동
        [SerializeField] private float recoilSharpness = 50f;       // 뒤로 밀리는 이동속도
        [SerializeField] private float maxRecoilDistance = 0.5f;    // 반동시 뒤로 밀릴 수 있는 최대거리
        private float recoilRepositionSharpness = 10f;              // 제자리로 돌아오는 속도
        private Vector3 accumulateRecoil;                           // 반동시 뒤로 밀리는 양

        private Vector3 weaponRecoilLocalPosition;                  // 반동시 이동한 최종 계산값, 반동후 제자리에 돌아오면 0

        // 저격 모드
        private bool isScopeOn = false;
        [SerializeField] private float distanceOnScope = 0.05f;

        public UnityAction OnScopedWeapon;                          // 저격 모드 시작시 등록된 함수 호출
        public UnityAction OffScopedWeapon;                          // 저격 모드 종료시 등록된 함수 호출
        #endregion

        private void Start()
        {
            // 참조
            playerInputHandler = GetComponent<PlayerInputHandler>();
            playerCharacterController = GetComponent<PlayerCharacterController>();

            // 초기화
            ActiveWeaponIndex = -1;
            weaponSwitchState = WeaponSwitchState.Down;

            // 액티브 무기 show 함수 등록
            OnSwitchToWeapon += OnWeaponSwitched;

            // 저격 모드 함수 등록
            OnScopedWeapon += OnScope;
            OffScopedWeapon += OffScope;

            // Fov 초기값 설정
            SetFov(defaultFov);

            // 지급 받은 무기 장착
            foreach (var weapon in startingWeapons)
            {
                AddWeapon(weapon);
            }
            SwitchWeapon(true);
        }

        private void Update()
        {
            // 현재 액티브 무기
            WeaponController activeWeapon = GetActiveWeapon();

            if(weaponSwitchState == WeaponSwitchState.Up)
            {
                // 조준 입력값 처리
                IsAiming = playerInputHandler.GetAimInputHeld();

                // 저격 모드 처리
                if (activeWeapon.shootType == WeaponShootType.Sniper)
                {
                    if (playerInputHandler.GetAimInputDown())
                    {
                        // 시작
                        isScopeOn = true;
                        OnScopedWeapon?.Invoke();
                    }
                    if (playerInputHandler.GetAimInputUp())
                    {
                        // 종료
                        OffScopedWeapon?.Invoke();
                    }
                }

                // 슛 처리
                bool isFire = activeWeapon.HandleShootInputs(
                    playerInputHandler.GetFireInputDown(), playerInputHandler.GetFireInputHeld(), playerInputHandler.GetFireInputUp()
                    );

                if (isFire)
                {
                    // 반동 효과
                    accumulateRecoil += Vector3.back * activeWeapon.recoilForce;
                    accumulateRecoil = Vector3.ClampMagnitude(accumulateRecoil, maxRecoilDistance); // 반동때문에 총이 몸 속으로 들어가면 안 된다
                }
            }

            if (!IsAiming && (weaponSwitchState == WeaponSwitchState.Up || weaponSwitchState == WeaponSwitchState.Down))
            {
                int switchWeaponInput = playerInputHandler.GetSwitchWeaponInput();
                if (switchWeaponInput != 0)
                {
                    bool switchUp = switchWeaponInput > 0;
                    SwitchWeapon(switchUp);
                }
            }

            // 적 포착
            IsPointingAtEnemy = false;
            if (activeWeapon)
            {
                RaycastHit hit;
                if (Physics.Raycast(weaponCamera.transform.position, weaponCamera.transform.forward, out hit, 300f))
                {
                    // 콜라이더 체크 - 적 판별
                    Health health = hit.collider.GetComponent<Health>();
                    if (health)
                    {
                        IsPointingAtEnemy = true;
                    }
                }
            }

        }

        private void LateUpdate()
        {
            UpdateWeaponBob();
            UpdateWeaponRecoil();
            UpdateWeaponAiming();
            UpdateWeaponSwitching();
            
            // 무기 최종 위치
            weaponParentSocket.localPosition = weaponMainLocalPosition + weaponBobLocalPosition + weaponRecoilLocalPosition;
        }

        // 반동
        void UpdateWeaponRecoil()
        {
            if(weaponRecoilLocalPosition.z >= accumulateRecoil.z * 0.99f)
            {
                weaponRecoilLocalPosition = Vector3.Lerp(weaponRecoilLocalPosition, accumulateRecoil, recoilSharpness * Time.deltaTime);

            }
            else
            {
                weaponRecoilLocalPosition = Vector3.Lerp(weaponRecoilLocalPosition, Vector3.zero, recoilRepositionSharpness * Time.deltaTime);
                accumulateRecoil = weaponRecoilLocalPosition;
            }
        }

        // 카메라 FOV 값 셋팅: 줌인, 줌아웃
        private void SetFov(float fov)
        {
            playerCharacterController.PlayerCamera.fieldOfView = fov;
            weaponCamera.fieldOfView = fov * weaponFovMultiplier;
        }

        // 무기 조준에 따른 연출: 무기 위치 조정, FOV 값 조정
        void UpdateWeaponAiming()
        {
            // 무기를 들고 있을때만 조준 가능
            if(weaponSwitchState == WeaponSwitchState.Up)
            {
                WeaponController activeWeapon = GetActiveWeapon();

                if (IsAiming && activeWeapon)   // 조준시 : 디폴트 -> Aiming 위치로 이동, fov: 디폴트 -> aimZoomRatio
                {
                    weaponMainLocalPosition = Vector3.Lerp(weaponMainLocalPosition, aimingWeaponPosition.position + activeWeapon.aimOffset,
                        aimingAnimationSpeed * Time.deltaTime);

                    // 저격 모드 시작
                    if (isScopeOn)
                    {
                        //weaponMainLocalPosition, 목표지점까지의 거리를 구한다
                        float dist = Vector3.Distance(weaponMainLocalPosition, aimingWeaponPosition.localPosition);
                        if(dist < distanceOnScope)
                        {
                            OnScopedWeapon?.Invoke();
                            isScopeOn = false;
                        }
                    }

                    float fov = Mathf.Lerp(playerCharacterController.PlayerCamera.fieldOfView, activeWeapon.aimZoomRatio * defaultFov,
                        aimingAnimationSpeed * Time.deltaTime);
                    SetFov(fov);
                }
                else            // 조준이 풀렸을 때: Aiming 위치 -> 디폴트 위치로 이동
                {
                    weaponMainLocalPosition = Vector3.Lerp(weaponMainLocalPosition, defalutWeaponPosition.localPosition,
                        aimingAnimationSpeed * Time.deltaTime);
                    float fov = Mathf.Lerp(playerCharacterController.PlayerCamera.fieldOfView, defaultFov,
                        aimingAnimationSpeed * Time.deltaTime);
                    SetFov(fov);
                }
            }
        }

        // 이동에 의한 무기 흔들림 값 구하기
        void UpdateWeaponBob()
        {
            // 
            if(Time.deltaTime > 0)
            {
                // 플레이어가 한 프레임동안 이동한 거리
                //playerCharacterController.transform.position - lastCharacterPosition
                // 현재 프레임에서 플레이어 이동 속도
                Vector3 playerCharacterVelocity =
                    (playerCharacterController.transform.position - lastCharacterPosition) / Time.deltaTime;

                float characterMovementFactor = 0f;
                if (playerCharacterController.IsGrounded)
                {
                    characterMovementFactor = Mathf.Clamp01(playerCharacterVelocity.magnitude /
                    (playerCharacterController.MaxSpeedOnGround * playerCharacterController.SprintSpeedModifier));
                }

                // 속도에 의한 흔들림 계수
                weaponBobFactor = Mathf.Lerp(weaponBobFactor, characterMovementFactor, bobSharpness * Time.deltaTime);

                // 흔들림량(조준시, 평상시)
                float bobAmount = IsAiming ? aimingBobAmount : defaultBobAmount;
                float frequency = bobFrequency;
                // 좌우 흔들림
                float vBobValue = Mathf.Sin(Time.time * frequency) * bobAmount * weaponBobFactor;
                // 위아래 흔들림 (좌우 흔들림의 절반)
                float hBobValue = ((Mathf.Sin(Time.time * frequency) * 0.5f) + 0.5f) * bobAmount * weaponBobFactor;

                // 흔들림 최종 변수에 적용
                weaponBobLocalPosition.x = hBobValue;
                weaponBobLocalPosition.y = Mathf.Abs(hBobValue);

                // 플레이어의 현재 프레임의 마지막 위치를 저장
                lastCharacterPosition = playerCharacterController.transform.position;
            }
        }

        // 상태에 따른 무기 연출
        void UpdateWeaponSwitching()
        {
            // Lerp 변수
            float switchingTimeFactor = 0f;
            if(weaponSwitchDelay == 0f)
            {
                switchingTimeFactor = 1f;
            }
            else
            {
                switchingTimeFactor = Mathf.Clamp01((Time.time - weaponSwitchTimeStarted) / weaponSwitchDelay);
            }

            // 지연시간이후 무기 상태 바꾸기
            if(switchingTimeFactor >= 1f)
            {
                if(weaponSwitchState == WeaponSwitchState.PutDownPrevious)
                {
                    // 현재무기 false, 새로운 무기 true
                    WeaponController oldWeapon = GetActiveWeapon();
                    if(oldWeapon != null)
                    {
                        oldWeapon.ShowWeapon(false);
                    }

                    ActiveWeaponIndex = weaponSwitchNewIndex;
                    WeaponController newWeapon = GetActiveWeapon();
                    OnSwitchToWeapon?.Invoke(newWeapon);

                    switchingTimeFactor = 0f;
                    if (newWeapon != null)
                    {
                        weaponSwitchTimeStarted = Time.time;
                        weaponSwitchState = WeaponSwitchState.PutUpNew;
                    }
                    else
                    {
                        weaponSwitchState = WeaponSwitchState.Down;
                    }
                }
                else if(weaponSwitchState == WeaponSwitchState.PutUpNew)
                {
                    weaponSwitchState = WeaponSwitchState.Up;
                }
            }

            // 지연시간동안 무기의 위치 이동
            if(weaponSwitchState == WeaponSwitchState.PutDownPrevious)
            {
                weaponMainLocalPosition = Vector3.Lerp(defalutWeaponPosition.localPosition, downWeaponPosition.localPosition, switchingTimeFactor);
            }
            else
            {
                weaponMainLocalPosition = Vector3.Lerp(downWeaponPosition.localPosition, defalutWeaponPosition.localPosition, switchingTimeFactor);
            }
        }

        // weaponSlots에 무기 프리팹으로 생성한 WeaponController 오브젝트 추가
        public bool AddWeapon(WeaponController weaponPrefab)
        {
            // 추가하는 무기 소지 여부 체크 - 중복 검사
            if(HasWeapon(weaponPrefab) != null)
            {
                Debug.Log("Already had this waepon");
                return false;
            }

            for(int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] == null)
                {
                    WeaponController weaponInstance = Instantiate(weaponPrefab, weaponParentSocket);  // WeaponController로 반환함
                    weaponInstance.transform.localPosition = Vector3.zero;
                    weaponInstance.transform.localRotation = Quaternion.identity;

                    weaponInstance.Owner = this.gameObject;
                    weaponInstance.SourcePrefab = weaponPrefab.gameObject;
                    weaponInstance.ShowWeapon(false);

                    weaponSlots[i] = weaponInstance;

                    return true;
                }
            }
            Debug.Log("weaponSlots full");
            return true;

        }

        // 매개변수로 들어온 무기
        private WeaponController HasWeapon(WeaponController weaponPrefab)
        {
            for(int i = 0; i < weaponSlots.Length; i++)
            {
                if(weaponSlots[i] != null && weaponSlots[i].SourcePrefab == weaponPrefab)
                {
                    return weaponSlots[i];
                }
            }

            return null;
        }

        public WeaponController GetActiveWeapon()
        {
            return GetWeaponAtSlotIndex(ActiveWeaponIndex);
        }

        // 지정된 슬롯에 무기가 있는지 여부
        public WeaponController GetWeaponAtSlotIndex(int index)
        {
            if(index >= 0 && index < weaponSlots.Length)
            {
                return weaponSlots[index];
            }
            return null;
        }

        // 0~9번 현재 갖고 있는 것 0, 1, 2
        // 무기 바꾸기, 현재 들고 있는 무기 false, 새로운 무기 true
        public void SwitchWeapon(bool ascendingOrder)
        {
            int newWeaponIndex = -1;
            int closestSlotDistance = weaponSlots.Length;
            for(int i = 0; i < weaponSlots.Length; i++)
            {
                if(i != ActiveWeaponIndex && GetWeaponAtSlotIndex(i) != null)
                {
                    int distanceToActiveIndex = GetDistanceBetweenWeaponSlot(ActiveWeaponIndex, i, ascendingOrder);
                    if(distanceToActiveIndex < closestSlotDistance)
                    {
                        closestSlotDistance = distanceToActiveIndex;
                        newWeaponIndex = i;
                    }
                }
            }

            // 새로 액티브할 무기 인덱스로 무기 교체
            SwitchToWeaponIndex(newWeaponIndex);
        }

        private void SwitchToWeaponIndex(int newWeaponIndex)
        {
            // newWeaponIndex 값 체크
            if(newWeaponIndex >= 0 && newWeaponIndex != ActiveWeaponIndex)
            {
                weaponSwitchNewIndex = newWeaponIndex;
                weaponSwitchTimeStarted = Time.time;

                // 현재 액티브한 무기가 있느냐?
                if(GetActiveWeapon() == null)
                {
                    weaponMainLocalPosition = downWeaponPosition.position;
                    weaponSwitchState = WeaponSwitchState.PutUpNew;
                    ActiveWeaponIndex = newWeaponIndex;

                    WeaponController weaponController = GetWeaponAtSlotIndex(newWeaponIndex);
                    OnSwitchToWeapon?.Invoke(weaponController);
                }
                else
                {
                    weaponSwitchState = WeaponSwitchState.PutDownPrevious;
                }
            }
        }

        // 슬롯 간 거리
        private int GetDistanceBetweenWeaponSlot(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
        {
            int distanceBetweenSlots = 0;

            if (ascendingOrder)
            {
                distanceBetweenSlots = toSlotIndex - fromSlotIndex;
            }
            else
            {
                distanceBetweenSlots = fromSlotIndex - toSlotIndex;
            }

            if(distanceBetweenSlots < 0)
            {
                distanceBetweenSlots = distanceBetweenSlots + weaponSlots.Length;
            }

            return distanceBetweenSlots;
        }

        void OnWeaponSwitched(WeaponController newWeapon)
        {
            if(newWeapon != null)
            {
                newWeapon.ShowWeapon(true);
            }
        }

        void OnScope()
        {
            weaponCamera.enabled = false;
        }

        void OffScope()
        {
            weaponCamera.enabled = true;
        }
    }
}

