// 틸트 시프트 + 비네트 (2026-11-15 사용자 요청 "HD-2D게임들의 기법을 조금" — 설계서 7.7 ⑦c, 블룸 뒤·8층 화면 효과 앞).
//  ① 초점 띠: 주인공 화면 높이(Focus)에서 위아래로 Band 까지는 또렷, 그 밖은 거리만큼 흐림(최대 MaxBlur 타깃 px) — 미니어처 디오라마 느낌
//  ② 흐림 = 원판 12점(포아송) 평균 — 흐림 반경이 0 이면 원본 한 점
//  ③ 비네트: 화면 가운데에서 모서리로 갈수록 어둡게(Vignette 만큼)
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_3
    #define PS_SHADERMODEL ps_4_0_level_9_3
#endif

float4x4 MatrixTransform;
float2 TexelSize;     // 1 / 타깃 크기
float Focus;          // 초점 높이(0..1 — 주인공 화면 y)
float Band;           // 또렷한 띠 반폭(화면 높이 비율)
float Ramp;           // 띠 끝에서 최대 흐림까지 거리(화면 높이 비율)
float MaxBlur;        // 최대 흐림 반경(타깃 px)
float Vignette;       // 모서리 어둡기(0..1)
float Aspect;         // 가로 / 세로

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

static const float2 Disk[12] =
{
    float2(-0.326, -0.406), float2(-0.840, -0.074), float2(-0.696, 0.457), float2(-0.203, 0.621),
    float2(0.962, -0.195), float2(0.473, -0.480), float2(0.519, 0.767), float2(0.185, -0.893),
    float2(0.507, 0.064), float2(0.896, 0.412), float2(-0.322, -0.933), float2(-0.792, -0.598)
};

float4 MainPS(VSOut input) : COLOR0
{
    float2 uv = input.Tex;
    float dy = abs(uv.y - Focus);
    float amount = smoothstep(Band, Band + Ramp, dy);
    float radius = amount * MaxBlur;
    float4 c = tex2D(SpriteSampler, uv);
    if (radius > 0.25)
    {
        float4 sum = c;
        for (int i = 0; i < 12; i++)
            sum += tex2D(SpriteSampler, uv + Disk[i] * radius * TexelSize);
        c = sum / 13.0;
    }
    float2 v = (uv - 0.5) * float2(Aspect, 1.0);
    float d = length(v) / length(float2(Aspect, 1.0) * 0.5);    // 모서리 = 1
    c.rgb *= 1.0 - Vignette * smoothstep(0.35, 1.0, d);
    c.a = 1.0;
    return c * input.Color;
}

technique TiltShift
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
