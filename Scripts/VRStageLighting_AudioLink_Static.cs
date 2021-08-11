﻿

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using VRCAudioLink;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
//using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using System.Collections.Immutable;
#endif

public class VRStageLighting_AudioLink_Static : UdonSharpBehaviour
{
     //////////////////Public Variables////////////////////

    [Header("Audio Link Settings")]
    [Tooltip("Enable or disable Audio Link Reaction for this fixture.")]
    [FieldChangeCallback(nameof(EnableAudioLink))]
    [SerializeField]
    private bool enableAudioLink;


    //[Tooltip("The Audio Link Script to react to.")]
    //public AudioLink audioLink;


    [Tooltip("The frequency band of the spectrum to react to.")]
    [FieldChangeCallback(nameof(Band))]
    [SerializeField]
    private int band;


    [Range(0, 31)]
    [Tooltip("The level of delay to add to the reaction.")]
    [FieldChangeCallback(nameof(Delay))]
    [SerializeField]
    private int delay;


    [Tooltip("Multiplier for the sensativity of the reaction.")]
    [Range(1.0f, 15.0f)]
    [FieldChangeCallback(nameof(BandMultiplier))]
    [SerializeField]
    private float bandMultiplier = 1.0f;


    [Tooltip ("Enable Color Chord tinting of the light emission.")]
    [FieldChangeCallback(nameof(ColorChord))]
    [SerializeField]
    private bool enableColorChord;
    

    [Header("General Settings")]



    [Range(0,1)]
    [FieldChangeCallback(nameof(GlobalIntensity))]
    [SerializeField]
    [Tooltip ("Sets the overall intensity of the shader. Good for animating or scripting effects related to intensity. Its max value is controlled by Final Intensity.")]
    private float globalIntensity = 1; 


    [Range(0,1)]
    [FieldChangeCallback(nameof(FinalIntensity))]
    [SerializeField]
    [Tooltip ("Sets the maximum brightness value of Global Intensity. Good for personalized settings of the max brightness of the shader by other users via UI.")]
    private float finalIntensity = 1;


    [Tooltip ("The main color of the light.")]
    [ColorUsage(false,true)]
    [FieldChangeCallback(nameof(LightColorTint))]
    [SerializeField]
    private Color lightColorTint = Color.white * 2.0f;


    [Tooltip ("Check this box if you wish to sample seperate texture for the color. The color will be influenced by the intensity of the original emission color! The texture is set in the shader itself.")]
    [FieldChangeCallback(nameof(ColorTextureSampling))]
    [SerializeField]
    private bool enableColorTextureSampling;


    [Tooltip ("The UV coordinates to sample the color from on the texture.")]
    [FieldChangeCallback(nameof(TextureSamplingCoordinates))]
    [SerializeField]
    private Vector2 textureSamplingCoordinates = new Vector2(0.5f, 0.5f);
    


    [Space(5)]
    [Header("Movement Settings")]


    [Tooltip ("Invert the pan values (Left/Right Movement) for movers.")]
    [FieldChangeCallback(nameof(InvertPan))]
    [SerializeField]
    private bool invertPan;


    [Tooltip ("Invert the tilt values (Up/Down Movement) for movers.")]
    [FieldChangeCallback(nameof(InvertTilt))]
    [SerializeField]
    private bool invertTilt;


    [Tooltip ("Enable this if the mover is hanging upside down.")]
    public bool isUpsideDown;

    
    [Tooltip ("Enable target following for a mover while in Udon mode.")]
    public bool followTarget;


    [Tooltip ("The target for this mover to follow.")]
    public Transform targetToFollow;


    [Tooltip ("The speed at which the target is followed, higher = slower.")]
    public float targetFollowLerpSpeed = 20.0f;


    [Tooltip ("An offset of where the mover will point at relative to the target.")]
    public Vector3 targetOffset;


    [Space(5)]
    [Header("Fixture Settings")]


    [Tooltip ("Enable projection spinning (Udon Override Only).")]
    [FieldChangeCallback(nameof(ProjectionSpin))]
    [SerializeField]
    private bool enableAutoSpin;


/////////////////////////////////
    [Range(0,360.0f)]
    [Tooltip ("Tilt (Up/Down) offset/movement. Directly controls tilt when in Udon Mode.")]
    [FieldChangeCallback(nameof(Tilt))]
    [SerializeField]
    private float tiltOffsetBlue = 90.0f;
    float startTiltOffset;

