namespace DiabloDefenceRemastered.Tests.Map;

public class ArenaLayoutTests
{
    private static ArenaGrid ExpandOk(MapDef map)
    {
        var errors = new List<LayoutError>();
        var grid = ArenaLayout.TryExpand(map, map.Tileset.Def, errors);
        Assert.Empty(errors);
        return grid!;
    }

    private static List<LayoutError> ExpandErrors(MapDef map)
    {
        var errors = new List<LayoutError>();
        Assert.Null(ArenaLayout.TryExpand(map, map.Tileset.Def, errors));
        Assert.NotEmpty(errors);
        return errors;
    }

    [Fact]
    public void Border_EightPlaces()
    {
        var g = ExpandOk(TestDefs.Map(12, place: []));
        Assert.Equal(TestDefs.CornerN, g.MegaTile(0, 0));
        Assert.Equal(TestDefs.CornerE, g.MegaTile(11, 0));
        Assert.Equal(TestDefs.CornerS, g.MegaTile(11, 11));
        Assert.Equal(TestDefs.CornerW, g.MegaTile(0, 11));
        Assert.Equal(TestDefs.EdgeNe, g.MegaTile(2, 0));
        Assert.Equal(TestDefs.EdgeSe, g.MegaTile(11, 2));
        Assert.Equal(TestDefs.EdgeSw, g.MegaTile(2, 11));
        Assert.Equal(TestDefs.EdgeNw, g.MegaTile(0, 2));
        Assert.Equal(12, g.MegaSize);
        Assert.Equal(24, g.Size);
    }

    [Fact]
    public void Gates_OpenTwoMiddleTiles_WithEndPieces()
    {
        var g = ExpandOk(TestDefs.Map(12, place: []));
        // M 12 → 가운데 (M − 2)/2 = 5: 열린 칸 5·6, 끝 조각 4·7 → 하위 칸 틈 10..13, 중심 11.5 = (N − 1)/2
        foreach (var side in Enum.GetValues<GateSide>())
        {
            for (var k = 0; k < 12; k++)
            {
                var (mx, my) = ArenaLayout.SideCell(side, k, 12);
                var expected = k switch
                {
                    5 or 6 => TestDefs.GateOpen,
                    4 or 7 => TestDefs.GateEnd,
                    0 or 11 => g.MegaTile(mx, my),   // 모서리
                    _ => side switch { GateSide.Nw => TestDefs.EdgeNw, GateSide.Ne => TestDefs.EdgeNe, GateSide.Se => TestDefs.EdgeSe, _ => TestDefs.EdgeSw },
                };
                Assert.Equal(expected, g.MegaTile(mx, my));
            }
        }
        // 북서 문 틈(하위 칸 (0..1, 10..13))은 걸을 수 있고 둘레 표시가 없다
        for (var ty = 10; ty <= 13; ty++)
            for (var tx = 0; tx <= 1; tx++)
            {
                Assert.True(g.IsWalkable(tx, ty));
                Assert.False(g.IsBoundary(tx, ty));
            }
        // 끝 조각은 막히고 둘레
        Assert.True(g.BlocksWalk(0, 9) && g.IsBoundary(0, 9));
        Assert.True(g.BlocksWalk(1, 14) && g.IsBoundary(1, 14));
    }

    [Fact]
    public void Gates_M20_GapIsSubCells18To21_Center19_5()
    {
        // 문서 수치: M 20 → 열린 칸 9·10 → 하위 칸 틈 18..21, 중심 19.5 = (N − 1)/2
        var g = ExpandOk(TestDefs.Map(20, place: []));
        Assert.Equal(19.5f, (g.Size - 1) / 2f);
        for (var ty = 16; ty <= 23; ty++)
        {
            var open = ty is >= 18 and <= 21;
            Assert.Equal(open, g.IsWalkable(0, ty));
            Assert.Equal(open, g.IsWalkable(1, ty));
        }
    }

