using System;
using UnityEngine;

namespace AudienceSDK.Sample
{
    public class FPSAudience : MonoBehaviour
    {
        public static FPSAudience instance = null;

        public static FPSAudience Instance
        {
            get
            {
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        private void Awake()
        {
            if (FPSAudience.Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            FPSAudience.Instance = this;
        }

        private void Start()
        {
            UnityEngine.Object.DontDestroyOnLoad(this.gameObject);
            CameraUtilities.PostCreateCamera = PostCreateCameraFunc;
            AudienceSDK.Audience.Initialize();
        }

        private void Update()
        {
        }

        private void OnApplicationQuit()
        {
            Debug.Log("OnApplicationQuit()");
            AudienceSDK.Audience.Deinitialize();
        }

        private static void PostCreateCameraFunc(AudienceCameraInstance instance)
        {

            instance.Instance = instance.GameObj.AddComponent<FPSAudienceCameraBehavior>();

            instance.Instance.Init(instance.Camera);
        }
    }
}
