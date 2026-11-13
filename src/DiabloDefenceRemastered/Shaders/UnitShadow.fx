// 유닛 그림자(설계서 7.7 — 2026-11-13 셰이더 그림자): 빛 지도(LightRT)에 유닛 실루엣을 빛 반대쪽 바닥에 눕혀 찍어 빛을 깎는다.
// 사각형 네 꼭짓점은 C# 이 기울여 준다(ShaderLight.DrawShadows). 픽셀마다:
//  ① 실루엣 = 그림 알파 ≥ 0.7(몸 — 구운 그림자 픽셀 제외), Soft 면 그림 알파 × 2(발밑 접지 그림자의 빛 텍스처 — 가운데 평평)
//  ② 바닥 y 버퍼에 값이 있는 픽셀(벽·유닛 그림)은 버림 — 그림자는 바닥에만 진다
// 출력 알파 = 실루엣 × 정점 알파. 혼합은 dest × (1 − 알파)(겹치면 더 어두워짐).
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_3
    #define PS_SHADERMODEL ps_4_0_level_9_3
#endif

float4x4 MatrixTransform;
float2 Camera;           // 월드 px
float2 ViewSize;         // 월드 px(960×540)
float Soft;              // 1 = 그림 알파 그대로(접지 그림자), 0 = 몸 실루엣

texture SpriteTexture;
sampler2D SpriteSampler : register(s0) = sampler_state { Texture = <SpriteTexture>; };

texture GroundTexture;
sampler2D GroundSampler : register(s1) = sampler_state
{
    Texture = <GroundTexture>;
    MinFilter = Point; MagFilter = Point; MipFilter = Point;
    AddressU = Clamp; AddressV = Clamp;
};

struct VSOut
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 Tex : TEXCOORD0;
    float2 Screen : TEXCOORD1;
};

VSOut MainVS(float4 position : POSITION0, float4 color : COLOR0, float2 tex : TEXCOORD0)
{
    VSOut o;
    o.Position = mul(position, MatrixTransform);
    o.Color = color;
    o.Tex = tex;
    o.Screen = (position.xy - Camera) / ViewSize;
    return o;
}

float4 MainPS(VSOut input) : COLOR0
{
    float a = tex2D(SpriteSampler, input.Tex).a;
    a = Soft > 0.5 ? saturate(a * 2.0) : step(0.7, a);   // 접지 그림자: 가운데는 발에 가리니 평평하게 채우고 끝만 흐리게
    float g = tex2Dlod(GroundSampler, float4(input.Screen, 0, 0)).a;
    a *= input.Color.a * (1.0 - step(0.5, g));
    return float4(0, 0, 0, a);
}

technique Shadow
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
