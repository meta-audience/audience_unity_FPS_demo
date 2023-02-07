using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudienceSDK.Sample
{
    public class FPSCameraFollow : CameraMoveAlgorithmBase
    {
        [SerializeField]
        private Transform _followTarget = null;

        [SerializeField]
        private Vector3 _absOffset = Vector3.zero;

        protected override void Start()
        {
            base.Start();
            if (this._followTarget)
            {
                this.MoveCameras(Quaternion.identity, this._followTarget.position + this._absOffset, true);
            }
        }

        private void OnEnable()
        {
            if (this._followTarget)
            {
                this.MoveCameras(Quaternion.identity, this._followTarget.position + this._absOffset, true);
            }
        }

        private void FixedUpdate()
        {
            if (this._followTarget)
            {
                this.MoveCameras(Quaternion.identity, this._followTarget.position + this._absOffset, true);
            }
        }
    }
}
