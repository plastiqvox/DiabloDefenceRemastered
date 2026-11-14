// 블룸 흐리기(2026-11-14, 설계서 7.7 ⑦b): 한 방향 9탭 가우스를 선형 표본 5번으로(가운데 + 양쪽 1.385·3.231 텍셀). Step = 방향 × 텍셀 크기(가로·세로 두 번 그림).
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_3
    #define PS_SHADERMODEL ps_4_0_level_9_3
#endif

float4x4 MatrixTransform;
float2 Step;

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
    float2 t = input.Tex;
    float3 c = tex2D(SpriteSampler, t).rgb * 0.2270270;
    c += tex2D(SpriteSampler, t + Step * 1.3846154).rgb * 0.3162162;
    c += tex2D(SpriteSampler, t - Step * 1.3846154).rgb * 0.3162162;
    c += tex2D(SpriteSampler, t + Step * 3.2307692).rgb * 0.0702703;
    c += tex2D(SpriteSampler, t - Step * 3.2307692).rgb * 0.0702703;
    return float4(c, 1) * input.Color;
}

technique Sprite
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
