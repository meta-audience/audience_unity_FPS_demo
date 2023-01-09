using System;
using UnityEngine;

namespace AudienceSDK.Sample
{
    public class FPSAudience : MonoBehaviour
    {
        public Action<bool> onAudienceInitStateChanged;

        public static FPSAudience instance = null;
        private bool audienceInited = false;
        // Developer Custom Assign
        [Tooltip("audience Viewer Camera will continually trace this pivot position.")]
        public GameObject ViewerCameraPivot;
        [Tooltip("Colliders with trigger on, use to generate avatar.")]
        public Transform AvatarGenrateAreasGroupRootPrefab;
        [Tooltip("Make gernerated avatar look at this position, suppose avatar appear in streamer view.")]
        public string PlayerTag;
        public string PlayerSceneName;
        //public Transform GenerateAreaFollowParent;
        private bool unloadedMainScene = false;
        private bool needUpdateParent = false;


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

        public bool AudienceInited
        {
            get
            {
                return this.audienceInited;
            }

            private set
            {
                this.audienceInited = value;
                this.onAudienceInitStateChanged?.Invoke(this.audienceInited);
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
            //init in menu, make generate area under this gameObj
            AudienceSDK.Audience.Context.EmojiAvatarManager.InitiateAvatarGenerateArea(AvatarGenrateAreasGroupRootPrefab, null);
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            this.AudienceInited = true;
        }

        private void Update()
         {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == PlayerSceneName && !unloadedMainScene && needUpdateParent)
            {
                //Please ensure player only one
                GameObject[] player = GameObject.FindGameObjectsWithTag("PlayerRoot");
                AudienceSDK.Audience.Context.EmojiAvatarManager.UpdateAvatarGenerateAreaParent(player[0].transform);
                needUpdateParent = false;
            }
        }
        // OnsceneLoaded -> player obj instantiate -> start
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene current, UnityEngine.SceneManagement.LoadSceneMode lm)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == PlayerSceneName)
            {
                needUpdateParent = true;
                unloadedMainScene = false;
            }
        }
        public void OnSceneUnloaded()
        {
            //Debug.Log("###  --- Now unload" + current.name);
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == PlayerSceneName)
            {
                unloadedMainScene = true;
                AudienceSDK.Audience.Context.EmojiAvatarManager.UpdateAvatarGenerateAreaParent(this.transform);
                
            }
        }

        private void OnApplicationQuit()
        {
            this.AudienceInited = false;
            Debug.Log("OnApplicationQuit()");
            AudienceSDK.Audience.Context.Stop();
            NativeMethods.DeInit();
        }

        private static void PostCreateCameraFunc(AudienceCameraInstance instance)
        {

            instance.Instance = instance.GameObj.AddComponent<FPSAudienceCameraBehavior>();

            instance.Instance.Init(instance.Camera);
        }
    }
}
