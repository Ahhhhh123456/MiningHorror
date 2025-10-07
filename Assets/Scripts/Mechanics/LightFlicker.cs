using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LightFlicker : MonoBehaviour
{
    public Light flickeringLight;
    public GameObject torchHead;
    public Material torchMaterial;
    public Material torchMaterialOff;
    // get a reference to the global volume
    public Volume globalVolume;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        flickeringLight = GetComponent<Light>();

        // reduce the coal count every second
        InvokeRepeating("ReduceCoalCount", 1f, 1f);
    }

    // Update is called once per frame
    void Update()
    {
        // Randomly change the intensity of the light to create a flickering effect
        if (GameManager.instance.coalCollected > 0)
        {
            flickeringLight.intensity = Random.Range(20.75f, 21.25f);
            torchHead.GetComponent<MeshRenderer>().material = torchMaterial; // Set the torch material to on

            // Decrease the intensity of the vignette effect by 0.001f
            if (globalVolume.profile.TryGet<Vignette>(out var vignette))
            {
                vignette.intensity.value -= 0.01f;
                vignette.intensity.value = Mathf.Clamp(vignette.intensity.value, 0, 1); // Clamp the intensity to prevent it from going too low
            }
            // do the same for the film grain effect
            if (globalVolume.profile.TryGet<FilmGrain>(out var filmGrain))
            {
                filmGrain.intensity.value -= 0.01f;
                filmGrain.intensity.value = Mathf.Clamp(filmGrain.intensity.value, 0, 1); // Clamp the intensity to prevent it from going too low
            }
            // do the same for the lens distortion effect
            if (globalVolume.profile.TryGet<LensDistortion>(out var lensDistortion))
            {
                lensDistortion.intensity.value -= 0.01f;
                lensDistortion.intensity.value = Mathf.Clamp(lensDistortion.intensity.value, 0, 1); // Clamp the intensity to prevent it from going too low
            }
        }
        else
        {
            flickeringLight.intensity = 0; // Turn off the light if no coal is collected
            torchHead.GetComponent<MeshRenderer>().material = torchMaterialOff; // Set the torch material to off
            // increment the intersity of the vignette effect by 0.001f
            if (globalVolume.profile.TryGet<Vignette>(out var vignette))
            {
                vignette.intensity.value += 0.001f;
                vignette.intensity.value = Mathf.Clamp(vignette.intensity.value, 0, 1); // Clamp the intensity to prevent it from going too high
            }
            // do the same for the film grain effect
            if (globalVolume.profile.TryGet<FilmGrain>(out var filmGrain))
            {
                filmGrain.intensity.value += 0.001f;
                filmGrain.intensity.value = Mathf.Clamp(filmGrain.intensity.value, 0, 1); // Clamp the intensity to prevent it from going too high
            }
            // do the same for the lens distortion effect
            if (globalVolume.profile.TryGet<LensDistortion>(out var lensDistortion))
            {
                lensDistortion.intensity.value += 0.001f;
                lensDistortion.intensity.value = Mathf.Clamp(lensDistortion.intensity.value, 0, 1); // Clamp the intensity to prevent it from going too high
            }
        }
    }
    private void ReduceCoalCount()
    {
        if (GameManager.instance.coalCollected > 0)
        {
            GameManager.instance.CollectCoal(-1); // Decrease coal count by 1
                                                  // update the UI text
            GameManager.instance.coalCountText.text = "Fuel: " + GameManager.instance.coalCollected;
        }
    }
    
}
