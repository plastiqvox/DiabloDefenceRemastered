// 빛 하나 더하기 (설계서 7.7 ② — 2026-11-13 셰이더 빛). 빛 둘레 사각형 하나를 그리며 픽셀마다:
//  ① 월드 자리 = RectMin + 텍스처 좌표 × RectSize
//  ② 바닥 자리 = 바닥 y 버퍼에 값이 있으면 (월드 x, 그 y) — 벽·유닛은 서 있는 바닥에서 빛을 받는다, 없으면 월드 자리(바닥)
//  ③ 감쇠 = smoothstep(1 − 거리/반경) — 예전 빛 텍스처(Primitives.Light)와 같은 곡선
//  ④ 벽 그림자 = 바닥 자리 → 빛 사이 칸 막힘 지도(선형 표본 — 부드러운 가장자리)를 16번 보고 가장 큰 막힘 × ShadowStrength 만큼 어둡게(은은하게)
// 출력 = 색 × 감쇠 × 그림자 — 더하기 혼합(One, One).
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_3
    #define PS_SHADERMODEL ps_4_0_level_9_3
#endif

float4x4 MatrixTransform;
float2 RectMin;          // 월드 px
float2 RectSize;
float2 Camera;           // 월드 px(흔들림 포함)
float2 ViewSize;         // 월드 px(960×540)
float2 LightPos;         // 월드 px
float LightRadius;
float3 LightColor;       // 0..1 × 세기
float4 Iso;              // (32N, H + 16, N, 쓰지 않음)
float ShadowStrength;    // 0 = 그림자 없음, 1 = 완전히 막음
float ShadowSkip;        // 시작·끝에서 건너뛸 거리(월드 px) — 벽 자기 칸·벽에 붙은 횃불이 스스로를 가리지 않게

// 0번 칸은 SpriteBatch 가 패스 적용 뒤 늘 그리는 텍스처(1px 흰색)로 덮는다 — 바닥·막힘 지도는 1·2번 칸에 고정하고, 0번 샘플러는 지워지지 않게 출력에 곱한다(값 1)
texture SpriteTexture;
sampler2D SpriteSampler : register(s0) = sampler_state { Texture = <SpriteTexture>; };

texture GroundTexture;
sampler2D GroundSampler : register(s1) = sampler_state
{
    Texture = <GroundTexture>;
    MinFilter = Point; MagFilter = Point; MipFilter = Point;
    AddressU = Clamp; AddressV = Clamp;
};

texture OccTexture;
sampler2D OccSampler : register(s2) = sampler_state
{
    Texture = <OccTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Point;
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

float2 CellUv(float2 world)
{
    float u = (world.x - Iso.x) / 32.0;
    float v = (world.y - Iso.y) / 16.0;
    float2 t = float2((v + u) * 0.5, (v - u) * 0.5);
    return (t + 0.5) / Iso.z;
}

float4 MainPS(VSOut input) : COLOR0
{
    float2 world = RectMin + input.Tex * RectSize;
    float2 screenUv = (world - Camera) / ViewSize;
    float4 g = tex2Dlod(GroundSampler, float4(screenUv, 0, 0));
    float2 ground = world;
    if (g.a > 0.5)
        ground.y = (g.r * 255.0 * 256.0 + g.g * 255.0) / 16.0 - 512.0 + Camera.y;

    float2 d = LightPos - ground;
    float dist = length(d);
    float v = saturate(1.0 - dist / LightRadius);
    float fall = v * v * (3.0 - 2.0 * v);
    if (fall <= 0.0)
        return float4(0, 0, 0, 0);

    float occ = 0.0;
    float span = dist - 2.0 * ShadowSkip;
    if (span > 0.0)
    {
        for (int i = 0; i < 16; i++)
        {
            float s = ShadowSkip + span * (i + 0.5) / 16.0;
            float2 p = ground + d * (s / dist);
            occ = max(occ, tex2Dlod(OccSampler, float4(CellUv(p), 0, 0)).r);
        }
    }
    float shade = 1.0 - ShadowStrength * occ;
    float keep = tex2Dlod(SpriteSampler, float4(input.Tex, 0, 0)).a;
    return float4(LightColor * (fall * shade * keep), 0);
}

technique Light
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