    [Fact]
    public void GateAt_MovesGate()
    {
        var m0 = TestDefs.Map(12, place: []);
        var g = ExpandOk(m0 with { Layout = m0.Layout with { Gates = [new GatePlacementDef { Id = "g", Side = GateSide.Ne, At = 2 }] } });
        Assert.Equal(TestDefs.GateEnd, g.MegaTile(1, 0));
        Assert.Equal(TestDefs.GateOpen, g.MegaTile(2, 0));
        Assert.Equal(TestDefs.GateOpen, g.MegaTile(3, 0));
        Assert.Equal(TestDefs.GateEnd, g.MegaTile(4, 0));
        Assert.Equal(TestDefs.EdgeNe, g.MegaTile(5, 0));
        Assert.Equal(TestDefs.EdgeNw, g.MegaTile(0, 5));   // 다른 변에는 문이 없다
    }

    [Fact]
    public void Props_ToSubCells_TilOrder_AndBoundaryMarks()
    {
        var g = ExpandOk(TestDefs.Map(12, place: [new PiecePlacementDef { Piece = "pillar", At = [new(3, 4)] }]));
        // 기둥 "x..." — 위-왼(2mx, 2my) 만 걷기·투사체 막힘(시야 통과), 둘레 아님. 글자가 아니라 성질 값으로 확인한다(6.11 "주의")
        const CellFlags walkMissile = CellFlags.BlocksWalk | CellFlags.BlocksProjectiles;
        const CellFlags all = walkMissile | CellFlags.BlocksSight;
        Assert.Equal(walkMissile, g.Flags(6, 8));
        Assert.Equal(CellFlags.None, g.Flags(7, 8));
        Assert.Equal(CellFlags.None, g.Flags(6, 9));
        Assert.Equal(CellFlags.None, g.Flags(7, 9));
        Assert.False(g.BlocksSight(6, 8));
        // 왼쪽 열 뒷벽 "X.X." — 바깥 줄(tx 0)만 셋 다 막힘, 둘레 표시는 칸 넷 모두
        Assert.Equal(all | CellFlags.Boundary, g.Flags(0, 4));
        Assert.Equal(CellFlags.Boundary, g.Flags(1, 4));
        Assert.Equal(all | CellFlags.Boundary, g.Flags(0, 5));
        // 윗줄 뒷벽 "XX.." — ty 0 줄만
        Assert.True(g.BlocksWalk(4, 0) && g.BlocksWalk(5, 0) && g.BlocksSight(4, 0));
        Assert.False(g.BlocksWalk(4, 1) || g.BlocksSight(4, 1));
        // 앞둘레 "XXXX" — 두 줄 두께
        Assert.True(g.BlocksWalk(22, 4) && g.BlocksWalk(23, 4) && g.BlocksProjectiles(22, 4) && g.BlocksSight(23, 4));
        // 바닥은 비어 있고 둘레 아님
        Assert.Equal(CellFlags.None, g.Flags(10, 10));
        // 격자 밖은 걷기·투사체 막힘 + Outside, 시야는 막지 않음(4.11)
        Assert.Equal(CellFlags.Outside | CellFlags.BlocksWalk | CellFlags.BlocksProjectiles, g.Flags(-1, 3));
        Assert.False(g.BlocksSight(-1, 3));
        Assert.False(g.BlocksSight(24, 30));
        Assert.Equal('#', g.Prop(24, 0));
    }

