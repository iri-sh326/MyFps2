using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Sample
{
    public class MaterialTest : MonoBehaviour
    {
        #region Variables
        private Renderer renderer;
        private MaterialPropertyBlock materialPropertyBlock;
        #endregion
        // Start is called before the first frame update
        void Start()
        {
            // ����
            renderer = GetComponent<Renderer>();

            // ���׸��� �÷� �ٲٱ�
            renderer.material.SetColor("_BaseColor", Color.red);
            //renderer.sharedMaterial.SetColor("_BaseColor", Color.red);

            //
            materialPropertyBlock = new MaterialPropertyBlock();
            materialPropertyBlock.SetColor("_BaseColor", Color.red);
            renderer.SetPropertyBlock(materialPropertyBlock);

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
