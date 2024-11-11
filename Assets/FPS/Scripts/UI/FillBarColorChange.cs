using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    /// <summary>
    /// ���������� ��������, ��׶���� ���� ����
    /// </summary>
    public class FillBarColorChange : MonoBehaviour
    {
        #region Variables
        public Image foregroundImage;
        public Color defalutForegroundColor;        // �������� �⺻ �÷�
        public Color flashForeGroundColorFull;      // �������� Ǯ�� ���� ���� �� �÷��� ȿ��

        public Image backgroundImage;
        public Color defaultBackgroundColor;        // ��׶��� �⺻ �÷�
        public Color flashBackgroundColor;     // ��׶��� ���������� 0�϶� �÷���

        private float fullValue = 1f;               // �������� Ǯ�϶��� ��
        private float emptyValue = 0f;              // �������� ������ ��

        private float colorChangeSharpness = 5f;    // �÷� ���� �ӵ�
        private float prevousValue;                 // �������� Ǯ�� ���� ������ 
        #endregion

        // �� ���� ���� �� �ʱ�ȭ
        public void Initialize(float fullValueRatio, float emptyValueRatio)
        {
            fullValue = fullValueRatio;
            emptyValue = emptyValueRatio;

            prevousValue = fullValue;
        }

        public void UpdateVisual(float currentRatio, Color flashBackgroundColorFull, Color flashBackgroundColorEmpty)
        {
            // �������� Ǯ�� ���� ����
            if(currentRatio == fullValue && currentRatio != prevousValue)
            {
                foregroundImage.color = flashBackgroundColorFull;
            }
            else if(currentRatio < emptyValue)
            {
                backgroundImage.color = flashBackgroundColorEmpty;
            }
            else
            {
                foregroundImage.color = Color.Lerp(foregroundImage.color, defalutForegroundColor, colorChangeSharpness * Time.deltaTime);
                backgroundImage.color = Color.Lerp(backgroundImage.color, defaultBackgroundColor, colorChangeSharpness * Time.deltaTime);
            }
           

            prevousValue = currentRatio;
        }
    }
}