    [Fact]
    public void SinglePurposeAndSightCells_ByFlags()
    {
        var g = ExpandOk(TestDefs.Map(12, place:
        [
            new PiecePlacementDef { Piece = "low_wall", At = [new(4, 4)] },             // ww..
            new PiecePlacementDef { Piece = "glass", At = [new(6, 4)] },                // mm..
            new PiecePlacementDef { Piece = "walk_sight_wall", At = [new(4, 6)] },      // WW..
            new PiecePlacementDef { Piece = "sight_missile_wall", At = [new(6, 6)] },   // MM..
            new PiecePlacementDef { Piece = "sight_only", At = [new(8, 6)] },           // LL..
            new PiecePlacementDef { Piece = "column", At = [new(8, 4)] },               // X...
        ]));
        // v0.4 글자(W = 걷기만, M = 투사체만)가 남으면 여기서 실패한다 — 글자가 아니라 성질 값으로 본다
        Assert.Equal(CellFlags.BlocksWalk, g.Flags(8, 8));
        Assert.Equal(CellFlags.BlocksProjectiles, g.Flags(12, 8));
        Assert.Equal(CellFlags.BlocksWalk | CellFlags.BlocksSight, g.Flags(8, 12));
        Assert.Equal(CellFlags.BlocksSight | CellFlags.BlocksProjectiles, g.Flags(12, 12));
        Assert.Equal(CellFlags.BlocksSight, g.Flags(16, 12));
        Assert.Equal(CellFlags.BlocksWalk | CellFlags.BlocksSight | CellFlags.BlocksProjectiles, g.Flags(16, 8));
        Assert.True(g.IsWalkable(16, 12));     // 시야만 막는 칸은 걸을 수 있다
        Assert.False(g.BlocksProjectiles(16, 12));
        Assert.Equal("wmWMLX", new string([g.Prop(8, 8), g.Prop(12, 8), g.Prop(8, 12), g.Prop(12, 12), g.Prop(16, 12), g.Prop(16, 8)]));
    }

    [Fact]
    public void PropChars_EightGlyphs_RoundTripWithFlags_AndMatchSolBits()
    {
        Assert.Equal(".wLWmxMX", ArenaLayout.PropChars);
        CellFlags[] expected =
        [
            CellFlags.None,
            CellFlags.BlocksWalk,
            CellFlags.BlocksSight,
            CellFlags.BlocksWalk | CellFlags.BlocksSight,
            CellFlags.BlocksProjectiles,
            CellFlags.BlocksWalk | CellFlags.BlocksProjectiles,
            CellFlags.BlocksSight | CellFlags.BlocksProjectiles,
            CellFlags.BlocksWalk | CellFlags.BlocksSight | CellFlags.BlocksProjectiles,
        ];
        Span<CellFlags> cells = stackalloc CellFlags[4];
        for (var sol = 0; sol < 8; sol++)
        {
            var ch = ArenaLayout.PropChars[sol];
            Assert.True(ArenaLayout.TryParseProps(new string(ch, 4), cells));
            Assert.Equal(expected[sol], cells[0]);                                       // 글자 → 성질 (sol & 7 의 bit0 걷기·bit1 시야·bit2 투사체)
            Assert.Equal(ch, ArenaGrid.PropChar(cells[0]));                              // 성질 → 글자
            Assert.Equal(ch, ArenaGrid.PropChar(cells[0] | CellFlags.Boundary));         // 둘레 표시는 글자에 안 들어감
            Assert.Equal(char.IsUpper(ch), (cells[0] & CellFlags.BlocksSight) != 0);   // 대문자 = 시야 막힘
        }
        Assert.False(ArenaLayout.TryParseProps("..#.", cells));
        Assert.False(ArenaLayout.TryParseProps("...", cells));
        Assert.False(ArenaLayout.TryParseProps("l...", cells));   // 소문자 l 은 없다(시야만 = L)
    }

    [Fact]
    public void Place_Line_FillsRowOrColumn()
    {
        var g = ExpandOk(TestDefs.Map(12, place:
        [
            new PiecePlacementDef { Piece = "low_wall", Line = [new(3, 5), new(6, 5)] },
            new PiecePlacementDef { Piece = "pillar", Line = [new(8, 7), new(8, 4)] },
        ]));
        for (var mx = 3; mx <= 6; mx++) Assert.Equal(TestDefs.LowWall, g.MegaTile(mx, 5));
        Assert.NotEqual(TestDefs.LowWall, g.MegaTile(7, 5));
        for (var my = 4; my <= 7; my++) Assert.Equal(TestDefs.Pillar, g.MegaTile(8, my));
    }