    [Range(0,360.0f)]
    [Tooltip ("Pan (Left/Right) offset/movement. Directly controls pan when in Udon Mode; is an offset when in DMX mode.")]
    [FieldChangeCallback(nameof(Pan))]
    [SerializeField]
    public float panOffsetBlueGreen = 0.0f;
    float startPanOffset;
////////////////////////////////////



    [Range(1,6)]
    [FieldChangeCallback(nameof(SelectGOBO))]
    [SerializeField]
    [Tooltip ("Use this to change what projection is selected")]
    private int selectGOBO = 1;


    [Header("Mesh Settings")]
    [Tooltip ("The meshes used to make up the light. You need atleast 1 mesh in this group for the script to work properly.")]
    public MeshRenderer[] objRenderers;


    [Tooltip ("Controls the radius of a mover/spot light.")]
    [Range(0, 5.5f)]
    [FieldChangeCallback(nameof(ConeWidth))]
    [SerializeField]
    private float coneWidth = 2.5f;


    [Range(0.5f,10.0f)]
    [FieldChangeCallback(nameof(ConeLength))]
    [SerializeField]
    [Tooltip ("Controls the length of the cone of a mover/spot light.")]
    private float coneLength = 8.5f; 
    [ColorUsage(true, true)]
    

    /////////////////Private Variables//////////////////
    
    private MaterialPropertyBlock props;
    //bool enableInstancing;
    float targetPanAngle, targetTiltAngle;
    private Vector3 targetToFollowLast;
    float[] spectrumBands;
    private bool wasChanged;
    private Color previousColorTint;
    private Transform previousTargetToFollowTransform;
    
    private float previousConeWidth, previousConeLength, previousGlobalIntensity, previousFinalIntensity;
    private int previousGOBOSelection;

    void OnEnable() 
    {
        if(objRenderers.Length > 0 && objRenderers[0] != null)
        {
            props = new MaterialPropertyBlock();
            //enableInstancing = true;
                    
            if(followTarget)
            {
                GetTargetAngles();
               // LerpToDefaultTarget();
                panOffsetBlueGreen = targetPanAngle;
                tiltOffsetBlue = targetTiltAngle;
                targetToFollowLast = targetToFollow.position;
            }
            previousColorTint = lightColorTint;
            previousConeWidth = coneWidth;
            previousConeLength = coneLength;
            previousGOBOSelection = selectGOBO;
            previousGlobalIntensity = globalIntensity;
            previousFinalIntensity = finalIntensity;
            if(targetToFollow)
            {
                previousTargetToFollowTransform = targetToFollow;
            }
            _UpdateInstancedProperties();
        }
        else
        {
            Debug.Log("Please add atleast one fixture renderer.");
            //enableInstancing = false;
        }
    }

