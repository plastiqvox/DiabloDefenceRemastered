// 바닥 예고 불빛 (2026-11-15 사용자 요청 "붉은 궤적이 여전히 그림판으로 그린 느낌", "바닥에서 붉은 불빛이 나는 느낌으로").
// 예고 하나 = 둘레까지 넉넉한 사각형 하나. 텍스처 없이 픽셀마다 모양을 계산한다:
//  ① 원(G < 0.5): 사각형 -1..1 의 반지름 × CircleSpill — 1 이 판정 경계. 막대(G ≥ 0.5): u = 길이 방향, v = 폭 방향(사각형 폭 = 막대 폭 × BarSpill)
//  ② d = 경계 밖 거리(경계 단위, 안쪽은 0 이하) → 안쪽 은은한 빛 + 진행(R)만큼 더 밝게, 경계에 밝은 테, 바깥으로 지수 번짐
//  ③ 불빛 일렁임: 시간에 따라 흐르는 fbm 노이즈로 밝기를 흔든다
// Mode 0 = 가산(빛 지도·발광 — Tint × 값), 1 = 필터(빛 지도 곱하기 — (1 − Tint) × 값, 블렌드가 빛 × (1 − 이 값))
// 정점 색: R = 진행, G = 모양(0 원·1 막대), B = 막대 길이 / 사각형 길이, A = 세기
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_3
    #define PS_SHADERMODEL ps_4_0_level_9_3
#endif

float4x4 MatrixTransform;
float3 Tint;
float Time;
float Mode;
#define CIRCLE_SPILL 1.45
#define BAR_SPILL 3.0

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

float Hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(Hash(i), Hash(i + float2(1.0, 0.0)), u.x), lerp(Hash(i + float2(0.0, 1.0)), Hash(i + float2(1.0, 1.0)), u.x), u.y);
}

float Fbm(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int k = 0; k < 3; k++)
    {
        v += a * Noise(p);
        p = p * 2.07 + float2(13.0, 7.0);
        a *= 0.5;
    }
    return v;
}

float4 MainPS(VSOut input) : COLOR0
{
    float progress = input.Color.r;
    float bar = input.Color.g;
    float strength = input.Color.a * 4.0;   // 정점 알파 = 세기 / 4
    float2 uv = input.Tex;

    float d;          // 경계 밖 거리(경계 단위)
    float filled;     // 진행 안이면 1
    float2 np;        // 노이즈 자리
    if (bar < 0.5)
    {
        float r = length(uv * 2.0 - 1.0) * CIRCLE_SPILL;
        d = r - 1.0;
        filled = r <= progress ? 1.0 : 0.0;
        np = uv * 6.0;
    }
    else
    {
        float lenFrac = max(input.Color.b, 0.01);
        float pad = (1.0 - lenFrac) * 0.5;
        float s = (uv.x - pad) / lenFrac;                   // 0..1 = 막대 안 길이 방향
        float across = abs(uv.y - 0.5) * BAR_SPILL * 2.0;    // 1 = 막대 폭 경계
        float along = max(max(-s, s - 1.0), 0.0) / max(lenFrac, 0.01) * 0.5 * BAR_SPILL;   // 끝 밖 거리(대략 폭 단위)
        d = max(across - 1.0, along);
        filled = (s >= 0.0 && s <= progress) ? 1.0 : 0.0;
        np = float2(uv.x * 10.0 / max(lenFrac, 0.2), uv.y * 3.0);
    }

    float flick = 0.75 + 0.5 * Fbm(np + float2(Time * 0.9, -Time * 1.3));   // 불빛 일렁임
    float inside = d <= 0.0 ? (0.35 + 0.45 * filled) * flick : 0.0;
    float rim = exp(-(d * d) / 0.006) * (0.6 + 0.2 * flick);
    float spill = exp(-max(d, 0.0) * 5.0) * 0.45 * flick * (d > 0.0 ? 1.0 : 0.0);
    float v = (inside + rim + spill) * strength;

    float3 c = Mode < 0.5 ? Tint * v : (1.0 - Tint) * saturate(v);
    return float4(c, 1.0);
}

technique GroundGlow
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