    [Fact]
    public void FloorMix_IsDeterministic_AndUsesBothFloors()
    {
        var a = ExpandOk(TestDefs.Map(12));
        var b = ExpandOk(TestDefs.Map(12));
        var seen = new HashSet<int>();
        for (var my = 1; my < 11; my++)
            for (var mx = 1; mx < 11; mx++)
            {
                Assert.Equal(a.MegaTile(mx, my), b.MegaTile(mx, my));
                seen.Add(a.MegaTile(mx, my));
            }
        Assert.Contains(TestDefs.Floor1, seen);
        Assert.Contains(TestDefs.Floor2, seen);
        Assert.Equal(a.LayoutHash, b.LayoutHash);
        Assert.Matches("^[0-9a-f]{8}$", a.LayoutHash);
        // 바닥 섞기는 맵 ID 의 해시를 쓴다
        var other = ExpandOk(TestDefs.Map(12, id: "other_map"));
        var differs = false;
        for (var my = 1; my < 11 && !differs; my++)
            for (var mx = 1; mx < 11; mx++)
                if (other.MegaTile(mx, my) != a.MegaTile(mx, my)) { differs = true; break; }
        Assert.True(differs);
    }

    [Fact]
    public void LayoutHash_ChangesWithLayout_AndTilesetDefinition()
    {
        var baseHash = ExpandOk(TestDefs.Map(12)).LayoutHash;

        var moved = TestDefs.Map(12, place: [new PiecePlacementDef { Piece = "pillar", At = [new(3, 3), new(8, 3), new(3, 8), new(8, 7)] }]);
        Assert.NotEqual(baseHash, ExpandOk(moved).LayoutHash);   // 기둥 하나 옮김

        var ts = TestDefs.Tileset();
        var props = new Dictionary<int, string>(ts.Props) { [TestDefs.Pillar] = ".X.." };
        Assert.NotEqual(baseHash, ExpandOk(TestDefs.Map(12, tileset: ts with { Props = props })).LayoutHash);   // 성질 표 한 칸

        var diff = ts with { SolKnownDiffs = [new SolDiffDef { Tile = TestDefs.Pillar, Sub = 0, Sol = ".", Use = "x", Why = "시험" }] };
        Assert.NotEqual(baseHash, ExpandOk(TestDefs.Map(12, tileset: diff)).LayoutHash);   // 보정 목록

        var pal = TestDefs.Map(12) with { Palette = "other_pal" };
        Assert.NotEqual(baseHash, ExpandOk(pal).LayoutHash);   // 팔레트

        // 시야 글자만 바꿈(x → X, 걷기·투사체는 그대로) — 해시가 바뀐다(v0.5, 식은 그대로 — 성질 글자를 잇는다)
        var sightOnly = new Dictionary<int, string>(ts.Props) { [TestDefs.Pillar] = "X..." };
        var sightGrid = ExpandOk(TestDefs.Map(12, tileset: ts with { Props = sightOnly }));
        Assert.NotEqual(baseHash, sightGrid.LayoutHash);
        Assert.True(sightGrid.BlocksSight(6, 6) && !ExpandOk(TestDefs.Map(12)).BlocksSight(6, 6));
    }

    [Fact]
    public void Errors_TileMissingFromProps()
    {
        var ts = TestDefs.Tileset();
        var props = new Dictionary<int, string>(ts.Props);
        props.Remove(TestDefs.Pillar);
        var errors = ExpandErrors(TestDefs.Map(12, tileset: ts with { Props = props }));
        Assert.Contains(errors, e => e.Message.Contains($"메가타일 {TestDefs.Pillar}") && e.Message.Contains("pieces.pillar"));
        Assert.Throws<InvalidDataException>(() => ArenaLayout.Expand(TestDefs.Map(12, tileset: ts with { Props = props })));
    }

