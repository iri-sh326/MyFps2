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

        private int weaponSwitchNewIndex;       // ���� �ٲ�� ���� �ε���
        private float weaponSwitchTimeStarted = 0f;
        [SerializeField] private float weaponSwitchDelay = 1f;
        #endregion

        private void Start()
        {
            // ����
            playerInputHandler = GetComponent<PlayerInputHandler>();

            // �ʱ�ȭ
            ActiveWeaponIndex = -1;
            weaponSwitchState = WeaponSwitchState.Down;

            // �̺�Ʈ �Լ� ���
            OnSwitchToWeapon += OnWeaponSwitched;

            // ���� ���� ���� ����
            foreach(var weapon in startingWeapons)
            {
                AddWeapon(weapon);
            }
        }

        private void Update()
        {
            if(weaponSwitchState == WeaponSwitchState.Up || weaponSwitchState == WeaponSwitchState.Down)
            {
                int switchWeaponInput = playerInputHandler.GetSwitchWeaponInput();
                if (switchWeaponInput != 0)
                {
                    bool switchUp = switchWeaponInput > 0;
                    SwitchWeapon(switchUp);
                }
            }

        }

        private void LateUpdate()
        {
            UpdateWeaponSwitching();

            // ���� ���� ��ġ
            weaponParentSocket.localPosition = weaponMainLocalPosition;
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
                    newWeapon.ShowWeapon(true);

                    switchingTimeFactor = 0f;
                    if (newWeapon != null)
                    {
                        weaponSwitchTimeStarted = Time.time;
                        weaponSwitchState = WeaponSwitchState.PutDownPrevious;
                    }
                    else
                    {

                    }
                }
                else if(weaponSwitchState == WeaponSwitchState.PutUpNew)
                {

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

            for(int i =0; i < weaponSlots.Length; i++)
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
    }
}