    void Start()
    {
        //spectrumBands = (float[])audioLink.GetProgramVariable("spectrumBands");
        if(objRenderers.Length > 0 && objRenderers[0] != null)
        {
            props = new MaterialPropertyBlock();
            //enableInstancing = true;
                    
            if(followTarget)
            {
                GetTargetAngles();
               // LerpToDefaultTarget();
                panOffsetBlueGreen = targetPanAngle;
                tiltOffsetBlue = targetTiltAngle;
                targetToFollowLast = targetToFollow.position;
            }
            previousColorTint = lightColorTint;
            previousConeWidth = coneWidth;
            previousConeLength = coneLength;
            previousGOBOSelection = selectGOBO;
            previousGlobalIntensity = globalIntensity;
            previousFinalIntensity = finalIntensity;
            if(targetToFollow)
            {
                previousTargetToFollowTransform = targetToFollow;
            }
            _UpdateInstancedProperties();
        }
        else
        {
            Debug.Log("Please add atleast one fixture renderer.");
            //enableInstancing = false;
        }
    }
/////////////////////////////////////////////////////////////////////////PROPERTIES///////////////////////////////////////////////////////////////////////////////////////////////
    public Color LightColorTint
    {
        get
        {
            return lightColorTint;
        }
        set
        {
            previousColorTint = lightColorTint;
            lightColorTint = value;
            _UpdateInstancedProperties();
        }
    }
    public float ConeWidth
    {
        get
        {
            return coneWidth;
        }
        set
        {
            previousConeWidth = coneWidth;
            coneWidth = value;
            _UpdateInstancedProperties();
        }
    }
    public float ConeLength
    {
        get
        {
            return ConeLength;
        }
        set
        {
            previousConeLength = coneLength;
            coneLength = value;
            _UpdateInstancedProperties();
        }
    }
    public float GlobalIntensity
    {
        get
        {
            return globalIntensity;
        }
        set
        {
            previousGlobalIntensity = globalIntensity;
            globalIntensity = value;
            _UpdateInstancedProperties();
        }
    }
    public float FinalIntensity
    {
        get
        {
            return finalIntensity;
        }
        set
        {
            previousFinalIntensity = finalIntensity;
            finalIntensity = value;
            _UpdateInstancedProperties();
        }
    }
    public int SelectGOBO
    {
        get
        {
            return selectGOBO;
        }
        set
        {
            previousGOBOSelection = selectGOBO;
            selectGOBO = value;
            _UpdateInstancedProperties();
        }
    }
    public bool EnableAudioLink
    {
        get
        {
            return enableAudioLink;
        }
        set
        {
            enableAudioLink = value;
            _UpdateInstancedProperties();
        }
    }
    public int Band
    {
        get
        {
            return band;
        }
        set
        {
            band = value;
            _UpdateInstancedProperties();
        }
    }
    public int Delay
    {
        get
        {
            return delay;
        }
        set
        {
            delay = value;
            _UpdateInstancedProperties();
        }
    }
    public float BandMultiplier
    {
        get
        {
            return bandMultiplier;
        }
        set
        {
            bandMultiplier = value;
            _UpdateInstancedProperties();
        }
    }
    public Vector2 TextureSamplingCoordinates
    {
        get
        {
            return textureSamplingCoordinates;
        }
        set
        {
            textureSamplingCoordinates = value;
            _UpdateInstancedProperties();
        }
    }
    public bool InvertPan
    {
        get
        {
            return invertPan;
        }
        set
        {
            invertPan = value;
            _UpdateInstancedProperties();
        }
    }
    public bool InvertTilt
    {
        get
        {
            return invertTilt;
        }
        set
        {
            invertTilt = value;
            _UpdateInstancedProperties();
        }
    }
    public bool ProjectionSpin
    {
        get
        {
            return enableAutoSpin;
        }
        set
        {
            enableAutoSpin = value;
            _UpdateInstancedProperties();
        }
    }
        public bool ColorChord
    {
        get
        {
            return enableColorChord;
        }
        set
        {
            enableColorChord = value;
            _UpdateInstancedProperties();
        }
    }
         public bool ColorTextureSampling
        {
            get
            {
                return enableColorTextureSampling;
            }
            set
            {
                enableColorTextureSampling = value;
                _UpdateInstancedProperties();
            }
        }
        public float Pan
        {
            get
            {
                return panOffsetBlueGreen;
            }
            set
            {
                panOffsetBlueGreen = value;
                _UpdateInstancedProperties();
            }
        }
        public float Tilt
        {
            get
            {
                return tiltOffsetBlue;
            }
            set
            {
                tiltOffsetBlue = value;
                _UpdateInstancedProperties();
            }
        }

/////////////////////////////////////////////////////////////////////////END PROPERTIES///////////////////////////////////////////////////////////////////////////////////////////////
    public void _ChangeCheckPosition()
    {
        GetTargetAngles();
        if((targetToFollow.position != targetToFollowLast) || (panOffsetBlueGreen != targetPanAngle || tiltOffsetBlue != targetTiltAngle))
        {
            LerpToDefaultTarget();
            targetToFollowLast = targetToFollow.position;
        }
        return;
    }


