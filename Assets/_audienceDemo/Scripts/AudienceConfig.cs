using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Audience
{
    public class AudienceConfigData
    {
        public string account;
        public string password;
        public string sceneName;
        public bool isUnityEnable360StereoCapture = false;
        public int cubemapSize = -1;
        public string defaultCamAvatarShader = "audience/color";
        public string defaultEmojiShader = "audience/emoji";
        public string defaultPreviewQuadShader = "Particles/Standard Unlit";
    }
}