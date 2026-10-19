using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LittlePaws.Maze
{
    /// <summary>Renders the existing animated 3D pet into the maze's transparent UI image.</summary>
    [DefaultExecutionOrder(100)] // Apply movement state before posing and rendering the pet.
    public sealed class MazePet3DView : MonoBehaviour
    {
        public RenderTexture Texture { get; private set; }
        public Vector2Int FacingDirection { get; private set; } = Vector2Int.down;
        public bool IsReady { get { return stage != null && pet != null && previewCamera != null && Texture != null; } }

        private const string PreviewLayerName = "MazePetPreview";
        private readonly Dictionary<Camera, int> cameraMasks = new Dictionary<Camera, int>();
        private readonly Dictionary<Light, int> lightMasks = new Dictionary<Light, int>();
        private GameObject stage;
        private PetVisual pet;
        private Camera previewCamera;
        private Light keyLight;
        private UniversalRenderPipeline.SingleCameraRequest renderRequest;
        private int previewMask, previewLayer;
        private bool visible = true;
        private bool moving;
        private Quaternion targetRotation = Quaternion.Euler(0, 180, 0);

        public void Initialize(bool puppy)
        {
            if (IsReady)
            {
                pet.Build(puppy);
                SetLayerRecursively(pet.transform, previewLayer);
                ResetFacing();
                return;
            }

            // Unity may keep old project layer names cached while the Editor remains open.
            // A numeric unused layer is valid for culling even without a configured name.
            int layer = ResolvePreviewLayer();
            if(layer < 0)
            {
                Debug.LogError("No unused layer is available for the 3D maze companion.",this);
                return;
            }
            previewLayer=layer;
            previewMask = 1 << layer;

            stage = new GameObject("Maze pet 3D preview stage");
            stage.transform.SetParent(transform, false);
            stage.transform.position = new Vector3(1000, 1000, 1000);
            // The owner can be a scaled UI transform; the model and camera use world metres.
            stage.transform.rotation = Quaternion.identity;
            Vector3 inheritedScale = stage.transform.lossyScale;
            stage.transform.localScale = new Vector3(
                SafeInverse(inheritedScale.x), SafeInverse(inheritedScale.y), SafeInverse(inheritedScale.z));

            GameObject model = new GameObject("Animated maze pet");
            model.transform.SetParent(stage.transform, false);
            pet = model.AddComponent<PetVisual>();
            pet.Build(puppy);
            ResetFacing();

            Texture = new RenderTexture(384, 384, 24, RenderTextureFormat.ARGB32)
            {
                name = "Maze pet transparent 3D texture",
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Texture.Create();
            ClearTexture();

            GameObject cameraObject = new GameObject("Maze pet preview camera");
            cameraObject.transform.SetParent(stage.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 2.4f, -4);
            cameraObject.transform.LookAt(stage.transform.TransformPoint(new Vector3(0, .85f, 0)));
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.enabled = false;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 1.12f;
            previewCamera.aspect = 1;
            previewCamera.nearClipPlane = .1f;
            previewCamera.farClipPlane = 12;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = Color.clear;
            previewCamera.cullingMask = previewMask;
            previewCamera.targetTexture = Texture;
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = false;
            previewCamera.useOcclusionCulling = false;
            UniversalAdditionalCameraData data = previewCamera.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Base;
            data.renderPostProcessing = false;
            data.volumeLayerMask = 0;
            data.renderShadows = false;
            data.allowXRRendering = false;
            data.antialiasing = AntialiasingMode.None;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;

            GameObject lightObject = new GameObject("Maze pet soft key light");
            lightObject.transform.SetParent(stage.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(35, -35, 0);
            keyLight = lightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1, .96f, .89f);
            keyLight.intensity = 1.3f;
            keyLight.shadows = LightShadows.None;
            keyLight.cullingMask = previewMask;
            SetLayerRecursively(stage.transform, layer);
            IsolatePreviewLayer();
            renderRequest = new UniversalRenderPipeline.SingleCameraRequest { destination = Texture };
            stage.SetActive(visible);
        }

        public void Face(Vector2Int direction)
        {
            if (direction == Vector2Int.zero) return;
            FacingDirection = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                ? new Vector2Int(direction.x > 0 ? 1 : -1, 0)
                : new Vector2Int(0, direction.y > 0 ? 1 : -1);
            float yaw = FacingDirection.x != 0 ? FacingDirection.x * 90 : FacingDirection.y > 0 ? 0 : 180;
            targetRotation = Quaternion.Euler(0, yaw, 0);
        }

        public void ResetFacing()
        {
            moving = false;
            FacingDirection = Vector2Int.down;
            targetRotation = Quaternion.Euler(0, 180, 0);
            if (pet != null) pet.transform.localRotation = targetRotation;
        }

        public void SetMoving(bool value) { moving = value; }

        public void SetVisible(bool value)
        {
            visible = value;
            if (stage != null) stage.SetActive(value);
        }

        private void LateUpdate()
        {
            if (!visible || !IsReady || !stage.activeInHierarchy) return;
            if(!Texture.IsCreated()){Texture.Create();ClearTexture();}
            pet.transform.localRotation = Quaternion.RotateTowards(pet.transform.localRotation,
                targetRotation, 900 * Time.unscaledDeltaTime);
            pet.AnimateRun(moving ? 1 : 0, Time.unscaledTime);

            // On the first frame the configured asset may exist before URP has initialized.
            // The normal scene camera initializes it later that frame; draw on the next update.
            if (GraphicsSettings.currentRenderPipeline != null && RenderPipelineManager.currentPipeline == null) return;

            // URP supports explicit requests, including a disabled camera. This keeps rendering
            // entirely off while the level selector or results screen is showing.
            Light previousSun = RenderSettings.sun;
            try
            {
                RenderSettings.sun = keyLight;
                if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
                    RenderPipeline.SubmitRenderRequest(previewCamera, renderRequest);
                else if (GraphicsSettings.currentRenderPipeline == null)
                    previewCamera.Render();
            }
            finally { RenderSettings.sun = previousSun; }
        }

        private void IsolatePreviewLayer()
        {
            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if (camera == previewCamera || !camera.gameObject.scene.IsValid()) continue;
                cameraMasks[camera] = camera.cullingMask;
                camera.cullingMask &= ~previewMask;
            }
            foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (light == keyLight || !light.gameObject.scene.IsValid()) continue;
                lightMasks[light] = light.cullingMask;
                light.cullingMask &= ~previewMask;
            }
        }

        private static int ResolvePreviewLayer()
        {
            int named=LayerMask.NameToLayer(PreviewLayerName);
            if(named>=0)return named;
            var used=new HashSet<int>();
            foreach(var item in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if(item.gameObject.scene.IsValid())used.Add(item.gameObject.layer);
            for(int i=31;i>=8;i--)
                if(string.IsNullOrEmpty(LayerMask.LayerToName(i)) && !used.Contains(i))return i;
            return -1;
        }
        private void ClearTexture()
        {
            var previous=RenderTexture.active;
            try{RenderTexture.active=Texture;GL.Clear(true,true,Color.clear);}
            finally{RenderTexture.active=previous;}
        }

        private static float SafeInverse(float value) { return Mathf.Abs(value) < .00001f ? 1 : 1 / value; }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root) SetLayerRecursively(child, layer);
        }

        private void OnDestroy()
        {
            // Restore only the reserved layer bit; preserve any unrelated changes made later.
            foreach (KeyValuePair<Camera, int> entry in cameraMasks)
                if (entry.Key != null) entry.Key.cullingMask = (entry.Key.cullingMask & ~previewMask) | (entry.Value & previewMask);
            foreach (KeyValuePair<Light, int> entry in lightMasks)
                if (entry.Key != null) entry.Key.cullingMask = (entry.Key.cullingMask & ~previewMask) | (entry.Value & previewMask);
            if (previewCamera != null) previewCamera.targetTexture = null;
            if (Texture != null)
            {
                Texture.Release();
                Destroy(Texture);
                Texture = null;
            }
            if (stage != null) Destroy(stage);
        }
    }
}
