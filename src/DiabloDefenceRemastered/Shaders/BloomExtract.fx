// 블룸 밝은 부분 뽑기(2026-11-14 사용자 요청 "블룸 효과", 설계서 7.7 ⑦b): 빛을 곱한 WorldRT 를 줄여 그리며 밝기가 Threshold 를 넘는 만큼만 남긴다.
// 부드러운 무릎(Knee) — 문턱 근처가 뚝 끊기지 않게 문턱 − Knee ~ 문턱 + Knee 를 2차 곡선으로 잇는다. 색은 그대로 비율만 줄인다(불은 주황으로 번짐).
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_3
    #define PS_SHADERMODEL ps_4_0_level_9_3
#endif

float4x4 MatrixTransform;
float Threshold;
float Knee;

texture SpriteTexture;
sampler2D SpriteSampler : register(s0) = sampler_state { Texture = <SpriteTexture>; };

struct VSOut
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 Tex : TEXCOORD0;
};

VSOut MainVS(float4 position : POSITION0, float4 color : COLOR0, float2 tex : TEXCOORD0)
{
    VSOut o;
    o.Position = mul(position, MatrixTransform);
    o.Color = color;
    o.Tex = tex;
    return o;
}

float4 MainPS(VSOut input) : COLOR0
{
    float3 c = tex2D(SpriteSampler, input.Tex).rgb;
    float b = max(c.r, max(c.g, c.b));
    float soft = clamp(b - Threshold + Knee, 0, 2 * Knee);
    soft = soft * soft / (4 * Knee + 0.0001);
    float w = max(soft, b - Threshold) / max(b, 0.0001);
    return float4(c * w, 1) * input.Color;
}

technique Sprite
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
