using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// ���� ��ü ����
    /// </summary>
    public enum WeaponSwitchState
    {
        Up,
        Down,
        PutDownPrevious,
        PutUpNew,
    }
    /// <summary>
    /// �÷��̾ ���� ������� �����ϴ� Ŭ���� 
    /// </summary>
    public class PlayerWeaponsManager : MonoBehaviour
    {
        #region Variables
        // ���� ���� - ������ ������ �� ó�� �������� ���޵Ǵ� ���� ����Ʈ(�κ��丮)
        public List<WeaponController> startingWeapons = new List<WeaponController>();

        // ���� ����
        // ���⸦ �����ϴ� ������Ʈ
        public Transform weaponParentSocket;

        // �÷��̾ ���� �߿� ��� �ٴϴ� ���� ����Ʈ
        private WeaponController[] weaponSlots = new WeaponController[9];
        // ���� ����Ʈ(����)�� Ȱ��ȭ�� ���⸦ �����ϴ� �ε���
        public int ActiveWeaponIndex { get; private set; }

        // ���� ��ü
        public UnityAction<WeaponController> OnSwitchToWeapon;  // ���� ��ü�� ��ϵ� �Լ� ȣ��

        private WeaponSwitchState weaponSwitchState;        // ���� ��ü�� ����

        private PlayerInputHandler playerInputHandler;

        // ���� ��ü�� ���Ǵ� ���� ��ġ
        private Vector3 weaponMainLocalPosition;

        public Transform defalutWeaponPosition;
        public Transform downWeaponPosition;
        public Transform aimingWeaponPosition;

        private int weaponSwitchNewIndex;       // ���� �ٲ�� ���� �ε���

        private float weaponSwitchTimeStarted = 0f;
        [SerializeField] private float weaponSwitchDelay = 1f;

        // �� ����
        public bool IsPointingAtEnemy { get; private set; }         // �� ���� ����
        public Camera weaponCamera;                                 // weaponCamera���� Ray�� �� Ȯ��

        // ����
        // ī�޶�
        private PlayerCharacterController playerCharacterController;
        public float defaultFov = 60f;          // ī�޶� �⺻ FOV ��
        public float weaponFovMultiplier = 1f;       // FOV ���� ���

        public bool IsAiming {  get; private set; }                 // ���� ���� ����
        [SerializeField] private float aimingAnimationSpeed = 10f;  // ���� �̵�, FOV ���� �ӵ�

        private float weaponAimingLocalPosition;

        // ��鸲
        [SerializeField] private float bobFrequency = 10f;
        [SerializeField] private float bobSharpness = 10f;
        [SerializeField] private float defaultBobAmount = 0.05f;         // ���� ��鸲 ��
        [SerializeField] private float aimingBobAmount = 0.02f;          // ������ ��鸲 ��

        private float weaponBobFactor;          // ��鸲 ���
        private Vector3 lastCharacterPosition;  // ���� �����ӿ����� �̵��ӵ��� ���ϱ� ���� ����

        private Vector3 weaponBobLocalPosition; // ��鸲 �� ���� ��갪, �̵����� ������ 0

        // �ݵ�
        [SerializeField] private float recoilSharpness = 50f;       // �ڷ� �и��� �̵��ӵ�
        [SerializeField] private float maxRecoilDistance = 0.5f;    // �ݵ��� �ڷ� �и� �� �ִ� �ִ�Ÿ�
        private float recoilRepositionSharpness = 10f;              // ���ڸ��� ���ƿ��� �ӵ�
        private Vector3 accumulateRecoil;                           // �ݵ��� �ڷ� �и��� ��

        private Vector3 weaponRecoilLocalPosition;                  // �ݵ��� �̵��� ���� ��갪, �ݵ��� ���ڸ��� ���ƿ��� 0

        // ���� ���
        private bool isScopeOn = false;
        [SerializeField] private float distanceOnScope = 0.05f;

        public UnityAction OnScopedWeapon;                          // ���� ��� ���۽� ��ϵ� �Լ� ȣ��
        public UnityAction OffScopedWeapon;                          // ���� ��� ����� ��ϵ� �Լ� ȣ��
        #endregion

        private void Start()
        {
            // ����
            playerInputHandler = GetComponent<PlayerInputHandler>();
            playerCharacterController = GetComponent<PlayerCharacterController>();

            // �ʱ�ȭ
            ActiveWeaponIndex = -1;
            weaponSwitchState = WeaponSwitchState.Down;

            // ��Ƽ�� ���� show �Լ� ���
            OnSwitchToWeapon += OnWeaponSwitched;

            // ���� ��� �Լ� ���
            OnScopedWeapon += OnScope;
            OffScopedWeapon += OffScope;

            // Fov �ʱⰪ ����
            SetFov(defaultFov);

            // ���� ���� ���� ����
            foreach (var weapon in startingWeapons)
            {
                AddWeapon(weapon);
            }
            SwitchWeapon(true);
        }

        private void Update()
        {
            // ���� ��Ƽ�� ����
            WeaponController activeWeapon = GetActiveWeapon();

            if(weaponSwitchState == WeaponSwitchState.Up)
            {
                // ���� �Է°� ó��
                IsAiming = playerInputHandler.GetAimInputHeld();

                // ���� ��� ó��
                if (activeWeapon.shootType == WeaponShootType.Sniper)
                {
                    if (playerInputHandler.GetAimInputDown())
                    {
                        // ����
                        isScopeOn = true;
                        OnScopedWeapon?.Invoke();
                    }
                    if (playerInputHandler.GetAimInputUp())
                    {
                        // ����
                        OffScopedWeapon?.Invoke();
                    }
                }

                // �� ó��
                bool isFire = activeWeapon.HandleShootInputs(
                    playerInputHandler.GetFireInputDown(), playerInputHandler.GetFireInputHeld(), playerInputHandler.GetFireInputUp()
                    );

                if (isFire)
                {
                    // �ݵ� ȿ��
                    accumulateRecoil += Vector3.back * activeWeapon.recoilForce;
                    accumulateRecoil = Vector3.ClampMagnitude(accumulateRecoil, maxRecoilDistance); // �ݵ������� ���� �� ������ ���� �� �ȴ�
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

            // �� ����
            IsPointingAtEnemy = false;
            if (activeWeapon)
            {
                RaycastHit hit;
                if (Physics.Raycast(weaponCamera.transform.position, weaponCamera.transform.forward, out hit, 300f))
                {
                    // �ݶ��̴� üũ - �� �Ǻ�
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
            
            // ���� ���� ��ġ
            weaponParentSocket.localPosition = weaponMainLocalPosition + weaponBobLocalPosition + weaponRecoilLocalPosition;
        }

        // �ݵ�
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

        // ī�޶� FOV �� ����: ����, �ܾƿ�
        private void SetFov(float fov)
        {
            playerCharacterController.PlayerCamera.fieldOfView = fov;
            weaponCamera.fieldOfView = fov * weaponFovMultiplier;
        }

        // ���� ���ؿ� ���� ����: ���� ��ġ ����, FOV �� ����
        void UpdateWeaponAiming()
        {
            // ���⸦ ��� �������� ���� ����
            if(weaponSwitchState == WeaponSwitchState.Up)
            {
                WeaponController activeWeapon = GetActiveWeapon();

                if (IsAiming && activeWeapon)   // ���ؽ� : ����Ʈ -> Aiming ��ġ�� �̵�, fov: ����Ʈ -> aimZoomRatio
                {
                    weaponMainLocalPosition = Vector3.Lerp(weaponMainLocalPosition, aimingWeaponPosition.position + activeWeapon.aimOffset,
                        aimingAnimationSpeed * Time.deltaTime);

                    // ���� ��� ����
                    if (isScopeOn)
                    {
                        //weaponMainLocalPosition, ��ǥ���������� �Ÿ��� ���Ѵ�
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
                else            // ������ Ǯ���� ��: Aiming ��ġ -> ����Ʈ ��ġ�� �̵�
                {
                    weaponMainLocalPosition = Vector3.Lerp(weaponMainLocalPosition, defalutWeaponPosition.localPosition,
                        aimingAnimationSpeed * Time.deltaTime);
                    float fov = Mathf.Lerp(playerCharacterController.PlayerCamera.fieldOfView, defaultFov,
                        aimingAnimationSpeed * Time.deltaTime);
                    SetFov(fov);
                }
            }
        }

        // �̵��� ���� ���� ��鸲 �� ���ϱ�
        void UpdateWeaponBob()
        {
            // 
            if(Time.deltaTime > 0)
            {
                // �÷��̾ �� �����ӵ��� �̵��� �Ÿ�
                //playerCharacterController.transform.position - lastCharacterPosition
                // ���� �����ӿ��� �÷��̾� �̵� �ӵ�
                Vector3 playerCharacterVelocity =
                    (playerCharacterController.transform.position - lastCharacterPosition) / Time.deltaTime;

                float characterMovementFactor = 0f;
                if (playerCharacterController.IsGrounded)
                {
                    characterMovementFactor = Mathf.Clamp01(playerCharacterVelocity.magnitude /
                    (playerCharacterController.MaxSpeedOnGround * playerCharacterController.SprintSpeedModifier));
                }

                // �ӵ��� ���� ��鸲 ���
                weaponBobFactor = Mathf.Lerp(weaponBobFactor, characterMovementFactor, bobSharpness * Time.deltaTime);

                // ��鸲��(���ؽ�, ����)
                float bobAmount = IsAiming ? aimingBobAmount : defaultBobAmount;
                float frequency = bobFrequency;
                // �¿� ��鸲
                float vBobValue = Mathf.Sin(Time.time * frequency) * bobAmount * weaponBobFactor;
                // ���Ʒ� ��鸲 (�¿� ��鸲�� ����)
                float hBobValue = ((Mathf.Sin(Time.time * frequency) * 0.5f) + 0.5f) * bobAmount * weaponBobFactor;

                // ��鸲 ���� ������ ����
                weaponBobLocalPosition.x = hBobValue;
                weaponBobLocalPosition.y = Mathf.Abs(hBobValue);

                // �÷��̾��� ���� �������� ������ ��ġ�� ����
                lastCharacterPosition = playerCharacterController.transform.position;
            }
        }

        // ���¿� ���� ���� ����
        void UpdateWeaponSwitching()
        {
            // Lerp ����
            float switchingTimeFactor = 0f;
            if(weaponSwitchDelay == 0f)
            {
                switchingTimeFactor = 1f;
            }
            else
            {
                switchingTimeFactor = Mathf.Clamp01((Time.time - weaponSwitchTimeStarted) / weaponSwitchDelay);
            }

            // �����ð����� ���� ���� �ٲٱ�
            if(switchingTimeFactor >= 1f)
            {
                if(weaponSwitchState == WeaponSwitchState.PutDownPrevious)
                {
                    // ���繫�� false, ���ο� ���� true
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

            // �����ð����� ������ ��ġ �̵�
            if(weaponSwitchState == WeaponSwitchState.PutDownPrevious)
            {
                weaponMainLocalPosition = Vector3.Lerp(defalutWeaponPosition.localPosition, downWeaponPosition.localPosition, switchingTimeFactor);
            }
            else
            {
                weaponMainLocalPosition = Vector3.Lerp(downWeaponPosition.localPosition, defalutWeaponPosition.localPosition, switchingTimeFactor);
            }
        }

        // weaponSlots�� ���� ���������� ������ WeaponController ������Ʈ �߰�
        public bool AddWeapon(WeaponController weaponPrefab)
        {
            // �߰��ϴ� ���� ���� ���� üũ - �ߺ� �˻�
            if(HasWeapon(weaponPrefab) != null)
            {
                Debug.Log("Already had this waepon");
                return false;
            }

            for(int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] == null)
                {
                    WeaponController weaponInstance = Instantiate(weaponPrefab, weaponParentSocket);  // WeaponController�� ��ȯ��
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

        // �Ű������� ���� ����
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

        // ������ ���Կ� ���Ⱑ �ִ��� ����
        public WeaponController GetWeaponAtSlotIndex(int index)
        {
            if(index >= 0 && index < weaponSlots.Length)
            {
                return weaponSlots[index];
            }
            return null;
        }

        // 0~9�� ���� ���� �ִ� �� 0, 1, 2
        // ���� �ٲٱ�, ���� ��� �ִ� ���� false, ���ο� ���� true
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

            // ���� ��Ƽ���� ���� �ε����� ���� ��ü
            SwitchToWeaponIndex(newWeaponIndex);
        }

        private void SwitchToWeaponIndex(int newWeaponIndex)
        {
            // newWeaponIndex �� üũ
            if(newWeaponIndex >= 0 && newWeaponIndex != ActiveWeaponIndex)
            {
                weaponSwitchNewIndex = newWeaponIndex;
                weaponSwitchTimeStarted = Time.time;

                // ���� ��Ƽ���� ���Ⱑ �ִ���?
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

        // ���� �� �Ÿ�
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

