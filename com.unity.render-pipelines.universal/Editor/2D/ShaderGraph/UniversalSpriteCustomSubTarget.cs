using System;
using System.Linq;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalSpriteCustomSubTarget : SubTarget<UniversalTarget>
    {
        [MenuItem("Assets/Create/Shader/Universal Render Pipeline/Sprite Custom Lit Shader Graph", false, 300)]
        public static void CreateSpriteLitGraph()
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(typeof(UniversalSpriteCustomSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
                UniversalBlockFields.SurfaceDescription.SpriteMask,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] {target}, blockDescriptors);
        }

        static readonly GUID kSourceCodeGuid = new GUID("69e608b3e7e0405bbc2f259ad9cfa196"); // UniversalUnlitSubTarget.cs

        public UniversalSpriteCustomSubTarget()
        {
            displayName = "Sprite Custom Lit";
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.AddSubShader(SubShaders.SpriteLit);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            // Only support SpriteColor legacy block if BaseColor/Alpha are not active
            var descs = context.blocks.Select(x => x.descriptor);
            bool useLegacyBlocks = !descs.Contains(BlockFields.SurfaceDescription.BaseColor) && !descs.Contains(BlockFields.SurfaceDescription.Alpha);
            context.AddField(CoreFields.UseLegacySpriteBlocks, useLegacyBlocks);

            // Surface Type & Blend Mode
            context.AddField(UniversalFields.SurfaceTransparent);
            context.AddField(Fields.BlendAlpha);
            context.AddField(Fields.DoubleSided);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Only support SpriteColor legacy block if BaseColor/Alpha are not active
            bool useLegacyBlocks = !context.currentBlocks.Contains(BlockFields.SurfaceDescription.BaseColor) && !context.currentBlocks.Contains(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescriptionLegacy.SpriteColor, useLegacyBlocks);

            context.AddBlock(UniversalBlockFields.SurfaceDescription.SpriteMask);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
        }

        #region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor SpriteLit = new SubShaderDescriptor()
            {
                pipelineTag = UniversalTarget.kPipelineTag,
                customTags = UniversalTarget.kLitMaterialTypeTag,
                renderType = $"{RenderType.Transparent}",
                renderQueue = $"{UnityEditor.ShaderGraph.RenderQueue.Transparent}",
                generatesPreview = true,
                passes = new PassCollection
                {
                    { SpriteLitPasses.Lit },
                    { SpriteLitPasses.Normal },
                    { SpriteLitPasses.Forward },
                },
            };
        }
        #endregion

        #region Passes
        static class SpriteLitPasses
        {
            public static PassDescriptor Lit = new PassDescriptor
            {
                // Definition
                displayName = "Sprite Lit",
                referenceName = "SHADERPASS_SPRITELIT",
                lightMode = "Universal2D",
                useInPreview = true,

                // Template
                passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = SpriteLitBlockMasks.FragmentLit,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = SpriteLitRequiredFields.Lit,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.Default,
                pragmas = CorePragmas._2DDefault,
                keywords = SpriteLitKeywords.Lit,
                includes = SpriteLitIncludes.Lit,
            };

            public static PassDescriptor Normal = new PassDescriptor
            {
                // Definition
                displayName = "Sprite Normal",
                referenceName = "SHADERPASS_SPRITENORMAL",
                lightMode = "NormalsRendering",
                useInPreview = true,

                // Template
                passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = SpriteLitBlockMasks.FragmentForwardNormal,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = SpriteLitRequiredFields.Normal,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.Default,
                pragmas = CorePragmas._2DDefault,
                includes = SpriteLitIncludes.Normal,
            };

            public static PassDescriptor Forward = new PassDescriptor
            {
                // Definition
                displayName = "Sprite Forward",
                referenceName = "SHADERPASS_SPRITEFORWARD",
                lightMode = "UniversalForward",
                useInPreview = true,

                // Template
                passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = SpriteLitBlockMasks.FragmentForwardNormal,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = SpriteLitRequiredFields.Forward,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.Default,
                pragmas = CorePragmas._2DDefault,
                includes = SpriteLitIncludes.Forward,
            };
        }
        #endregion

        #region PortMasks
        static class SpriteLitBlockMasks
        {
            public static BlockFieldDescriptor[] FragmentLit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescriptionLegacy.SpriteColor,
                BlockFields.SurfaceDescription.Alpha,
                UniversalBlockFields.SurfaceDescription.SpriteMask,
            };

            public static BlockFieldDescriptor[] FragmentForwardNormal = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescriptionLegacy.SpriteColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.NormalTS,
            };
        }
        #endregion

        #region RequiredFields
        static class SpriteLitRequiredFields
        {
            public static FieldCollection Lit = new FieldCollection()
            {
                StructFields.Varyings.color,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.screenPosition,
            };

            public static FieldCollection Normal = new FieldCollection()
            {
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
            };

            public static FieldCollection Forward = new FieldCollection()
            {
                StructFields.Varyings.color,
                StructFields.Varyings.texCoord0,
            };
        }
        #endregion

        #region Keywords
        static class SpriteLitKeywords
        {
            public static KeywordCollection Lit = new KeywordCollection
            {
                { CoreKeywordDescriptors.ShapeLightType0 },
                { CoreKeywordDescriptors.ShapeLightType1 },
                { CoreKeywordDescriptors.ShapeLightType2 },
                { CoreKeywordDescriptors.ShapeLightType3 },
            };
        }
        #endregion

        #region Includes
        static class SpriteLitIncludes
        {
            const string k2DLightingUtil = "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl";
            const string k2DNormal = "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl";
            const string kSpriteLitPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteLitPass.hlsl";
            const string kSpriteNormalPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteNormalPass.hlsl";
            const string kSpriteForwardPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteForwardPass.hlsl";

            public static IncludeCollection Lit = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { k2DLightingUtil, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kSpriteLitPass, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Normal = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { k2DNormal, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kSpriteNormalPass, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kSpriteForwardPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
