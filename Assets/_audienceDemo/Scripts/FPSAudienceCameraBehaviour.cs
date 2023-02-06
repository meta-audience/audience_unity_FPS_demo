using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AudienceSDK;
namespace AudienceSDK.Sample
{
    public class FPSAudienceCameraBehavior : AudienceCameraBehaviourBase
    {
        // Start is called before the first frame update
        public override void Init(AudienceSDK.Camera camera)
        {
            UnityEngine.Object.DontDestroyOnLoad(this.gameObject);
            this.DelayedInit(camera);
        }

        protected override void DelayedInit(AudienceSDK.Camera camera)
        {
            base.DelayedInit(camera);
            _streamingCamera.cullingMask |= 1 << LayerMask.NameToLayer("unitychan");
            _streamingCamera.cullingMask |= 1 << LayerMask.NameToLayer("Weapon");
        }

        protected override void OnGUI()
        {
            base.OnGUI();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void Update()
        {
        }
    }
}
