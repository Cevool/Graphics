#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

struct Attributes
{
    uint vertexID : SV_VertexID;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float occlusion : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

sampler2D _FlareTex;
TEXTURE2D_X(_FlareOcclusionBufferTex);

float4 _FlareColorValue;
float4 _FlareData0; // x: localCos0, y: localSin0, zw: PositionOffsetXY
float4 _FlareData1; // x: OcclusionRadius, y: OcclusionSampleCount, z: ScreenPosZ, w: Falloff
float4 _FlareData2; // xy: ScreenPos, zw: FlareSize
float4 _FlareData3; // xy: RayOffset, z: invSideCount
float4 _FlareData4; // x: SDF Roundness, y: SDF Frequency
float4 _FlareData5; // x: Allow Offscreen, y: Edge Offset

#define _FlareColor             _FlareColorValue

#define _LocalCos0              _FlareData0.x
#define _LocalSin0              _FlareData0.y
#define _PositionOffset         _FlareData0.zw

#define _OcclusionRadius        _FlareData1.x
#define _OcclusionSampleCount   _FlareData1.y
#define _ScreenPosZ             _FlareData1.z
#define _FlareFalloff           _FlareData1.w

#define _ScreenPos              _FlareData2.xy
#define _FlareSize              _FlareData2.zw

#define _FlareRayOffset         _FlareData3.xy
#define _FlareShapeInvSide      _FlareData3.z

#define _FlareSDFRoundness      _FlareData4.x
#define _FlareSDFPolyRadius     _FlareData4.y
#define _FlareSDFPolyParam0     _FlareData4.z
#define _FlareSDFPolyParam1     _FlareData4.w

#define _OcclusionOffscreen     _FlareData5.x
#define _FlareEdgeOffset        _FlareData5.y

float2 Rotate(float2 v, float cos0, float sin0)
{
    return float2(v.x * cos0 - v.y * sin0,
                  v.x * sin0 + v.y * cos0);
}

float GetOcclusion(float2 screenPos, float flareDepth, float ratio)
{
    if (_OcclusionSampleCount == 0.0f)
        return 1.0f;

    float contrib = 0.0f;
    float sample_Contrib = 1.0f / _OcclusionSampleCount;
    float2 ratioScale = float2(1.0f / ratio, 1.0);

    for (uint i = 0; i < (uint)_OcclusionSampleCount; i++)
    {
        float2 dir = _OcclusionRadius * SampleDiskUniform(Hash(2 * i + 0 + 1), Hash(2 * i + 1 + 1));
        float2 pos = screenPos + dir;
        pos.xy = pos * 0.5f + 0.5f;
        pos.y = 1.0f - pos.y;
        if (all(pos >= 0) && all(pos <= 1))
        {
            float depth0 = LinearEyeDepth(SampleCameraDepth(pos), _ZBufferParams);
            if (flareDepth < depth0)
                contrib += sample_Contrib;
        }
        else if (_OcclusionOffscreen > 0.0f)
        {
            contrib += sample_Contrib;
        }
    }

    return contrib;
}

Varyings vert(Attributes input, uint instanceID : SV_InstanceID)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float screenRatio = _ScreenSize.y / _ScreenSize.x;

    float4 posPreScale = float4(2.0f, 2.0f, 1.0f, 1.0f) * GetQuadVertexPosition(input.vertexID % 6) - float4(1.0f, 1.0f, 0.0f, 0.0);
    output.texcoord = GetQuadTexCoord(input.vertexID % 6);

    posPreScale.xy *= _FlareSize;
    float2 local = Rotate(posPreScale.xy, _LocalCos0, _LocalSin0);

    local.x *= screenRatio;

    output.positionCS.xy = local + _ScreenPos + _FlareRayOffset + _PositionOffset;
    output.positionCS.zw = posPreScale.zw;

    float occlusion = GetOcclusion(_ScreenPos.xy, _ScreenPosZ, screenRatio);

    if (_OcclusionOffscreen < 0.0f && // No lens flare off screen
        (any(_ScreenPos.xy < -1) || any(_ScreenPos.xy >= 1)))
        occlusion *= 0.0f;

    output.occlusion = occlusion;

    return output;
}

float InverseGradient(float x)
{
    // Do *not* simplify as 1.0f - x
    return x * (1.0f - x) / (x + 1e-6f);
}

float4 ComputeCircle(float2 uv)
{
    float2 v = (uv - 0.5f) * 2.0f;

    float x = length(v);

    float sdf = saturate((x - 1.0f) / (_FlareEdgeOffset - 1.0f));

#if FLARE_INVERSE_SDF
    sdf = InverseGradient(sdf);
#endif

    return pow(sdf, _FlareFalloff);
}

// Modfied from ref: https://www.shadertoy.com/view/MtKcWW
// https://www.shadertoy.com/view/3tGBDt
float4 ComputePolygon(float2 uv_)
{
    float2 p = uv_ * 2.0f - 1.0f;

    float r = _FlareSDFPolyRadius;
    float an = _FlareSDFPolyParam0;
    float he = _FlareSDFPolyParam1;

    float bn = an * floor((atan2(p.y, p.x) + 0.5f * an) / an);
    float cos0 = cos(bn);
    float sin0 = sin(bn);
    p = float2( cos0 * p.x + sin0 * p.y,
               -sin0 * p.x + cos0 * p.y);

    // side of polygon
    float sdf = length(p - float2(r, clamp(p.y, -he, he))) * sign(p.x - r) - _FlareSDFRoundness;

    sdf *= _FlareEdgeOffset;

#if FLARE_INVERSE_SDF
    sdf = saturate(-sdf);
    sdf = InverseGradient(sdf);
#else
    sdf = saturate(-sdf);
#endif

    return saturate(pow(sdf, _FlareFalloff));
}

float4 GetFlareShape(float2 uv)
{
#if FLARE_CIRCLE
    return ComputeCircle(uv);
#elif FLARE_POLYGON
    return ComputePolygon(uv);
#elif FLARE_SHIMMER
    return ComputeShimmer(uv);
#else
    return tex2D(_FlareTex, uv);
#endif
}