    [Fact]
    public void Errors_PlaceOnBorder_OutOfRange_Overlap_Diagonal_UnknownPiece()
    {
        var errors = ExpandErrors(TestDefs.Map(12, place:
        [
            new PiecePlacementDef { Piece = "pillar", At = [new(0, 5), new(12, 3), new(4, 4), new(4, 4)] },
            new PiecePlacementDef { Piece = "pillar", Line = [new(2, 2), new(5, 5)] },
            new PiecePlacementDef { Piece = "statue", At = [new(5, 5)] },
            new PiecePlacementDef { Piece = "pillar" },
        ]));
        Assert.Contains(errors, e => e.Path == "$.layout.place[0].at[0]" && e.Message.Contains("테두리"));
        Assert.Contains(errors, e => e.Path == "$.layout.place[0].at[1]" && e.Message.Contains("밖"));
        Assert.Contains(errors, e => e.Path == "$.layout.place[0].at[3]" && e.Message.Contains("이미"));
        Assert.Contains(errors, e => e.Path == "$.layout.place[1].line");
        Assert.Contains(errors, e => e.Path == "$.layout.place[2].piece");
        Assert.Contains(errors, e => e.Path == "$.layout.place[3]");
    }

    [Fact]
    public void Errors_Gates()
    {
        var ts = TestDefs.Tileset();
        var noSe = ts with { Gates = new Dictionary<string, GateSetDef> { [TestDefs.Border] = ts.Gates[TestDefs.Border] with { Se = null } } };
        var errors = ExpandErrors(TestDefs.Map(12, tileset: noSe));
        Assert.Contains(errors, e => e.Path == "$.layout.gates[2].side" && e.Message.Contains("se"));

        var m = TestDefs.Map(12, place: []);
        var bad = m with
        {
            Layout = m.Layout with
            {
                Gates =
                [
                    new GatePlacementDef { Id = "a", Side = GateSide.Nw, At = 1 },    // 끝 조각이 모서리
                    new GatePlacementDef { Id = "b", Side = GateSide.Ne, At = 10 },   // 열린 칸이 모서리
                    new GatePlacementDef { Id = "c", Side = GateSide.Se },
                    new GatePlacementDef { Id = "d", Side = GateSide.Se, At = 6 },    // c 와 겹침
                    new GatePlacementDef { Id = "c", Side = GateSide.Sw },            // ID 중복
                ],
            },
        };
        var e2 = ExpandErrors(bad);
        Assert.Contains(e2, e => e.Path == "$.layout.gates[0].at");
        Assert.Contains(e2, e => e.Path == "$.layout.gates[1].at");
        Assert.Contains(e2, e => e.Path == "$.layout.gates[3]" && e.Message.Contains("겹친다"));
        Assert.Contains(e2, e => e.Path == "$.layout.gates[4].id");

        var unknownBorder = m with { Layout = m.Layout with { Border = "nope" } };
        Assert.Contains(ExpandErrors(unknownBorder), e => e.Path == "$.layout.border");
    }

    [Fact]
    public void Errors_GateOpenTileMustBeEmpty()
    {
        var ts = TestDefs.Tileset();
        var props = new Dictionary<int, string>(ts.Props) { [TestDefs.GateOpen] = "X..." };
        var errors = ExpandErrors(TestDefs.Map(12, tileset: ts with { Props = props }));
        Assert.Contains(errors, e => e.Path.StartsWith("$.layout.gates[", StringComparison.Ordinal) && e.Message.Contains("비어 있지 않다"));
    }

