using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    /// <summary>
    /// 게이지바의 게이지색, 백그라운드색 변경 구현
    /// </summary>
    public class FillBarColorChange : MonoBehaviour
    {
        #region Variables
        public Image foregroundImage;
        public Color defalutForegroundColor;        // 게이지의 기본 컬러
        public Color flashForeGroundColorFull;      // 게이지가 풀로 차는 순간 색 플래시 효과

        public Image backgroundImage;
        public Color defaultBackgroundColor;        // 백그라운드 기본 컬러
        public Color flashBackgroundColor;     // 백그라운드 게이지값이 0일때 컬러값

        private float fullValue = 1f;               // 게이지가 풀일때의 값
        private float emptyValue = 0f;              // 게이지가 빌때의 값

        private float colorChangeSharpness = 5f;    // 컬러 변경 속도
        private float prevousValue;                 // 게이지가 풀로 차는 순간을 
        #endregion

        // 색 변경 관련 값 초기화
        public void Initialize(float fullValueRatio, float emptyValueRatio)
        {
            fullValue = fullValueRatio;
            emptyValue = emptyValueRatio;

            prevousValue = fullValue;
        }

        public void UpdateVisual(float currentRatio, Color flashBackgroundColorFull, Color flashBackgroundColorEmpty)
        {
            // 게이지가 풀로 차는 순간
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

