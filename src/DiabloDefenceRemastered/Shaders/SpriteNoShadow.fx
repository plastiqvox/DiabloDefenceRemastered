// 0~5층 그리기(설계서 7.7 — 2026-11-13 셰이더 그림자): SpriteBatch 기본 그리기와 같고, 원작 그림에 구워진 그림자 픽셀만 버린다.
// 원작 그림자 = 팔레트 색인 0 을 "검정 × 알파 0.5" 로 구운 픽셀(가져오기 shadow_alpha). 몸은 알파 1 이라
// "그림 알파 < 0.7 이면서 거의 검정" 인 픽셀만 버리면 그림자만 빠진다 — 빛 텍스처처럼 흰색에 부드러운 알파인 그림은 그대로.
// 대신 그림자는 UnitShadow.fx 가 빛 반대쪽으로 다시 드리운다.
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
    float4 c = tex2D(SpriteSampler, input.Tex);
    clip(c.a < 0.7 && max(c.r, max(c.g, c.b)) < 0.02 ? -1 : 1);
    return c * input.Color;
}

technique Sprite
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
