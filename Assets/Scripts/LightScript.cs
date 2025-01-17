using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine;

public class LightScript : MonoBehaviour
{
		private Light myLight;
		public ARCameraManager manager;
		
    void Start()
    {
    	myLight = this.GetComponent<Light>();        	
		RenderSettings.ambientMode = AmbientMode.Skybox;
    }

    void OnEnable()
    {
			manager.frameReceived += handleChange;
    }
    void OnDisable()
    {
			manager.frameReceived -= handleChange;
    }
        
    void handleChange(ARCameraFrameEventArgs args) 
    {

        if (args.lightEstimation.averageBrightness.HasValue) {
            myLight.intensity = args.lightEstimation.averageBrightness.Value;
        }

        if (args.lightEstimation.averageColorTemperature.HasValue) {
            myLight.colorTemperature = args.lightEstimation.averageColorTemperature.Value;
        }

        if (args.lightEstimation.colorCorrection.HasValue) {
            myLight.color = args.lightEstimation.colorCorrection.Value;
        }

        if (args.lightEstimation.mainLightDirection.HasValue) {
            myLight.transform.rotation = 
				Quaternion.LookRotation(args.lightEstimation.mainLightDirection.Value);
        }

        if (args.lightEstimation.mainLightIntensityLumens.HasValue) {

            myLight.intensity =  args.lightEstimation.mainLightIntensityLumens.Value;
        }

        if (args.lightEstimation.ambientSphericalHarmonics.HasValue) {
            RenderSettings.ambientProbe = args.lightEstimation.ambientSphericalHarmonics.Value;
	    }		

    }
}
