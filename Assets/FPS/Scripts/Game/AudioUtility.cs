using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// ����� �÷��� ���� ��� ����
    /// </summary>
    public class AudioUtility : MonoBehaviour
    {
        // ������ ��ġ�� ���ӿ�����Ʈ�� �����ϰ� AudioSource ������Ʈ�� �߰��ؼ� ������ Ŭ���� �÷����Ѵ�
        // Ŭ�� ���� �÷��̰� ������ �ڵ����� ų�Ѵ� - TimeSelfDestruct ������Ʈ �̿�
        public static void CreateSfx(AudioClip clip, Vector3 position, float spatialBlend, float rolloffDistanceMin = 1f)
        {
            GameObject impactSfxInstance = new GameObject();
            impactSfxInstance.transform.position = position;

            // audio clip play
            AudioSource source = impactSfxInstance.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = spatialBlend;
            source.minDistance = rolloffDistanceMin;
            source.Play();

            // ������Ʈ kill
            TimeSelfDestruct timeSelfDestruct = impactSfxInstance.AddComponent<TimeSelfDestruct>();
            timeSelfDestruct.lifeTime = clip.length;
        }
    }
}

