using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Windows.Speech;

namespace VoiceCommandMod
{
    public class Reviver : MonoBehaviour
    {
        public void Update()
        {
            if ((Plugin.drecognizer == null || Plugin.drecognizer.Status != SpeechSystemStatus.Running || Time.realtimeSinceStartup >= LastStartupTime + 5f) && Time.realtimeSinceStartup >= LastStartupTime + 1f)
            {
                LastStartupTime = Time.realtimeSinceStartup;
                Plugin.CreateRecognizer();
            }
        }

        public static float LastStartupTime;
    }
}
