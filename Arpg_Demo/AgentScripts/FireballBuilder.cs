using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AgentTools
{
    public static class FireballBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Projectiles/Fireball.prefab";
        private const string MaterialDir = "Assets/Res/Material/Fireball";

        private const string FireTexturePath =
            "Assets/Res/Textures/Fireball/Fire13_Orange.png";
        private const string GlowTexturePath =
            "Assets/ArtRes/Hovl Studio/Sword slash VFX/Textures/Glow1.png";
        private const string SparkTexturePath =
            "Assets/ArtRes/Hovl Studio/Sword slash VFX/Textures/SparkTrail1.png";
        private const string SmokeTexturePath =
            "Assets/ArtRes/Hovl Studio/8 KI levels/Textures/Smoke30.png";
        private const string TrailTexturePath =
            "Assets/ArtRes/Hovl Studio/8 KI levels/Textures/Trail65.png";

        private static readonly Color CoreColor = new Color(1f, 0.62f, 0.16f, 1f);
        private static readonly Color InnerColor = new Color(1f, 0.72f, 0.18f, 1f);
        private static readonly Color OuterColor = new Color(1f, 0.25f, 0.02f, 1f);
        private static readonly Color SparkColor = new Color(1f, 0.65f, 0.12f, 1f);
        private static readonly Color SmokeColor = new Color(0.16f, 0.055f, 0.02f, 0.16f);
        private static readonly Color LightColor = new Color(1f, 0.38f, 0.06f, 1f);

        public static void Build()
        {
            Texture2D fireTexture = LoadTexture(FireTexturePath);
            Texture2D glowTexture = LoadTexture(GlowTexturePath);
            Texture2D sparkTexture = LoadTexture(SparkTexturePath);
            Texture2D smokeTexture = LoadTexture(SmokeTexturePath);
            Texture2D trailTexture = LoadTexture(TrailTexturePath);

            Material coreMaterial = CreateCoreMaterial();
            Material flameMaterial = CreateParticleMaterial(
                MaterialDir + "/M_Fireball_Flame.mat", glowTexture, false, true);
            Material smokeMaterial = CreateParticleMaterial(
                MaterialDir + "/M_Fireball_Smoke.mat", smokeTexture, false, true);
            Material glowMaterial = CreateParticleMaterial(
                MaterialDir + "/M_Fireball_Glow.mat", glowTexture, false, false);
            Material emberMaterial = CreateParticleMaterial(
                MaterialDir + "/M_Fireball_Ember.mat", sparkTexture, true, false);
            Material trailMaterial = CreateParticleMaterial(
                MaterialDir + "/M_Fireball_Trail.mat", trailTexture, false, false);
            Material outerMaterial = CreateParticleMaterial(
                MaterialDir + "/M_Fireball_Outer.mat", fireTexture, false, true);
            flameMaterial.SetColor("_BaseColor", new Color(1.3f, 2.3f, 0.9f, 1f));
            outerMaterial.SetColor("_BaseColor", new Color(1.1f, 1.8f, 0.5f, 1f));
            glowMaterial.SetColor("_BaseColor", new Color(2.2f, 1.35f, 0.35f, 1f));
            trailMaterial.SetColor("_BaseColor", new Color(2.5f, 1.5f, 0.35f, 1f));
            EditorUtility.SetDirty(flameMaterial);
            EditorUtility.SetDirty(outerMaterial);
            EditorUtility.SetDirty(glowMaterial);
            EditorUtility.SetDirty(trailMaterial);

            GameObject root = new GameObject("Fireball");
            try
            {
                BuildCore(root.transform, coreMaterial);
                BuildCoreGlow(root.transform, glowMaterial);
                BuildFlameInner(root.transform, flameMaterial);
                BuildFlameOuter(root.transform, outerMaterial);
                BuildEmbers(root.transform, emberMaterial);
                BuildSmoke(root.transform, smokeMaterial);
                BuildTrail(root, trailMaterial);
                BuildLight(root.transform);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null)
                    throw new InvalidOperationException("Failed to save fireball prefab.");

                EditorUtility.SetDirty(prefab);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            EnableBloom();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Fireball prefab created: " + PrefabPath);
        }

        private static void BuildCore(Transform parent, Material material)
        {
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            core.transform.SetParent(parent, false);
            core.transform.localPosition = Vector3.zero;
            core.transform.localScale = Vector3.one * 0.22f;

            Collider collider = core.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);

            MeshRenderer renderer = core.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static void BuildCoreGlow(Transform parent, Material material)
        {
            ParticleSystem ps = CreateParticleSystem("CoreGlow", parent);

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.22f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.03f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.38f, 0.68f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.75f, 0.25f, 1f),
                new Color(1f, 0.42f, 0.06f, 1f));
            main.maxParticles = 16;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 20f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.015f;
            shape.radiusThickness = 1f;

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(CreateGradient(
                new Color(1f, 0.98f, 0.7f, 0.8f),
                new Color(1f, 0.88f, 0.42f, 1f),
                new Color(1f, 0.55f, 0.08f, 0.55f),
                new Color(1f, 0.18f, 0.01f, 0f)));

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, CreateCurve(1f, 0.55f));

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.material = material;
            renderer.sortingFudge = 12f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void BuildFlameInner(Transform parent, Material material)
        {
            ParticleSystem ps = CreateParticleSystem("FlameInner", parent);

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.56f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 0.9f, 0.9f),
                new Color(1f, 0.85f, 0.45f, 0.8f));
            main.maxParticles = 56;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 42f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.075f;
            shape.radiusThickness = 1f;

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(CreateGradient(
                new Color(1f, 1f, 0.95f, 1f),
                new Color(1f, 0.96f, 0.62f, 1f),
                new Color(1f, 0.72f, 0.2f, 0.75f),
                new Color(1f, 0.38f, 0.04f, 0f)));

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, CreateCurve(0.75f, 1.45f, 0.7f, 0.05f));

            ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.07f;
            noise.frequency = 1.8f;
            noise.scrollSpeed = 0.35f;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.material = material;
            renderer.sortingFudge = 8f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void BuildFlameOuter(Transform parent, Material material)
        {
            ParticleSystem ps = CreateParticleSystem("FlameOuter", parent);

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f, 0.48f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.52f, 0.95f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.85f, 0.4f, 0.65f),
                new Color(0.9f, 0.4f, 0.08f, 0.55f));
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 30f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.14f;
            shape.radiusThickness = 0.75f;

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.z = new ParticleSystem.MinMaxCurve(-0.55f, -0.12f);
            velocity.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.18f);

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(CreateGradient(
                new Color(1f, 0.98f, 0.72f, 0.9f),
                new Color(1f, 0.78f, 0.35f, 0.9f),
                new Color(1f, 0.48f, 0.08f, 0.5f),
                new Color(0.6f, 0.09f, 0f, 0f)));

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, CreateCurve(0.45f, 1.1f, 0.85f, 0.1f));

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.16f;
            noise.frequency = 1.2f;
            noise.scrollSpeed = 0.5f;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.material = material;
            renderer.sortingFudge = 5f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void BuildEmbers(Transform parent, Material material)
        {
            ParticleSystem ps = CreateParticleSystem("Embers", parent);

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.62f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.65f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.075f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.85f, 0.28f, 1f),
                new Color(1f, 0.35f, 0.015f, 0.9f));
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.gravityModifier = 0.35f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 22f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.09f;
            shape.radiusThickness = 0.6f;

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(CreateGradient(
                new Color(1f, 0.95f, 0.6f, 1f),
                new Color(1f, 0.55f, 0.08f, 0.9f),
                new Color(1f, 0.16f, 0.01f, 0.45f),
                new Color(0.3f, 0.01f, 0f, 0f)));

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, CreateCurve(1f, 0.1f));

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.22f;
            noise.frequency = 2.2f;
            noise.scrollSpeed = 0.8f;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.material = material;
            renderer.sortingFudge = 10f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void BuildSmoke(Transform parent, Material material)
        {
            ParticleSystem ps = CreateParticleSystem("SmokeTrail", parent);

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.28f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(SmokeColor);
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 7f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;
            shape.radiusThickness = 0.8f;

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.z = new ParticleSystem.MinMaxCurve(-0.18f, -0.04f);
            velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(CreateGradient(
                new Color(0.25f, 0.08f, 0.025f, 0.13f),
                new Color(0.16f, 0.045f, 0.012f, 0.18f),
                new Color(0.08f, 0.02f, 0.005f, 0.08f),
                new Color(0.02f, 0.005f, 0f, 0f)));

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, CreateCurve(0.8f, 1.4f, 2.1f, 2.8f));

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.1f;
            noise.frequency = 0.8f;
            noise.scrollSpeed = 0.2f;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.material = material;
            renderer.sortingFudge = 2f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void BuildTrail(GameObject root, Material material)
        {
            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.34f;
            trail.minVertexDistance = 0.015f;
            trail.widthMultiplier = 1f;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.42f),
                new Keyframe(0.35f, 0.28f),
                new Keyframe(1f, 0f));
            trail.colorGradient = CreateGradient(
                new Color(1f, 0.85f, 0.35f, 0.85f),
                new Color(1f, 0.45f, 0.05f, 0.8f),
                new Color(1f, 0.14f, 0.01f, 0.45f),
                new Color(0.3f, 0.01f, 0f, 0f));
            trail.material = material;
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.generateLightingData = false;
            trail.emitting = true;
        }

        private static void BuildLight(Transform parent)
        {
            GameObject lightObject = new GameObject("PointLight");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = Vector3.zero;

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = LightColor;
            light.intensity = 5.5f;
            light.range = 4.5f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
        }

        private static ParticleSystem CreateParticleSystem(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);

            ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = true;

            ParticleSystemRenderer renderer = gameObject.GetComponent<ParticleSystemRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return ps;
        }

        private static Material CreateCoreMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP Lit shader not found.");

            Material material = GetOrCreateMaterial(MaterialDir + "/M_Fireball_Core.mat", shader);
            material.SetColor("_BaseColor", new Color(0.42f, 0.12f, 0.015f, 1f));
            material.SetColor("_EmissionColor", new Color(1.2f, 0.62f, 0.05f, 1f));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.18f);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateParticleMaterial(
            string path, Texture2D texture, bool additive, bool softParticles)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                throw new InvalidOperationException("URP Particles/Unlit shader not found.");

            Material material = GetOrCreateMaterial(path, shader);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_EmissionColor", additive
                ? new Color(1.2f, 0.75f, 0.28f, 1f)
                : new Color(0.08f, 0.03f, 0.01f, 1f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", additive ? 2f : 0f);
            material.SetFloat("_SrcBlend", additive
                ? (float)UnityEngine.Rendering.BlendMode.One
                : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", additive
                ? (float)UnityEngine.Rendering.BlendMode.One
                : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_SoftParticlesEnabled", softParticles ? 1f : 0f);
            material.SetFloat("_SoftParticlesNearFadeDistance", 0.05f);
            material.SetFloat("_SoftParticlesFarFadeDistance", 1.2f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_EMISSION");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (softParticles)
                material.EnableKeyword("_SOFTPARTICLES_ON");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateMaterial(string path, Shader shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            return material;
        }

        private static Texture2D LoadTexture(string path)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                Debug.LogWarning("Missing VFX texture: " + path);
            return texture;
        }

        private static AnimationCurve CreateCurve(params float[] keys)
        {
            AnimationCurve curve = new AnimationCurve();
            for (int i = 0; i < keys.Length; i++)
            {
                float time = keys.Length > 1 ? (float)i / (keys.Length - 1) : 0f;
                curve.AddKey(time, keys[i]);
            }

            return curve;
        }

        private static Gradient CreateGradient(Color start, Color middle, Color end, Color tail)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(middle, 0.35f),
                    new GradientColorKey(end, 0.72f),
                    new GradientColorKey(tail, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(start.a, 0f),
                    new GradientAlphaKey(middle.a, 0.35f),
                    new GradientAlphaKey(end.a, 0.72f),
                    new GradientAlphaKey(tail.a, 1f)
                });
            return gradient;
        }

        private static void EnableBloom()
        {
            VolumeProfile profile =
                AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/DefaultVolumeProfile.asset");
            if (profile == null)
            {
                Debug.LogWarning("DefaultVolumeProfile not found; Bloom unchanged.");
                return;
            }

            if (!profile.TryGet(out Bloom bloom))
            {
                Debug.LogWarning("Bloom override not found in DefaultVolumeProfile.");
                return;
            }

            bloom.active = true;
            bloom.intensity.Override(0.38f);
            bloom.threshold.Override(0.95f);
            bloom.scatter.Override(0.6f);
            bloom.highQualityFiltering.Override(true);
            EditorUtility.SetDirty(profile);
            Debug.Log("Bloom enabled on DefaultVolumeProfile.");
        }
    }
}
