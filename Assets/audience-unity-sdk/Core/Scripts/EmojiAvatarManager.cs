using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace AudienceSDK {
    public class EmojiAvatarManager : MonoBehaviour {

        public float AvatarTotalLifeTime { get; private set; } = 15.0f;
        
        private const string avatarResourcesPath = "Audience/Avatar/";
        private const string avatarSingleKey = "single";
        private const string avatarSingleFileName = "Avatar_Single";
        private const string avatarMultipleKey = "multiple";
        private const string avatarMultipleFileName = "Avatar_Single";
        private const string prefabExtension = ".prefab";
        private const string avatarGenerateArea = "DefaultGenerateArea";

        private Dictionary<string, GameObject> _emojiAvatarPrefabList;
        private LinkedList<EmojiAvatarBehaviourBase> _avatarList;

        //public Vector3 AvatarGeneratePos { get; set; } = Vector3.zero
        private List<Collider> _avatarGenerateColliders = new List<Collider>();
        private Transform _avatarGenerateCollidersObjRoot = null;
        private Transform _areasGenerateFollowParent = null;
        private float _avatarColliderRadius = 0.3f;
        private int _avatarColliderRetryTimes = 20;
        public void InitiateAvatarGenerateArea(Transform generateAreaRoot, Transform areasParent){

            if (areasParent != null){
                this._areasGenerateFollowParent = areasParent;
            }
            else{
                this._areasGenerateFollowParent = this.transform;
            }

            GameObject tmpInstance = null;
            //Set gernerate areas
            if (generateAreaRoot != null){
                this._avatarGenerateCollidersObjRoot = generateAreaRoot;
            }
            else{
                //Set default if null
                GameObject tmpArea = Resources.Load<GameObject>(avatarResourcesPath + avatarGenerateArea);
                Debug.Log(avatarResourcesPath + avatarGenerateArea);
                tmpInstance = Instantiate(tmpArea);

                this._avatarGenerateCollidersObjRoot = tmpInstance.transform;
            }

            _avatarGenerateCollidersObjRoot.SetParent(_areasGenerateFollowParent);

            Collider[] tmpColliders = _avatarGenerateCollidersObjRoot.GetComponentsInChildren<Collider>();
            foreach (Collider col in tmpColliders)
            {
                this._avatarGenerateColliders.Add(col);
            }

        }
        public void UpdateAvatarGenerateAreaParent(Transform targetParent)
        {
            _avatarGenerateCollidersObjRoot.SetParent(targetParent,false);
        }

        private void moveAreaToPlayerChildren(string playerTag)
        {
            if(playerTag == null || playerTag == "")
            {
                Debug.Log("Cannot find player tags");
                return;
            }

            var playerRoot = GameObject.FindGameObjectWithTag(playerTag);
            if (playerRoot == null)
            {
                Debug.Log("Cannot find player obj");
                return;
            }
            _avatarGenerateCollidersObjRoot.SetParent(playerRoot.transform);
        }

        internal AudienceReturnCode GetAvatar(ChatAuthor author, ref EmojiAvatarBehaviourBase avatar) {
            var targetAuthors = new List<ChatAuthor>();
            targetAuthors.Add(author);

            var rc = this.FindExistAvatars(targetAuthors, ref avatar);
            if (rc != AudienceReturnCode.AudienceSDKOk) {
                return rc;
            }

            if (avatar == null) {
                rc = this.CreateAvatar(targetAuthors, ref avatar);
                if (rc != AudienceReturnCode.AudienceSDKOk) {
                    return rc;
                }
            }

            if (avatar == null) {
                Debug.LogError("CreateEmoji Fail, avatar access fail.");
                return AudienceReturnCode.AudienceSDKInternalError;
            }

            return AudienceReturnCode.AudienceSDKOk;
        }

        internal AudienceReturnCode RearrangeAvatarList(EmojiAvatarBehaviourBase avatar) {
            if (this._avatarList == null) {
                Debug.LogError("RearrangeAvatarList error, class Not Inited.");
                return AudienceReturnCode.AudienceSDKNotInited;
            }

            if (avatar == null || !this._avatarList.Contains(avatar)) {
                Debug.LogError("RearrangeAvatarList error, RearrangeAvatarList but not found.");
                return AudienceReturnCode.AudienceInvalidParams;
            }

            this._avatarList.Remove(avatar);
            this._avatarList.AddLast(avatar);
            return AudienceReturnCode.AudienceSDKOk;
        }

        private void Awake() {
            UnityEngine.Object.DontDestroyOnLoad(this.gameObject);
            this._avatarList = new LinkedList<EmojiAvatarBehaviourBase>();
            this._emojiAvatarPrefabList = new Dictionary<string, GameObject>();
            this.PreloadEmojiAvatar();
            //init with default
            InitiateAvatarGenerateArea(null, null);
        }

        private void Start() {
            
        }

        private void OnDestroy() {
        }


        private AudienceReturnCode FindExistAvatars(List<ChatAuthor> targetAuthors, ref EmojiAvatarBehaviourBase emojiAvatar) {
            if (this._avatarList == null) {
                Debug.LogError("FindExistAvatars fail, list not init.");
                return AudienceReturnCode.AudienceSDKNotInited;
            }

            if (targetAuthors == null) {
                Debug.LogError("FindExistAvatars fail, targetAuthors is null.");
                return AudienceReturnCode.AudienceInvalidParams;
            }

            foreach (EmojiAvatarBehaviourBase avatar in this._avatarList) {
                if (avatar == null || !avatar.IsAlive()) {
                    continue;
                }

                var avatarAuthors = new List<ChatAuthor>(avatar.GetAvatarAuthors());

                if (avatarAuthors.Count != targetAuthors.Count || avatarAuthors.Count == 0) {
                    continue;
                }

                foreach (ChatAuthor targetAuthor in targetAuthors) {
                    var matchedAuthor = avatarAuthors.Find(x => x.platform == targetAuthor.platform && x.user_id == targetAuthor.user_id);
                    if (matchedAuthor != null) {
                        avatarAuthors.Remove(matchedAuthor);
                    }
                }

                if (avatarAuthors.Count == 0) {
                    emojiAvatar = avatar;
                    return AudienceReturnCode.AudienceSDKOk;
                }
            }

            return AudienceReturnCode.AudienceSDKOk;
        }

        private AudienceReturnCode CreateAvatar(List<ChatAuthor> avatarAuthors, ref EmojiAvatarBehaviourBase avatar) {
            var rc = AudienceReturnCode.AudienceSDKOk;

            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null) {
                Debug.LogError("CreateAvatar fail, Camera.main not exist");
                return AudienceReturnCode.AudienceSDKInternalError;
            }

            if (this._emojiAvatarPrefabList == null || this._emojiAvatarPrefabList.Count <= 0) {
                Debug.LogError("CreateAvatar fail, emojiAvatarPrefabList is empty");
                return AudienceReturnCode.AudienceSDKNotInited;
            }
            Vector3 avatarPositon = Vector3.zero;
            rc = this.GenerateAvatarPosition(ref avatarPositon);
            if (rc != AudienceReturnCode.AudienceSDKOk) {
                return rc;
            }

            if (avatarAuthors == null || avatarAuthors.Count <= 0) {
                Debug.LogError("CreateAvatar fail, avatarAuthors is empty");
                return AudienceReturnCode.AudienceInvalidParams;
            } else if (avatarAuthors.Count == 1) {
                if (this._emojiAvatarPrefabList.ContainsKey(avatarSingleKey) && this._emojiAvatarPrefabList[avatarSingleKey] != null) {
                    var avatarObject = Instantiate(this._emojiAvatarPrefabList[avatarSingleKey]);
                    avatarObject.transform.SetParent(_avatarGenerateCollidersObjRoot.transform);
                    avatarObject.transform.LookAt(mainCamera.transform);
                    avatarObject.transform.localPosition = avatarPositon;
                    var avatarCollider = avatarObject.AddComponent<SphereCollider>();
                    avatarCollider.radius = this._avatarColliderRadius;
                    var avatarBehavior = avatarObject.AddComponent<EmojiAvatarSingleAuthorBehaviour>();
                    avatarBehavior.OnAvatarFinished += this.HandleAvatarFinished;
                    rc = avatarBehavior.SetAuthors(avatarAuthors);
                    if (rc != AudienceReturnCode.AudienceSDKOk) {
                        UnityEngine.Object.DestroyImmediate(avatarObject);
                        return rc;
                    }

                    rc = avatarBehavior.Init();
                    if (rc != AudienceReturnCode.AudienceSDKOk) {
                        UnityEngine.Object.DestroyImmediate(avatarObject);
                        return rc;
                    }

                    avatar = avatarBehavior;
                    this._avatarList.AddLast(avatarBehavior);
                    return AudienceReturnCode.AudienceSDKOk;
                } else {
                    Debug.LogError("CreateAvatar fail, target AvatarPrefab not found");
                    return AudienceReturnCode.AudienceSDKMapKeyNotFound;
                }
            } else {
                if (this._emojiAvatarPrefabList.ContainsKey(avatarMultipleKey) && this._emojiAvatarPrefabList[avatarMultipleKey] != null) {
                    // TODO this sprint interact not support
                    // Instantiate avatar, add behavior
                    // set authors and init behavior.
                    Debug.LogError("CreateEmoji Fail, interact emoji not support.");
                    return AudienceReturnCode.AudienceSDKInternalError;
                } else {
                    Debug.LogError("CreateAvatar fail, target AvatarPrefab not found");
                    return AudienceReturnCode.AudienceSDKMapKeyNotFound;
                }
            }
        }

        private AudienceReturnCode GenerateAvatarPosition(ref Vector3 avatarPos) {

            Debug.Log("generated 001 ");
            for (int i = 0; i < this._avatarColliderRetryTimes; ++i)
            {
                int randomListIndex = UnityEngine.Random.Range(0, _avatarGenerateColliders.Count - 1);
                Debug.Log("generated 002 randomListIndex: " + randomListIndex);
                var randomColliderInList = _avatarGenerateColliders[randomListIndex];

                Vector3 extents = randomColliderInList.bounds.size / 2f;

                Vector3 randomPoint = new Vector3(
                    UnityEngine.Random.Range(-extents.x, extents.x),
                    UnityEngine.Random.Range(-extents.y, extents.y),
                    UnityEngine.Random.Range(-extents.z, extents.z)
                    );
                Vector3 generatepoint = randomPoint;
                Debug.Log("generated 003 randomPoint: " + generatepoint);
                avatarPos = generatepoint;
                
                if (randomColliderInList.bounds.Contains(generatepoint))
                {
                    Debug.Log("generated 004 Contains: ");
                    return AudienceReturnCode.AudienceSDKOk;

                }
                return AudienceReturnCode.AudienceSDKOk;
            }

            if (this._avatarList != null && this._avatarList.Count > 0) {
                Debug.Log("GenerateAvatarPosition, retry too many times, use the oldest avatar position.");
                var oldestAvatar = this._avatarList.First.Value;
                avatarPos = oldestAvatar.transform.position;
                this._avatarList.RemoveFirst();
                UnityEngine.Object.DestroyImmediate(oldestAvatar.gameObject);
                return AudienceReturnCode.AudienceSDKOk;
            }

            Debug.LogWarning("GenerateAvatarPosition fail, no position for new coming avatar.");
            return AudienceReturnCode.AudienceSDKInternalError;
        }

        private void PreloadEmojiAvatar() {
            /*
            * audience-unity-sdk.csproj would define DLL_BUILD
            * dll will load resources from embeded resources.
            * AudienceSDK-Assembly won't define DLL_BUILD
            * it will load resouces from Resources folder.
            */
#if DLL_BUILD
            var assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream("AudienceSDK.Resources.Art.audience_sdk");
            var audienceSDKBundle = AssetBundle.LoadFromStream(stream);

            var avatarSingle = audienceSDKBundle.LoadAsset<GameObject>(avatarSingleFileName + prefabExtension);
            this._emojiAvatarPrefabList.Add(avatarSingleKey, avatarSingle);

            var avatarMultiple = audienceSDKBundle.LoadAsset<GameObject>(avatarMultipleFileName + prefabExtension);
            this._emojiAvatarPrefabList.Add(avatarMultipleKey, avatarMultiple);

            audienceSDKBundle.Unload(false);
            stream.Close();
#else
            var avatarSingle = Resources.Load<GameObject>(avatarResourcesPath + avatarSingleFileName);
            this._emojiAvatarPrefabList.Add(avatarSingleKey, avatarSingle);

            var avatarMultiple = Resources.Load<GameObject>(avatarResourcesPath + avatarMultipleFileName);
            this._emojiAvatarPrefabList.Add(avatarMultipleKey, avatarMultiple);
#endif
        }

        private void HandleAvatarFinished(EmojiAvatarBehaviourBase avatar) {
            if (avatar != null && this._avatarList != null && this._avatarList.Contains(avatar)) {
                this._avatarList.Remove(avatar);
            }
        }
    }

}
