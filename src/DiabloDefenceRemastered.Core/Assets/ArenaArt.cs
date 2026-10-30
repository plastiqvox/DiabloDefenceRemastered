using System.Text.Json;
using System.Text.Json.Serialization;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Core.Assets;

/// <summary>칸 그림 종류(<c>arena.json</c> 의 <c>cells</c>).</summary>
public enum ArenaCellKind : byte
{
    /// <summary>막힌 칸의 벽 기둥.</summary>
    Wall = 0,
    /// <summary>걸을 수 있는 칸의 바닥 위 조각(덤불·아치 윗부분).</summary>
    FloorPiece = 1,
    /// <summary>아치 덧그림(L1S).</summary>
    Overlay = 2,
}

/// <summary>
/// 맵 그림 목록 <c>&lt;맵 art&gt;/arena.json</c> (설계서 7.6, 계약 WP1b). 가져오기 도구(WP0)가 쓰고 렌더(WP5)·시작 검사(WP7)·13.5-10 이 읽는다.
/// <code>
/// { "format": 1, "map": "cathedral_arena", "layout_hash": "…", "box": [2560, 1408],
///   "floor": [ { "file": "floor_0_0.png", "pos": [0, 0] }, … ],
///   "columns": "columns",
///   "cells": [ [tx, ty, kind, frame], … ] }
/// </code>
/// <c>floor</c> = <see cref="FloorChunks"/> 그대로(이름·자리), <c>columns</c> = 기둥 아틀라스 이름(<c>columns.png</c> + <c>columns.json</c>, SpriteMeta 모양·동작 하나·방향 1),
/// <c>cells</c> 는 (ty, tx, kind) 순 정렬, <b>frame 은 1부터</b>(6.1). 같은 칸에 kind 1 과 2 가 같이 있을 수 있다.
/// </summary>
public sealed record ArenaArt
{
    public const int CurrentFormat = 1;
    /// <summary>바닥 조각 한 변(px).</summary>
    public const int ChunkSize = 512;
    public const string FileName = "arena.json";
    public const string MinimapFile = "minimap.png";

    public int Format { get; init; }
    public required string Map { get; init; }
    public required string LayoutHash { get; init; }
    /// <summary>외곽 상자(<c>IsoMath.BoxSize</c>).</summary>
    public Int2 Box { get; init; }
    public IReadOnlyList<ArenaFloorChunk> Floor { get; init; } = [];
    public string Columns { get; init; } = "columns";
    public IReadOnlyList<ArenaCell> Cells { get; init; } = [];

    public static ArenaArt Read(string path) => AssetJson.Read<ArenaArt>(path);
    public static ArenaArt Parse(string json) => AssetJson.Deserialize<ArenaArt>(json);

    /// <summary>
    /// 바닥 조각 이름 규칙(1.4 "아레나", 자원 없이 계산): 조각 (cx, cy) 는 성질 <c>.</c> 인 칸의 마름모 상자
    /// <c>[C.x−32, C.x+32) × [C.y−16, C.y+16)</c> 가 하나라도 <c>[512cx, 512cx+512) × [512cy, 512cy+512)</c> 와 겹치면 있다.
    /// 결과는 (cy, cx) 순. 픽셀이 모두 투명이어도 규칙이 있다고 하면 쓴다.
    /// </summary>
    public static IReadOnlyList<Int2> FloorChunks(ArenaGrid grid, IsoMath iso)
    {
        var set = new SortedSet<Int2>(Comparer<Int2>.Create((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X)));
        for (var ty = 0; ty < grid.Size; ty++)
            for (var tx = 0; tx < grid.Size; tx++)
            {
                if ((grid.Flags(tx, ty) & (CellFlags.BlocksWalk | CellFlags.BlocksProjectiles)) != 0) continue;
                var c = iso.CellCenter(tx, ty);
                // 반열린 [a, b) 와 겹치는 조각: floor(a/512) .. ceil(b/512) − 1
                int x0 = FloorDiv((int)c.X - IsoMath.HalfWidth), x1 = CeilDiv((int)c.X + IsoMath.HalfWidth) - 1;
                int y0 = FloorDiv((int)c.Y - IsoMath.HalfHeight), y1 = CeilDiv((int)c.Y + IsoMath.HalfHeight) - 1;
                for (var cy = y0; cy <= y1; cy++)
                    for (var cx = x0; cx <= x1; cx++)
                        set.Add(new Int2(cx, cy));
            }
        return [.. set];
    }

    public static IReadOnlyList<Int2> FloorChunks(ArenaGrid grid) => FloorChunks(grid, grid.Iso);

