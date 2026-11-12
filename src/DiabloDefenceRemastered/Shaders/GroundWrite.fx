// 바닥 y 버퍼 쓰기 (설계서 7.7 — 2026-11-13 셰이더 빛). 벽·유닛 그림의 불투명 픽셀에 "이 픽셀이 서 있는 바닥 y" 를 쓴다.
// 정점 색 RG = 바닥 y 16비트(위·아래 바이트 — ShaderLight.EncodeGround), 출력 A = 1 이 "값 있음". 투명 픽셀은 버린다(뒤에 그린 것이 앞).
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_3
    #define PS_SHADERMODEL ps_4_0_level_9_3
#endif

float4x4 MatrixTransform;

texture SpriteTexture;
sampler2D SpriteSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Point; MagFilter = Point; MipFilter = Point;
    AddressU = Clamp; AddressV = Clamp;
};

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
    float a = tex2D(SpriteSampler, input.Tex).a;
    clip(a - 0.5);
    return float4(input.Color.rg, 0, 1);
}

technique Ground
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
