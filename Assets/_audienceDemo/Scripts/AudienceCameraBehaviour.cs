using System.Linq;
using AudienceSDK;
using UnityEngine;

namespace Audience
{
    public class AudienceCameraBehaviour : AudienceCameraBehaviourBase
    {
        private float moveSpeed = 1.5f;
        private float rotateSpeed = 45.0f;
        public Transform ViewerCameraPivot;

        public override void Init(AudienceSDK.Camera camera)
        {
            Debug.Log("AudienceCameraBehaviour :: Init");
            UnityEngine.Object.DontDestroyOnLoad(this.gameObject);
            this.DelayedInit(camera);
            // this._cam.cullingMask |= BeatSaberUtils.CustomAvatarFirstPersonExclusionMask;
            // this._streamingCamera.cullingMask |= BeatSaberUtils.CustomAvatarFirstPersonExclusionMask;
        }

        protected override void DelayedInit(AudienceSDK.Camera camera)
        {
            Debug.Log("AudienceCameraBehaviour :: DelayedInit");
            base. DelayedInit(camera);
            _streamingCamera.cullingMask |= 1 << LayerMask.NameToLayer("unitychan");
            _streamingCamera.cullingMask |= 1 << LayerMask.NameToLayer("Weapon");
        }

        protected override void OnGUI()
        {
            base.OnGUI();
        }

        protected override void OnDestroy()
        {
            Debug.Log("AudienceCameraBehaviour :: OnDestroy");
            base.OnDestroy();
        }

        // Update is called once per frame
        void Update()
        {
            this.ThirdPersonPos = ViewerCameraPivot.position;
        }

        void MoveLeft()
        {
            this.ThirdPersonPos += Quaternion.Euler(this.ThirdPersonRot) * Vector3.left * this.moveSpeed * Time.deltaTime;
        }

        void MoveRight()
        {
            this.ThirdPersonPos += Quaternion.Euler(this.ThirdPersonRot) * Vector3.right * this.moveSpeed * Time.deltaTime;
        }

        void MoveFront()
        {
            this.ThirdPersonPos += Quaternion.Euler(this.ThirdPersonRot) * Vector3.forward * this.moveSpeed * Time.deltaTime;
        }

        void MoveBack()
        {
            this.ThirdPersonPos += Quaternion.Euler(this.ThirdPersonRot) * Vector3.back * this.moveSpeed * Time.deltaTime;
        }

        void MoveUp()
        {
            this.ThirdPersonPos += Quaternion.Euler(this.ThirdPersonRot) * Vector3.up * this.moveSpeed * Time.deltaTime;
        }

        void MoveDown()
        {
            this.ThirdPersonPos += Quaternion.Euler(this.ThirdPersonRot) * Vector3.down * this.moveSpeed * Time.deltaTime;
        }

        void RotateLeft()
        {
            this.ThirdPersonRot = (Quaternion.AngleAxis(this.rotateSpeed * Time.deltaTime, Vector3.down) * Quaternion.Euler(this.ThirdPersonRot)).eulerAngles;
        }

        void RotateRight()
        {
            this.ThirdPersonRot = (Quaternion.AngleAxis(this.rotateSpeed * Time.deltaTime, Vector3.up) * Quaternion.Euler(this.ThirdPersonRot)).eulerAngles;
        }

        void RotateUp()
        {
            this.ThirdPersonRot = (Quaternion.Euler(this.ThirdPersonRot) * Quaternion.AngleAxis(this.rotateSpeed * Time.deltaTime, Vector3.left)).eulerAngles;
        }

        void RotateDown()
        {
            this.ThirdPersonRot = (Quaternion.Euler(this.ThirdPersonRot) * Quaternion.AngleAxis(this.rotateSpeed * Time.deltaTime, Vector3.right)).eulerAngles;
        }
    }
}