    [Fact]
    public void ValidateTileset_PropsKeysSubsetOfNamedTiles_AndDiffs()
    {
        var ok = new List<LayoutError>();
        ArenaLayout.ValidateTileset(TestDefs.Tileset(), ok);
        Assert.Empty(ok);

        var ts = TestDefs.Tileset();
        var props = new Dictionary<int, string>(ts.Props) { [99] = "....", [TestDefs.Glass] = "MMQ." };
        props.Remove(TestDefs.LowWall);
        var bad = ts with
        {
            Props = props,
            SolKnownDiffs =
            [
                new SolDiffDef { Tile = TestDefs.Pillar, Sub = 1, Sol = ".", Use = "M", Why = "성질 표와 다름" },
                new SolDiffDef { Tile = 77, Sub = 0, Sol = ".", Use = "X", Why = "없는 메가타일" },
                new SolDiffDef { Tile = TestDefs.Pillar, Sub = 0, Sol = ".", Use = "x", Why = " " },
            ],
            DesignDiffs = [new SolDiffDef { Tile = TestDefs.Pillar, Sub = 4, Sol = "X", Use = "X", Why = "범위" }],
        };
        var errors = new List<LayoutError>();
        ArenaLayout.ValidateTileset(bad, errors);
        Assert.Contains(errors, e => e.Path == "$.props.99" && e.Message.Contains("이름 붙인 번호"));   // 쓰지 않는 메가타일(SOL 덤프 막기)
        Assert.Contains(errors, e => e.Path == $"$.props.{TestDefs.Glass}");                           // 틀린 글자
        Assert.Contains(errors, e => e.Path == "$.props" && e.Message.Contains($"{TestDefs.LowWall}")); // 이름 붙였는데 표에 없음
        Assert.Contains(errors, e => e.Path == "$.sol_known_diffs[0].use");
        Assert.Contains(errors, e => e.Path == "$.sol_known_diffs[1].tile");
        Assert.Contains(errors, e => e.Path == "$.sol_known_diffs[2].why");
        Assert.Contains(errors, e => e.Path == "$.design_diffs[0].sub");
        Assert.Contains(errors, e => e.Path == "$.design_diffs[0]" && e.Message.Contains("같다"));
        Assert.DoesNotContain(errors, e => e.Path == "$.sol_known_diffs[2].use");   // 성질 표 글자(x)와 같음

        // 8글자: 시야 보정도 같은 원소 하나(아치: 원본 . → M), v0.4 에 없던 글자도 받는다
        var arch = ts with { SolKnownDiffs = [new SolDiffDef { Tile = TestDefs.SightMissileWall, Sub = 0, Sol = ".", Use = "M", Why = "아치 보정" }],
                             DesignDiffs = [new SolDiffDef { Tile = TestDefs.Column, Sub = 0, Sol = "x", Use = "X", Why = "기둥이 시야를 막게" }] };
        var archErrors = new List<LayoutError>();
        ArenaLayout.ValidateTileset(arch, archErrors);
        Assert.Empty(archErrors);
        var badGlyph = ts with { SolKnownDiffs = [new SolDiffDef { Tile = TestDefs.Pillar, Sub = 0, Sol = "Q", Use = "x", Why = "글자" }] };
        var glyphErrors = new List<LayoutError>();
        ArenaLayout.ValidateTileset(badGlyph, glyphErrors);
        Assert.Contains(glyphErrors, e => e.Path == "$.sol_known_diffs[0].sol");
    }

    [Fact]
    public void NegativeTileNumbers_AreTilesetErrors_AndHashIgnoresCulture()
    {
        var ts = TestDefs.Tileset();
        var border = ts.Borders[TestDefs.Border] with { CornerN = -5 };
        var props = new Dictionary<int, string>(ts.Props) { [-5] = "XXX." };
        props.Remove(TestDefs.CornerN);
        var neg = ts with { Borders = new Dictionary<string, BorderTilesDef> { [TestDefs.Border] = border }, Props = props };

        var errors = new List<LayoutError>();
        ArenaLayout.ValidateTileset(neg, errors);
        Assert.Contains(errors, e => e.Path == "$" && e.Message.Contains("0 이상"));
        Assert.Contains(errors, e => e.Path == "$.props.-5");

        // 음수 부호가 U+2212 인 문화권에서도 해시가 같다(배치 풀기는 타일셋 검사와 따로 돈다)
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var a = ExpandOk(TestDefs.Map(12, tileset: neg)).LayoutHash;
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("sv-SE");
            Assert.Equal(a, ExpandOk(TestDefs.Map(12, tileset: neg)).LayoutHash);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = saved; }
    }

    [Fact]
    public void NamedTiles_UnionOfFloorBordersGatesPieces()
    {
        Assert.Equal(
            [1, 2, 3, 11, 12, 13, 14, 15, 16, 17, 18, 20, 30, 31, 32, 33, 34, 35, 36, 37, 38],
            ArenaLayout.NamedTiles(TestDefs.Tileset()));
    }
}
