using AudienceSDK;
using Newtonsoft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Audience
{
    public class Audience : MonoBehaviour
    {
        private static Audience instance = null;
        private AudienceConfigData configData;
        public GameObject ViewerCameraPivot;

        public List<NativeSceneSummaryData> CachedProfiles { get; set; }

        private void Awake()
        {
            if (Audience.instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Audience.instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("Audience Start");
            UnityEngine.Object.DontDestroyOnLoad(this.gameObject);

            CameraUtilities.PostCreateCamera = PostCreateCameraFunc;
            //Application.targetFrameRate = 120;
            Debug.Log("NativeMethods.Init");
            NativeMethods.Init();
            Debug.Log("Audience.Initialize();");
            AudienceSDK.Audience.Initialize();
            this.LoadConfig();
            AudienceSDK.Audience.Context.SetCurrentCultureName("zh-tw");

            AudienceSDK.Audience.Context.RefreshSceneListCompleted += OnRefreshProfileListCompleted;
        }

        void OnDestroy()
        {
            Debug.Log("Audience OnDestroy");
        }

        public void OnApplicationQuit()
        {
            Debug.Log("OnApplicationQuit");
            AudienceSDK.Audience.Context.Stop();
            AudienceSDK.Audience.Context.RefreshSceneListCompleted -= OnRefreshProfileListCompleted;
            Debug.Log("NativeMethods.Stop");
            NativeMethods.Stop();
            Debug.Log("NativeMethods.DeInit");
            NativeMethods.DeInit();

        }
        void OnRefreshProfileListCompleted(List<NativeSceneSummaryData> sceneList)
        {
            Debug.LogFormat("RegisterRefreshSceneListCompleted: size={0}", sceneList.Count);
            this.CachedProfiles = sceneList;
        }

        // Update is called once per frame
        void Update()
        {

            if (true)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    Login();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    Refresh();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    LoadScene();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    this.StartCoroutine(StartStreamCoroutine());
                }
                else if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    this.StartCoroutine(StopStreamCoroutine());
                }
                else if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    UnloadScene();
                }
            }
        }

        public void Login()
        {
            var error_code = AudienceSDK.Audience.Context.Login(this.configData.account, this.configData.password);
            Debug.LogFormat("AudienceSDK.Login ({0}): {1}", this.configData.account, error_code);
        }

        public void Logout()
        {
            var error_code = AudienceSDK.Audience.Context.Logout();
            Debug.LogFormat("AudienceSDK.Logout: {0}", error_code);
        }

        public void Refresh()
        {
            var error_code = AudienceSDK.Audience.Context.RefreshSceneList();
            Debug.LogFormat("AudienceSDK.RefreshProfileList: {0}", error_code);
        }

        public void LoadScene()
        {
            Debug.Log(this.configData.sceneName);
            var sceneIndex = this.CachedProfiles.FindIndex(x => x.SceneName == configData.sceneName);
            Debug.Log(sceneIndex);
            if (sceneIndex >= 0)
            {
                var error_code = AudienceSDK.Audience.Context.LoadScene(this.CachedProfiles[sceneIndex].SceneId);
                Debug.LogFormat("AudienceSDK.LoadProfile: {0}", error_code);
            }
            else
            {
                Debug.LogFormat("Scene:{0} not found.", configData.sceneName);
            }
        }

        public void UnloadScene()
        {
            var error_code = AudienceSDK.Audience.Context.UnloadScene();
            Debug.LogFormat("AudienceSDK.UnloadProfile: {0}", error_code);
        }

        public IEnumerator StartStreamCoroutine()
        {
            Debug.LogFormat("AudienceSDK.Start");
            Task<int> task = Task.Run(() => AudienceSDK.Audience.Context.Start());

            while (!task.IsCompleted)
            {
                yield return null;
            }

            var error_code = task.Result;
            Debug.LogFormat("AudienceSDK.Start completed: {0}", error_code);

            if (error_code == 0)
            {
                AudienceSDK.Audience.Context.StartSendCameraFrame();
            }
        }

        public IEnumerator StopStreamCoroutine()
        {
            Debug.LogFormat("AudienceSDK.Stop");
            Task<int> task = Task.Run(() => AudienceSDK.Audience.Context.Stop());

            while (!task.IsCompleted)
            {
                yield return null;
            }

            var error_code = task.Result;
            Debug.LogFormat("AudienceSDK.Stop completed: {0}", error_code);

            AudienceSDK.Audience.Context.StopSendCameraFrame();
        }

        public StreamState GetCurrentStreamState()
        {
            return AudienceSDK.Audience.Context.GetCurrentStreamState();
        }

        internal static void PostCreateCameraFunc(AudienceCameraInstance instance)
        {
            var audience_ViewerCameraPivot = Audience.instance.ViewerCameraPivot.transform;
            instance.GameObj.transform.SetParent(audience_ViewerCameraPivot);
            instance.Instance = instance.GameObj.AddComponent<AudienceCameraBehaviour>();
            //parse camera pivot
            instance.GameObj.GetComponent<AudienceCameraBehaviour>().ViewerCameraPivot = audience_ViewerCameraPivot;
            instance.Instance.Init(instance.Camera);
            
        }

        private void LoadConfig()
        {
            var configPath = Application.dataPath + "/../audience_user_config.json";
            StreamReader reader = new StreamReader(configPath);

            if (reader != null)
            {
                var content = reader.ReadToEnd();
                this.configData = Newtonsoft.Json.JsonConvert.DeserializeObject<AudienceConfigData>(content);

                AudienceSDK.UserConfig.IsUnityEnable360StereoCapture = configData.isUnityEnable360StereoCapture;
                AudienceSDK.UserConfig.CubemapSize = configData.cubemapSize;

                int maximumCubemapSize = 16384;
                var configCubemapSize = configData.cubemapSize;

                // configCubemapSize should bigger than zero and power of 2
                if (configCubemapSize > 0 && configCubemapSize <= maximumCubemapSize && ((configCubemapSize & (configCubemapSize - 1)) == 0))
                {
                    AudienceSDK.UserConfig.CubemapSize = configCubemapSize;
                }
                else
                {
                    // config has key, but illegal, need calculate one.
                    AudienceSDK.UserConfig.CubemapSize = -1;
                }

                if (configData.sceneName == null)
                {
                    configData.sceneName = "";
                }

                AudienceSDK.UserConfig.DefaultCamAvatarShader = this.configData.defaultCamAvatarShader;
                AudienceSDK.UserConfig.DefaultEmojiShader = this.configData.defaultEmojiShader;
                AudienceSDK.UserConfig.DefaultPreviewQuadShader = this.configData.defaultPreviewQuadShader;
                reader.Close();
            }
            else
            {
                Debug.LogError("Can't find museum_user_config.json");
            }
        }
    }
}
