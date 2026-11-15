// 바닥 핏웅덩이 (2026-11-15 사용자 결정 "기존의 피타일은 제거하고 핏웅덩이 몇군데" — 설계서 7.3 0층 바닥 바로 뒤).
// 웅덩이마다 바닥 위 사각형 하나(가로 2r × 세로 r — 아이소 바닥 타원). 텍스처 없이 픽셀마다:
//  ① 모양 = 타원 거리 + fbm 노이즈 흔들림 → 부드러운 가장자리, 둘레에 작은 핏방울(노이즈 문턱)
//  ② 색 = 가운데로 갈수록 짙은 검붉음, 가장자리는 조금 밝게(얇게 고인 피), 위 왼쪽에 젖은 번들거림
// 이 그림은 빛·안개 곱하기(6층) 앞이라 어둠 속에서는 거의 안 보이고 주인공 빛이 닿을 때 드러난다.
// 정점 색: R = 모양 씨앗(0..1), A = 진하기. 출력은 SpriteBatch AlphaBlend 에 맞게 미리 곱한 알파.
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_3
    #define PS_SHADERMODEL ps_4_0_level_9_3
#endif

float4x4 MatrixTransform;
// 밟기 물결(2026-11-15 "물웅덩이 밟는 효과") — 웅덩이마다 최근 4개: (자리 x, 자리 y(-1..1), 생긴 때(초), 안 씀). 아직 없는 칸은 생긴 때가 아주 과거
float Time;
float4 Ripples[4];
#define RIPPLE_LIFE 1.2
#define RIPPLE_SPEED 0.85

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
    float a = Hash(i);
    float b = Hash(i + float2(1.0, 0.0));
    float c = Hash(i + float2(0.0, 1.0));
    float d = Hash(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float Fbm(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int k = 0; k < 4; k++)
    {
        v += a * Noise(p);
        p = p * 2.03 + float2(17.0, 9.0);
        a *= 0.5;
    }
    return v;
}

float4 MainPS(VSOut input) : COLOR0
{
    float seed = input.Color.r * 97.0;
    float2 p = input.Tex * 2.0 - 1.0;                      // 사각형 안 -1..1 = 바닥 타원

    // ③ 밟기 물결: 밟은 자리에서 퍼지는 고리 둘(앞 고리 + 뒤따르는 약한 고리) — 고리에서 표면을 밀고(굴절) 번들거림을 더한다
    float ring = 0.0;
    float2 push = float2(0.0, 0.0);
    float splash = 0.0;
    for (int k = 0; k < 4; k++)
    {
        float age = Time - Ripples[k].z;
        if (age >= 0.0 && age < RIPPLE_LIFE)
        {
            float2 v = p - Ripples[k].xy;
            float dist = length(v);
            float life = 1.0 - age / RIPPLE_LIFE;
            float r1 = age * RIPPLE_SPEED;
            float w = 0.035 + age * 0.05;
            float g1 = exp(-((dist - r1) * (dist - r1)) / (w * w));
            float r2 = max(0.0, r1 - 0.16);
            float g2 = exp(-((dist - r2) * (dist - r2)) / (w * w)) * 0.5;
            float g = (g1 + g2) * life * life;
            ring += g;
            push += (dist > 0.0001 ? v / dist : float2(0.0, 0.0)) * g * 0.06;
            splash += (1.0 - smoothstep(0.0, 0.12, dist)) * saturate(1.0 - age / 0.18);   // 밟은 순간 발밑이 짙게 눌림
        }
    }
    p -= push;

    float n = Fbm(p * 2.3 + float2(seed * 1.7, seed * 3.9));
    float d = length(p) + (n - 0.5) * 0.75;                 // 흔들린 거리

    float body = 1.0 - smoothstep(0.50, 0.62, d);
    float dn = Noise(p * 9.0 + float2(seed * 5.3, seed * 2.1));
    float drops = step(0.82, dn) * (1.0 - smoothstep(0.80, 0.95, length(p))) * (1.0 - body);
    float a = max(body, drops * 0.8);
    clip(a - 0.002);

    float depth = saturate(1.0 - d / 0.62);
    float3 col = lerp(float3(0.36, 0.035, 0.03), float3(0.17, 0.008, 0.008), depth);
    float rim = smoothstep(0.36, 0.56, d) * (1.0 - smoothstep(0.56, 0.62, d));
    col += float3(0.10, 0.015, 0.012) * rim;                // 얇게 고인 가장자리
    float hl = 1.0 - smoothstep(0.0, 0.32, length(p - float2(-0.22, -0.30)) + (n - 0.5) * 0.25);
    col += float3(0.20, 0.07, 0.06) * hl * body;            // 젖은 번들거림
    col += float3(0.30, 0.10, 0.09) * saturate(ring) * body; // 물결 고리 번들거림
    col *= 1.0 - 0.45 * saturate(splash) * body;

    a *= input.Color.a;
    return float4(col * a, a);
}

technique BloodPool
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