    /// <summary>조각 파일 이름 <c>floor_&lt;cx&gt;_&lt;cy&gt;.png</c>.</summary>
    public static string FloorChunkFile(Int2 chunk) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"floor_{chunk.X}_{chunk.Y}.png");

    /// <summary>조각 왼쪽 위(월드 px) = (512cx, 512cy).</summary>
    public static Int2 FloorChunkPos(Int2 chunk) => new(chunk.X * ChunkSize, chunk.Y * ChunkSize);

    /// <summary>
    /// 맵 그림 폴더에 가져오기가 만드는 파일 이름(Assets 뿌리 기준, 13.5-10·시작 검사 ③): arena.json·columns.png·columns.json·minimap.png·floor_*.
    /// </summary>
    public static IReadOnlyList<string> OutputNames(ArenaGrid grid, string columns = "columns")
    {
        var art = grid.Map.Art.TrimEnd('/');
        var names = new List<string>
        {
            $"{art}/{FileName}",
            $"{art}/{columns}.png",
            $"{art}/{columns}.json",
            $"{art}/{MinimapFile}",
        };
        foreach (var chunk in FloorChunks(grid)) names.Add($"{art}/{FloorChunkFile(chunk)}");
        names.Sort(StringComparer.Ordinal);
        return names;
    }

    /// <summary>
    /// 검사: 형식 판, <c>columns</c> 이름, 맵 ID·<c>layout_hash</c>·상자가 저장소 배치와 같음, <c>floor</c> = <see cref="FloorChunks"/>, <c>cells</c> 의 칸이 격자 안·종류 0..2·frame ≥ 1·(ty, tx, kind) 순.
    /// </summary>
    public IReadOnlyList<string> Validate(ArenaGrid grid)
    {
        var errors = new List<string>();
        if (Format != CurrentFormat) errors.Add($"arena.json 형식 {Format} ≠ {CurrentFormat}");
        if (!AssetFormat.IsValidRelativePath(Columns) || Columns.Contains('/'))
            errors.Add($"기둥 아틀라스 이름 '{Columns}' 는 맵 그림 폴더 안 파일 이름이다(구분자·'..' 없음)");
        if (!string.Equals(Map, grid.Map.Id, StringComparison.Ordinal)) errors.Add($"맵 '{Map}' ≠ '{grid.Map.Id}'");
        if (!string.Equals(LayoutHash, grid.LayoutHash, StringComparison.Ordinal))
            errors.Add($"맵 배치가 바뀌었다(layout_hash {LayoutHash} ≠ {grid.LayoutHash}) — 가져오기를 다시 돌린다");
        var box = grid.Iso.BoxSize;
        if (Box != new Int2((int)box.X, (int)box.Y)) errors.Add($"상자 {Box} ≠ [{box.X},{box.Y}]");

        var chunks = FloorChunks(grid);
        if (Floor.Count != chunks.Count) errors.Add($"바닥 조각 {Floor.Count}개 ≠ 규칙 {chunks.Count}개");
        else
            for (var i = 0; i < chunks.Count; i++)
                if (Floor[i].File != FloorChunkFile(chunks[i]) || Floor[i].Pos != FloorChunkPos(chunks[i]))
                    errors.Add($"floor[{i}] {Floor[i].File} {Floor[i].Pos} ≠ {FloorChunkFile(chunks[i])} {FloorChunkPos(chunks[i])}");

        for (var i = 0; i < Cells.Count; i++)
        {
            var c = Cells[i];
            if (!grid.IsInside(c.Tx, c.Ty)) errors.Add($"cells[{i}] ({c.Tx},{c.Ty}) 가 격자 밖이다");
            if (c.Kind > ArenaCellKind.Overlay) errors.Add($"cells[{i}] 종류 {(int)c.Kind} 는 0..2");
            if (c.Frame < 1) errors.Add($"cells[{i}] frame {c.Frame} — 프레임 번호는 1부터");
            if (i > 0 && Compare(Cells[i - 1], c) >= 0) errors.Add($"cells[{i}] 가 (ty, tx, kind) 순이 아니다");
        }
        return errors;
    }

    private static int Compare(ArenaCell a, ArenaCell b) =>
        a.Ty != b.Ty ? a.Ty.CompareTo(b.Ty) : a.Tx != b.Tx ? a.Tx.CompareTo(b.Tx) : a.Kind.CompareTo(b.Kind);

    private static int FloorDiv(int v) => (int)Math.Floor(v / (double)ChunkSize);
    private static int CeilDiv(int v) => (int)Math.Ceiling(v / (double)ChunkSize);
}

public sealed record ArenaFloorChunk
{
    public required string File { get; init; }
    /// <summary>월드 px 왼쪽 위 = (512cx, 512cy).</summary>
    public Int2 Pos { get; init; }
}

/// <summary>칸 그림 하나 <c>[tx, ty, kind, frame]</c>(frame 1부터, 기둥 아틀라스 안 프레임).</summary>
[JsonConverter(typeof(ArenaCellJsonConverter))]
public readonly record struct ArenaCell(int Tx, int Ty, ArenaCellKind Kind, int Frame);

public sealed class ArenaCellJsonConverter : JsonConverter<ArenaCell>
{
    private const string What = "tx,ty,kind,frame";

    public override ArenaCell Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DefJson.BeginArray(ref reader, What);
        var tx = DefJson.ReadInt(ref reader, What);
        var ty = DefJson.ReadInt(ref reader, What);
        var kind = DefJson.ReadInt(ref reader, What);
        var frame = DefJson.ReadInt(ref reader, What);
        DefJson.EndArray(ref reader, What);
        if (kind is < 0 or > 255) throw new JsonException($"{What}: 종류 {kind} 가 틀렸다");
        return new ArenaCell(tx, ty, (ArenaCellKind)kind, frame);
    }

    public override void Write(Utf8JsonWriter writer, ArenaCell v, JsonSerializerOptions options) =>
        writer.WriteRawValue(string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"[{v.Tx},{v.Ty},{(int)v.Kind},{v.Frame}]"), skipInputValidation: true);
}