    public void _UpdateInstancedProperties()
    {   
        //Color Texture Sampling
        props.SetFloat("_TextureColorSampleX", textureSamplingCoordinates.x);
        props.SetFloat("_TextureColorSampleY", textureSamplingCoordinates.y);
        props.SetInt("_EnableColorTextureSample", enableColorTextureSampling == true ? 1 : 0);

        //AudioLink Stuff
        props.SetFloat("_EnableAudioLink", enableAudioLink == true ? 1.0f : 0.0f);
        props.SetInt("_EnableColorChord", enableColorChord == true ? 1 : 0);
        //props.SetFloat("_NumBands", spectrumBands.Length);
        props.SetFloat("_Delay", delay);
        props.SetFloat("_BandMultiplier", bandMultiplier);
        props.SetFloat("_Band", band);
        //Movement Stuff
        props.SetInt("_PanInvert", invertPan == true ? 1 : 0);
        props.SetInt("_TiltInvert", invertTilt == true ? 1 : 0);
        props.SetInt("_EnableSpin", enableAutoSpin == true ? 1 : 0);
        props.SetFloat("_FixtureRotationX", tiltOffsetBlue);
        props.SetFloat("_FixtureBaseRotationY", panOffsetBlueGreen);
        //Other Stuff
        props.SetInt("_ProjectionSelection", selectGOBO);
        props.SetColor("_Emission", lightColorTint);
        props.SetFloat("_ConeWidth", coneWidth);
        props.SetFloat("_GlobalIntensity", globalIntensity);
        props.SetFloat("_FinalIntensity", finalIntensity);
        props.SetFloat("_ConeLength", Mathf.Abs(coneLength - 10.5f));
        // for(int i = 0; i < objRenderers.Length; i++)
        // {
        //     objRenderers[i].SetPropertyBlock(props);
        // }
        switch(objRenderers.Length)
        {
            case 1:
                objRenderers[0].SetPropertyBlock(props);
                break;
            case 2:
                objRenderers[0].SetPropertyBlock(props);
                objRenderers[1].SetPropertyBlock(props);
                break;
            case 3:
                objRenderers[0].SetPropertyBlock(props);
                objRenderers[1].SetPropertyBlock(props);
                objRenderers[2].SetPropertyBlock(props);
                break;
            case 4:
                objRenderers[0].SetPropertyBlock(props);
                objRenderers[1].SetPropertyBlock(props);
                objRenderers[2].SetPropertyBlock(props);
                objRenderers[3].SetPropertyBlock(props);
                break;
            case 5:
                objRenderers[0].SetPropertyBlock(props);
                objRenderers[1].SetPropertyBlock(props);
                objRenderers[2].SetPropertyBlock(props);
                objRenderers[3].SetPropertyBlock(props);
                objRenderers[4].SetPropertyBlock(props);
                break;
            default:
                Debug.Log("Too many mesh renderers for this fixture!");
                break;          
        }
    }
    void GetTargetAngles()
    {
            Vector3 dir = ((targetToFollow.position + targetOffset) - this.transform.position).normalized;
            float dist = Vector3.Distance(targetToFollow.position,this.transform.position);       
            Quaternion rot = Quaternion.LookRotation(dir,Vector3.up);
            Vector3 angles = rot.eulerAngles;
            targetPanAngle = invertPan == true ? (1.0f-((angles.y))*-1.0f) : angles.y;
            targetTiltAngle = invertTilt == true ? (angles.x-180.0f)*-1.0f : angles.x;
            targetPanAngle = isUpsideDown == false ? targetPanAngle+180.0f : targetPanAngle;
            targetTiltAngle = isUpsideDown == false ? -targetTiltAngle : targetTiltAngle;
            targetPanAngle = targetPanAngle - this.transform.localEulerAngles.y;
            return;
    }
    void LerpToDefaultTarget()
    {
            float previousPanAngle = panOffsetBlueGreen;
            float previousTiltAngle = tiltOffsetBlue;
            panOffsetBlueGreen = Mathf.LerpAngle(previousPanAngle, targetPanAngle, targetFollowLerpSpeed * Time.deltaTime);
            tiltOffsetBlue = Mathf.Clamp(Mathf.LerpAngle(previousTiltAngle, targetTiltAngle, targetFollowLerpSpeed * Time.deltaTime),0.0f, 180.0f);
            return;
    }
    #if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmos()
        {
            this.UpdateProxy(ProxySerializationPolicy.RootOnly);
            Gizmos.color = lightColorTint;
            if(targetToFollow != null)
            {
                Gizmos.DrawWireSphere(targetToFollow.position, 0.25f);
            }
            Gizmos.DrawWireSphere(transform.position, 0.05f);
        }
    #endif
}
