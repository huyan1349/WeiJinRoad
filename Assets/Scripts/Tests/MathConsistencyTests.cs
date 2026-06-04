using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using WeiJinRoad.World;

namespace WeiJinRoad.Tests
{
    // ═══════════════════════════════════════════════════════════════
    // 测试框架基础设施
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 简易测试框架：提供断言和日志输出
    /// </summary>
    internal static class TestFramework
    {
        private static int _totalTests;
        private static int _passedTests;
        private static int _failedTests;
        private static readonly List<string> _failures = new List<string>();

        public static void Reset()
        {
            _totalTests = 0;
            _passedTests = 0;
            _failedTests = 0;
            _failures.Clear();
        }

        public static void AssertTrue(bool condition, string message)
        {
            _totalTests++;
            if (condition)
            {
                _passedTests++;
            }
            else
            {
                _failedTests++;
                _failures.Add(message);
                Debug.LogError($"  ✗ FAIL: {message}");
            }
        }

        public static void AssertApprox(float actual, float expected, float tolerance, string message)
        {
            bool pass = Mathf.Abs(actual - expected) <= tolerance;
            _totalTests++;
            if (pass)
            {
                _passedTests++;
            }
            else
            {
                _failedTests++;
                string detail = $"{message} (expected={expected:F6}, actual={actual:F6}, diff={Mathf.Abs(actual - expected):F6}, tol={tolerance})";
                _failures.Add(detail);
                Debug.LogError($"  ✗ FAIL: {detail}");
            }
        }

        public static void AssertRange(float value, float min, float max, string message)
        {
            bool pass = value >= min && value <= max;
            _totalTests++;
            if (pass)
            {
                _passedTests++;
            }
            else
            {
                _failedTests++;
                string detail = $"{message} (value={value:F6}, range=[{min},{max}])";
                _failures.Add(detail);
                Debug.LogError($"  ✗ FAIL: {detail}");
            }
        }

        public static void PrintSummary(string testName)
        {
            Debug.Log($"  [{testName}] {_passedTests}/{_totalTests} passed, {_failedTests} failed");
        }

        public static int FailedCount => _failedTests;
        public static int TotalCount => _totalTests;
        public static int PassedCount => _passedTests;
        public static IReadOnlyList<string> Failures => _failures;
    }

    // ═══════════════════════════════════════════════════════════════
    // MockRoadSampler — 用于 TerrainHeight 测试的模拟道路采样器
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 模拟道路采样器：将 RoadSplineData 的全局 RoadSample
    /// 转换为 WeiJinRoad.World.RoadSample，供 TerrainHeight 使用。
    /// </summary>
    internal class MockRoadSampler : IRoadSampler
    {
        private readonly RoadSplineData _data;

        public MockRoadSampler(RoadSplineData data)
        {
            _data = data;
        }

        public RoadSample Sample(float z)
        {
            var s = _data.Sample(z);
            return new RoadSample
            {
                Z = s.z,
                CenterX = s.centerX,
                Height = s.height,
                Width = s.width,
                HalfWidth = s.halfWidth,
                Direction = s.direction,
                RouteZ = s.routeZ,
                IsMountainRoad = s.isMountainRoad,
                IsForestRoad = s.isForestRoad,
                MountainBedHalfWidth = s.mountainBedHalfWidth,
            };
        }

        public ClosestRoadPointResult ClosestRoadPoint(float x, float z, float searchRadius, float step)
        {
            var result = _data.ClosestRoadPoint(x, z, searchRadius, step);
            if (!result.HasValue)
            {
                return new ClosestRoadPointResult { Found = false };
            }
            var (sample, distSq) = result.Value;
            return new ClosestRoadPointResult
            {
                Sample = new RoadSample
                {
                    Z = sample.z,
                    CenterX = sample.centerX,
                    Height = sample.height,
                    Width = sample.width,
                    HalfWidth = sample.halfWidth,
                    Direction = sample.direction,
                    RouteZ = sample.routeZ,
                    IsMountainRoad = sample.isMountainRoad,
                    IsForestRoad = sample.isForestRoad,
                    MountainBedHalfWidth = sample.mountainBedHalfWidth,
                },
                DistSq = distSq,
                Found = true,
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // NoiseTest — 噪声函数一致性测试
    // ═══════════════════════════════════════════════════════════════

    internal static class NoiseTest
    {
        [MenuItem("Tools/WeiJinRoad/Tests/Noise Test")]
        public static void Run()
        {
            Debug.Log("══════════════════════════════════════════");
            Debug.Log("  Noise 一致性测试");
            Debug.Log("══════════════════════════════════════════");
            TestFramework.Reset();

            TestHash2Determinism();
            TestHash2Range();
            TestValueNoise2DDeterminism();
            TestValueNoise2DRange();
            TestFbmDeterminism();
            TestFbmRange();
            TestFbmSmoothness();
            TestRidgedNoiseDeterminism();
            TestRidgedNoiseRange();
            TestRidgedNoiseSmoothness();
            TestFbmOctaveConsistency();
            TestRidgedNoiseOctaveConsistency();
            TestCrossLanguageConsistency();

            TestFramework.PrintSummary("Noise");
            if (TestFramework.FailedCount == 0)
                Debug.Log("  ✓ Noise 全部测试通过！");
        }

        /// <summary>
        /// Hash2 确定性：相同输入必须产生相同输出
        /// </summary>
        static void TestHash2Determinism()
        {
            Debug.Log("  [Hash2 确定性]");
            for (int i = 0; i < 20; i++)
            {
                float x = i * 37.3f - 100f;
                float y = i * 53.7f + 200f;
                float a = Noise.Hash2(x, y);
                float b = Noise.Hash2(x, y);
                TestFramework.AssertApprox(a, b, 0.0001f,
                    $"Hash2({x:F1}, {y:F1}) 确定性");
            }
        }

        /// <summary>
        /// Hash2 输出范围 [0,1)
        /// </summary>
        static void TestHash2Range()
        {
            Debug.Log("  [Hash2 范围]");
            for (int i = 0; i < 50; i++)
            {
                float x = i * 7.13f - 50f;
                float y = i * 11.31f + 30f;
                float v = Noise.Hash2(x, y);
                TestFramework.AssertRange(v, 0f, 1f,
                    $"Hash2({x:F1}, {y:F1}) 范围");
            }
        }

        /// <summary>
        /// ValueNoise2D 确定性
        /// </summary>
        static void TestValueNoise2DDeterminism()
        {
            Debug.Log("  [ValueNoise2D 确定性]");
            float[,] testPoints = {
                { 0f, 0f }, { 1.5f, 2.3f }, { -3.7f, 4.1f },
                { 100f, 200f }, { -50f, -75f }, { 0.001f, 0.002f }
            };
            for (int i = 0; i < testPoints.GetLength(0); i++)
            {
                float x = testPoints[i, 0];
                float y = testPoints[i, 1];
                float a = Noise.ValueNoise2D(x, y);
                float b = Noise.ValueNoise2D(x, y);
                TestFramework.AssertApprox(a, b, 0.0001f,
                    $"ValueNoise2D({x:F1}, {y:F1}) 确定性");
            }
        }

        /// <summary>
        /// ValueNoise2D 输出范围 [0,1]
        /// </summary>
        static void TestValueNoise2DRange()
        {
            Debug.Log("  [ValueNoise2D 范围]");
            for (int i = 0; i < 50; i++)
            {
                float x = i * 3.17f - 25f;
                float y = i * 5.23f + 10f;
                float v = Noise.ValueNoise2D(x, y);
                TestFramework.AssertRange(v, 0f, 1f,
                    $"ValueNoise2D({x:F1}, {y:F1}) 范围");
            }
        }

        /// <summary>
        /// Fbm 确定性
        /// </summary>
        static void TestFbmDeterminism()
        {
            Debug.Log("  [Fbm 确定性]");
            float[,] testPoints = {
                { 0f, 0f }, { 100f, 100f }, { -50f, 75f },
                { 0.5f, 0.5f }, { 256f, 512f }
            };
            for (int i = 0; i < testPoints.GetLength(0); i++)
            {
                float x = testPoints[i, 0];
                float y = testPoints[i, 1];
                float a = Noise.Fbm(x, y, 6);
                float b = Noise.Fbm(x, y, 6);
                TestFramework.AssertApprox(a, b, 0.0001f,
                    $"Fbm({x:F1}, {y:F1}, 6) 确定性");
            }
        }

        /// <summary>
        /// Fbm 输出范围 [0,1]（基于 ValueNoise2D 的 [0,1] 输出）
        /// </summary>
        static void TestFbmRange()
        {
            Debug.Log("  [Fbm 范围]");
            for (int i = 0; i < 50; i++)
            {
                float x = i * 2.71f - 30f;
                float y = i * 4.13f + 15f;
                float v = Noise.Fbm(x, y, 6);
                TestFramework.AssertRange(v, 0f, 1f,
                    $"Fbm({x:F1}, {y:F1}, 6) 范围");
            }
        }

        /// <summary>
        /// Fbm 平滑性：相邻输入产生相似输出
        /// </summary>
        static void TestFbmSmoothness()
        {
            Debug.Log("  [Fbm 平滑性]");
            float delta = 0.01f;
            float maxDiff = 0.05f;
            float[,] testPoints = {
                { 10f, 20f }, { 50f, 50f }, { -30f, 40f },
                { 0f, 0f }, { 100f, -50f }
            };
            for (int i = 0; i < testPoints.GetLength(0); i++)
            {
                float x = testPoints[i, 0];
                float y = testPoints[i, 1];
                float v0 = Noise.Fbm(x, y, 6);
                float vx = Noise.Fbm(x + delta, y, 6);
                float vy = Noise.Fbm(x, y + delta, 6);
                TestFramework.AssertTrue(
                    Mathf.Abs(vx - v0) < maxDiff,
                    $"Fbm X方向平滑 @({x:F0},{y:F0}) diff={Mathf.Abs(vx - v0):F6}");
                TestFramework.AssertTrue(
                    Mathf.Abs(vy - v0) < maxDiff,
                    $"Fbm Y方向平滑 @({x:F0},{y:F0}) diff={Mathf.Abs(vy - v0):F6}");
            }
        }

        /// <summary>
        /// RidgedNoise 确定性
        /// </summary>
        static void TestRidgedNoiseDeterminism()
        {
            Debug.Log("  [RidgedNoise 确定性]");
            float[,] testPoints = {
                { 0f, 0f }, { 50f, 50f }, { -25f, 100f },
                { 1.5f, 2.5f }, { 200f, -100f }
            };
            for (int i = 0; i < testPoints.GetLength(0); i++)
            {
                float x = testPoints[i, 0];
                float y = testPoints[i, 1];
                float a = Noise.RidgedNoise(x, y, 6);
                float b = Noise.RidgedNoise(x, y, 6);
                TestFramework.AssertApprox(a, b, 0.0001f,
                    $"RidgedNoise({x:F1}, {y:F1}, 6) 确定性");
            }
        }

        /// <summary>
        /// RidgedNoise 输出范围 [0,1]
        /// </summary>
        static void TestRidgedNoiseRange()
        {
            Debug.Log("  [RidgedNoise 范围]");
            for (int i = 0; i < 50; i++)
            {
                float x = i * 3.71f - 40f;
                float y = i * 6.13f + 20f;
                float v = Noise.RidgedNoise(x, y, 6);
                TestFramework.AssertRange(v, 0f, 1f,
                    $"RidgedNoise({x:F1}, {y:F1}, 6) 范围");
            }
        }

        /// <summary>
        /// RidgedNoise 平滑性
        /// </summary>
        static void TestRidgedNoiseSmoothness()
        {
            Debug.Log("  [RidgedNoise 平滑性]");
            float delta = 0.01f;
            float maxDiff = 0.08f;
            float[,] testPoints = {
                { 10f, 20f }, { 50f, 50f }, { -30f, 40f },
                { 0f, 0f }, { 100f, -50f }
            };
            for (int i = 0; i < testPoints.GetLength(0); i++)
            {
                float x = testPoints[i, 0];
                float y = testPoints[i, 1];
                float v0 = Noise.RidgedNoise(x, y, 6);
                float vx = Noise.RidgedNoise(x + delta, y, 6);
                float vy = Noise.RidgedNoise(x, y + delta, 6);
                TestFramework.AssertTrue(
                    Mathf.Abs(vx - v0) < maxDiff,
                    $"RidgedNoise X方向平滑 @({x:F0},{y:F0}) diff={Mathf.Abs(vx - v0):F6}");
                TestFramework.AssertTrue(
                    Mathf.Abs(vy - v0) < maxDiff,
                    $"RidgedNoise Y方向平滑 @({x:F0},{y:F0}) diff={Mathf.Abs(vy - v0):F6}");
            }
        }

        /// <summary>
        /// Fbm 八度一致性：更多八度 = 更精细但基础值相似
        /// </summary>
        static void TestFbmOctaveConsistency()
        {
            Debug.Log("  [Fbm 八度一致性]");
            float x = 42.5f, y = 67.3f;
            float v1 = Noise.Fbm(x, y, 1);
            float v2 = Noise.Fbm(x, y, 2);
            float v4 = Noise.Fbm(x, y, 4);
            float v6 = Noise.Fbm(x, y, 6);

            TestFramework.AssertRange(v1, 0f, 1f, "Fbm 1-octave 范围");
            TestFramework.AssertRange(v2, 0f, 1f, "Fbm 2-octave 范围");
            TestFramework.AssertRange(v4, 0f, 1f, "Fbm 4-octave 范围");
            TestFramework.AssertRange(v6, 0f, 1f, "Fbm 6-octave 范围");

            float vn = Noise.ValueNoise2D(x, y);
            TestFramework.AssertApprox(v1, vn, 0.0001f,
                "Fbm 1-octave == ValueNoise2D");
        }

        /// <summary>
        /// RidgedNoise 八度一致性
        /// </summary>
        static void TestRidgedNoiseOctaveConsistency()
        {
            Debug.Log("  [RidgedNoise 八度一致性]");
            float x = 42.5f, y = 67.3f;
            float v1 = Noise.RidgedNoise(x, y, 1);
            float v2 = Noise.RidgedNoise(x, y, 2);
            float v4 = Noise.RidgedNoise(x, y, 4);
            float v6 = Noise.RidgedNoise(x, y, 6);

            TestFramework.AssertRange(v1, 0f, 1f, "RidgedNoise 1-octave 范围");
            TestFramework.AssertRange(v2, 0f, 1f, "RidgedNoise 2-octave 范围");
            TestFramework.AssertRange(v4, 0f, 1f, "RidgedNoise 4-octave 范围");
            TestFramework.AssertRange(v6, 0f, 1f, "RidgedNoise 6-octave 范围");
        }

        /// <summary>
        /// 跨语言一致性：验证 C# 实现与 TypeScript 实现的数学等价性。
        ///
        /// TypeScript 中 Math.sin 和 Math.floor 的行为与 C# 的 Mathf.Sin 和 Mathf.Floor
        /// 在 IEEE 754 浮点数下应完全一致。此测试验证关键中间值。
        /// </summary>
        static void TestCrossLanguageConsistency()
        {
            Debug.Log("  [跨语言一致性]");

            // 验证 Hash2 核心公式
            {
                float a = 0f * 50f + 0f * 120f;
                float sinA = Mathf.Sin(a);
                float expected = sinA * 43758.5453123f - Mathf.Floor(sinA * 43758.5453123f);
                float actual = Noise.Hash2(0f, 0f);
                TestFramework.AssertApprox(actual, expected, 0.0001f,
                    "Hash2(0,0) 与手动计算一致");
                TestFramework.AssertApprox(actual, 0f, 0.0001f,
                    "Hash2(0,0) = 0 (sin(0)=0)");
            }

            // 验证 Hash2 的分形属性：不同输入产生不同输出
            {
                float h00 = Noise.Hash2(0f, 0f);
                float h10 = Noise.Hash2(1f, 0f);
                float h01 = Noise.Hash2(0f, 1f);
                float h11 = Noise.Hash2(1f, 1f);
                TestFramework.AssertTrue(h10 != h00, "Hash2(1,0) != Hash2(0,0)");
                TestFramework.AssertTrue(h01 != h00, "Hash2(0,1) != Hash2(0,0)");
                TestFramework.AssertTrue(h11 != h00, "Hash2(1,1) != Hash2(0,0)");
                TestFramework.AssertTrue(h11 != h10, "Hash2(1,1) != Hash2(1,0)");
                TestFramework.AssertTrue(h11 != h01, "Hash2(1,1) != Hash2(0,1)");
            }

            // 验证 ValueNoise2D 在整数格点上的值等于 Hash2
            {
                float vn00 = Noise.ValueNoise2D(0f, 0f);
                float h00 = Noise.Hash2(0f, 0f);
                TestFramework.AssertApprox(vn00, h00, 0.0001f,
                    "ValueNoise2D(0,0) == Hash2(0,0) (整数格点)");

                float vn53 = Noise.ValueNoise2D(5f, 3f);
                float h53 = Noise.Hash2(5f, 3f);
                TestFramework.AssertApprox(vn53, h53, 0.0001f,
                    "ValueNoise2D(5,3) == Hash2(5,3) (整数格点)");
            }

            // 验证 ValueNoise2D 在格点间的插值在 [0,1] 内
            {
                float vn = Noise.ValueNoise2D(0.5f, 0.5f);
                TestFramework.AssertRange(vn, 0f, 1f,
                    "ValueNoise2D(0.5,0.5) 范围 [0,1]");
            }

            // Fbm(x,y,1) == ValueNoise2D(x,y)
            {
                float x = 7.3f, y = 13.7f;
                float fbm1 = Noise.Fbm(x, y, 1);
                float vn = Noise.ValueNoise2D(x, y);
                TestFramework.AssertApprox(fbm1, vn, 0.0001f,
                    "Fbm(x,y,1) == ValueNoise2D(x,y)");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // RoadSplineTest — 道路样条采样测试
    // ═══════════════════════════════════════════════════════════════

    internal static class RoadSplineTest
    {
        [MenuItem("Tools/WeiJinRoad/Tests/RoadSpline Test")]
        public static void Run()
        {
            Debug.Log("══════════════════════════════════════════");
            Debug.Log("  RoadSpline 一致性测试");
            Debug.Log("══════════════════════════════════════════");
            TestFramework.Reset();

            var data = new RoadSplineData();

            TestSampleBasic(data);
            TestSampleDeterminism(data);
            TestSampleRangeCount(data);
            TestSampleMonotonicZ(data);
            TestClosestRoadPoint(data);
            TestClosestRoadPointOnRoad(data);
            TestCoordinateConversion();
            TestRoadHeightConsistency(data);
            TestRoadWidthConsistency(data);
            TestForestIntrusionDetection(data);
            TestMountainRoadDetection(data);
            TestSampleBoundaryValues(data);

            TestFramework.PrintSummary("RoadSpline");
            if (TestFramework.FailedCount == 0)
                Debug.Log("  ✓ RoadSpline 全部测试通过！");
        }

        static void TestSampleBasic(RoadSplineData data)
        {
            Debug.Log("  [Sample 基本验证]");
            float[] testZ = { 500f, 300f, 200f, 100f, 0f, -100f, -300f, -500f, -700f, -1000f, -1400f };
            foreach (float z in testZ)
            {
                var s = data.Sample(z);
                TestFramework.AssertTrue(!float.IsNaN(s.centerX), $"Sample({z:F0}).centerX 非 NaN");
                TestFramework.AssertTrue(!float.IsInfinity(s.centerX), $"Sample({z:F0}).centerX 非 Infinity");
                TestFramework.AssertTrue(!float.IsNaN(s.height), $"Sample({z:F0}).height 非 NaN");
                TestFramework.AssertTrue(!float.IsInfinity(s.height), $"Sample({z:F0}).height 非 Infinity");
                TestFramework.AssertTrue(s.width > 0f, $"Sample({z:F0}).width > 0 (actual={s.width:F2})");
                TestFramework.AssertTrue(s.halfWidth > 0f, $"Sample({z:F0}).halfWidth > 0");
            }
        }

        static void TestSampleDeterminism(RoadSplineData data)
        {
            Debug.Log("  [Sample 确定性]");
            float[] testZ = { 400f, 150f, 50f, -200f, -600f, -1200f };
            foreach (float z in testZ)
            {
                var a = data.Sample(z);
                var b = data.Sample(z);
                TestFramework.AssertApprox(a.centerX, b.centerX, 0.0001f,
                    $"Sample({z:F0}).centerX 确定性");
                TestFramework.AssertApprox(a.height, b.height, 0.0001f,
                    $"Sample({z:F0}).height 确定性");
            }
        }

        static void TestSampleRangeCount(RoadSplineData data)
        {
            Debug.Log("  [SampleRange 数量]");
            {
                var range = data.SampleRange(500f, 400f, 10);
                TestFramework.AssertTrue(range.Length == 11,
                    $"SampleRange(500,400,10) 返回 11 个点 (actual={range.Length})");
            }
            {
                var range = data.SampleRange(0f, -100f, 5);
                TestFramework.AssertTrue(range.Length == 6,
                    $"SampleRange(0,-100,5) 返回 6 个点 (actual={range.Length})");
            }
            {
                var range = data.SampleRange(300f, 300f, 0);
                TestFramework.AssertTrue(range.Length == 1,
                    $"SampleRange(300,300,0) 返回 1 个点 (actual={range.Length})");
            }
        }

        static void TestSampleMonotonicZ(RoadSplineData data)
        {
            Debug.Log("  [SampleRange Z 单调性]");
            var range = data.SampleRange(500f, -500f, 20);
            for (int i = 1; i < range.Length; i++)
            {
                TestFramework.AssertTrue(range[i].z <= range[i - 1].z,
                    $"SampleRange Z单调 i={i}: {range[i].z:F2} <= {range[i - 1].z:F2}");
            }
        }

        static void TestClosestRoadPoint(RoadSplineData data)
        {
            Debug.Log("  [ClosestRoadPoint 基本功能]");
            var sample = data.Sample(100f);
            var result = data.ClosestRoadPoint(sample.centerX, 100f, 28f, 2f);
            TestFramework.AssertTrue(result.HasValue,
                "ClosestRoadPoint 在道路中心应找到结果");
            if (result.HasValue)
            {
                TestFramework.AssertTrue(result.Value.distSq < 1f,
                    $"ClosestRoadPoint 道路中心 distSq < 1 (actual={result.Value.distSq:F4})");
            }

            var farResult = data.ClosestRoadPoint(500f, 100f, 28f, 2f);
            if (farResult.HasValue)
            {
                TestFramework.AssertTrue(result.Value.distSq < farResult.Value.distSq,
                    "远离道路的距离应大于道路中心的距离");
            }
        }

        static void TestClosestRoadPointOnRoad(RoadSplineData data)
        {
            Debug.Log("  [ClosestRoadPoint 道路上自身]");
            float[] testZ = { 300f, 0f, -400f, -800f };
            foreach (float z in testZ)
            {
                var s = data.Sample(z);
                var result = data.ClosestRoadPoint(s.centerX, z, 28f, 2f);
                TestFramework.AssertTrue(result.HasValue,
                    $"ClosestRoadPoint @z={z:F0} 应找到结果");
                if (result.HasValue)
                {
                    TestFramework.AssertTrue(result.Value.distSq < 4f,
                        $"ClosestRoadPoint @z={z:F0} distSq < 4 (actual={result.Value.distSq:F4})");
                }
            }
        }

        static void TestCoordinateConversion()
        {
            Debug.Log("  [坐标转换一致性]");
            float[] routeZs = { 400f, 300f, 100f, 0f, -100f, -500f, -1000f };
            foreach (float rz in routeZs)
            {
                float worldZ = RoadCoord.RouteToWorldZ(rz);
                float backRz = RoadCoord.WorldToRouteZ(worldZ);
                TestFramework.AssertApprox(backRz, rz, 0.0001f,
                    $"RouteToWorldZ({rz:F0}) -> WorldToRouteZ 往返一致");
            }

            float forestStart = RoadConstants.ForestIntrusionStartZ;
            float forestEnd = RoadConstants.ForestIntrusionEndZ;
            float midForest = (forestStart + forestEnd) / 2f;

            float routeMid = RoadCoord.WorldToRouteZ(midForest);
            TestFramework.AssertTrue(RoadCoord.IsForestIntrusionZ(midForest),
                $"Z={midForest:F1} 应在森林侵入区域");
            TestFramework.AssertApprox(routeMid, midForest + RoadConstants.ForestIntrusionLength, 0.01f,
                $"森林区域 WorldToRouteZ 转换正确");

            float outsideZ = 400f;
            TestFramework.AssertTrue(!RoadCoord.IsForestIntrusionZ(outsideZ),
                "Z=400 不在森林侵入区域");
            TestFramework.AssertApprox(RoadCoord.WorldToRouteZ(outsideZ), outsideZ, 0.0001f,
                "森林外 WorldToRouteZ 不变");
        }

        static void TestRoadHeightConsistency(RoadSplineData data)
        {
            Debug.Log("  [道路高度一致性]");
            var startSample = data.Sample(500f);
            var summitSample = data.Sample(-1500f);
            TestFramework.AssertTrue(summitSample.height > startSample.height,
                $"山顶高度({summitSample.height:F1}) > 起点高度({startSample.height:F1})");

            var range = data.SampleRange(500f, -1500f, 100);
            float maxJump = 0f;
            for (int i = 1; i < range.Length; i++)
            {
                float jump = Mathf.Abs(range[i].height - range[i - 1].height);
                if (jump > maxJump) maxJump = jump;
            }
            TestFramework.AssertTrue(maxJump < 5f,
                $"局部高度最大跳变 < 5 (actual={maxJump:F2})");
        }

        static void TestRoadWidthConsistency(RoadSplineData data)
        {
            Debug.Log("  [道路宽度一致性]");
            float[] testZ = { 500f, 300f, 200f, 100f, 0f, -100f, -300f, -500f, -700f, -1000f, -1400f };
            foreach (float z in testZ)
            {
                var s = data.Sample(z);
                TestFramework.AssertRange(s.width, 3f, 15f,
                    $"Sample({z:F0}).width 范围 [3,15] (actual={s.width:F2})");
                TestFramework.AssertApprox(s.halfWidth, s.width / 2f, 0.01f,
                    $"Sample({z:F0}).halfWidth == width/2");
            }
        }

        static void TestForestIntrusionDetection(RoadSplineData data)
        {
            Debug.Log("  [森林侵入检测]");
            float midForest = (RoadConstants.ForestIntrusionStartZ + RoadConstants.ForestIntrusionEndZ) / 2f;
            var forestSample = data.Sample(midForest);
            TestFramework.AssertTrue(forestSample.isForestRoad,
                $"Z={midForest:F1} 应标记为森林路");

            var highwaySample = data.Sample(400f);
            TestFramework.AssertTrue(!highwaySample.isForestRoad,
                "Z=400 不应标记为森林路");

            var mountainSample = data.Sample(-1000f);
            TestFramework.AssertTrue(!mountainSample.isForestRoad,
                "Z=-1000 不应标记为森林路");
        }

        static void TestMountainRoadDetection(RoadSplineData data)
        {
            Debug.Log("  [山路检测]");
            var mountainSample = data.Sample(-1000f);
            TestFramework.AssertTrue(mountainSample.isMountainRoad,
                "Z=-1000 应标记为山路");

            var highwaySample = data.Sample(400f);
            TestFramework.AssertTrue(!highwaySample.isMountainRoad,
                "Z=400 不应标记为山路");

            TestFramework.AssertTrue(mountainSample.mountainBedHalfWidth > 0f,
                $"山路路基半宽 > 0 (actual={mountainSample.mountainBedHalfWidth:F2})");

            TestFramework.AssertApprox(highwaySample.mountainBedHalfWidth, 0f, 0.01f,
                "非山路路基半宽 == 0");
        }

        static void TestSampleBoundaryValues(RoadSplineData data)
        {
            Debug.Log("  [边界值采样]");
            var maxSample = data.Sample(RoadConstants.RoadZMax);
            TestFramework.AssertTrue(!float.IsNaN(maxSample.centerX),
                $"RoadZMax={RoadConstants.RoadZMax} 采样有效");

            var minSample = data.Sample(RoadConstants.RoadZMin);
            TestFramework.AssertTrue(!float.IsNaN(minSample.centerX),
                $"RoadZMin={RoadConstants.RoadZMin} 采样有效");

            var overMax = data.Sample(RoadConstants.RoadZMax + 100f);
            TestFramework.AssertTrue(!float.IsNaN(overMax.centerX),
                "超出ZMax采样有效");

            var underMin = data.Sample(RoadConstants.RoadZMin - 100f);
            TestFramework.AssertTrue(!float.IsNaN(underMin.centerX),
                "超出ZMin采样有效");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // TerrainHeightTest — 地形高度一致性测试
    // ═══════════════════════════════════════════════════════════════

    internal static class TerrainHeightTest
    {
        private static RoadSplineData _data;
        private static MockRoadSampler _sampler;

        [MenuItem("Tools/WeiJinRoad/Tests/TerrainHeight Test")]
        public static void Run()
        {
            Debug.Log("══════════════════════════════════════════");
            Debug.Log("  TerrainHeight 一致性测试");
            Debug.Log("══════════════════════════════════════════");
            TestFramework.Reset();

            _data = new RoadSplineData();
            _sampler = new MockRoadSampler(_data);
            TerrainHeight.RoadSampler = _sampler;

            TestRoadCenterHeight();
            TestOffRoadHigherThanRoad();
            TestTerrainNormalUnitVector();
            TestSurfaceTypeValid();
            TestTerrainHeightDeterminism();
            TestTerrainHeightSmoothness();
            TestRoadCuttingShoulder();
            TestLakeRegion();
            TestMountainRegion();
            TestSummitRegion();

            TestFramework.PrintSummary("TerrainHeight");
            if (TestFramework.FailedCount == 0)
                Debug.Log("  ✓ TerrainHeight 全部测试通过！");
        }

        static void TestRoadCenterHeight()
        {
            Debug.Log("  [道路中心高度一致性]");
            float[] testZ = { 400f, 200f, 100f, 0f, -100f, -300f, -500f, -800f, -1200f };

            foreach (float z in testZ)
            {
                var roadSample = _data.Sample(z);
                float terrainHeight = TerrainHeight.GetTerrainHeight(roadSample.centerX, z);
                float diff = Mathf.Abs(terrainHeight - roadSample.height);
                TestFramework.AssertTrue(diff < 1f,
                    $"道路中心高度 @z={z:F0}: terrain={terrainHeight:F2}, road={roadSample.height:F2}, diff={diff:F2}");
            }
        }

        static void TestOffRoadHigherThanRoad()
        {
            Debug.Log("  [道路切割：路外地形高于路面]");
            float[] testZ = { 300f, 100f, 0f, -100f, -300f, -600f };

            foreach (float z in testZ)
            {
                var roadSample = _data.Sample(z);
                float roadH = TerrainHeight.GetTerrainHeight(roadSample.centerX, z);
                float offset = roadSample.halfWidth + 10f;
                float leftH = TerrainHeight.GetTerrainHeight(roadSample.centerX - offset, z);
                float rightH = TerrainHeight.GetTerrainHeight(roadSample.centerX + offset, z);

                TestFramework.AssertTrue(leftH >= roadH - 0.5f,
                    $"z={z:F0} 左侧路外地形({leftH:F2}) >= 路面({roadH:F2})-0.5");
                TestFramework.AssertTrue(rightH >= roadH - 0.5f,
                    $"z={z:F0} 右侧路外地形({rightH:F2}) >= 路面({roadH:F2})-0.5");
            }
        }

        static void TestTerrainNormalUnitVector()
        {
            Debug.Log("  [地形法线单位向量]");
            float[] testX = { 0f, 10f, -10f, 50f };
            float[] testZ = { 300f, 100f, 0f, -200f, -500f, -1000f };

            foreach (float x in testX)
            {
                foreach (float z in testZ)
                {
                    Vector3 normal = TerrainHeight.GetTerrainNormal(x, z);
                    float magnitude = normal.magnitude;
                    TestFramework.AssertApprox(magnitude, 1f, 0.01f,
                        $"Normal({x:F0},{z:F0}) 长度=1 (actual={magnitude:F4})");
                    TestFramework.AssertTrue(normal.y > 0f,
                        $"Normal({x:F0},{z:F0}).y > 0 (actual={normal.y:F4})");
                }
            }
        }

        static void TestSurfaceTypeValid()
        {
            Debug.Log("  [地表类型有效性]");
            float[] testX = { 0f, 10f, -10f, 50f, -50f };
            float[] testZ = { 400f, 200f, 100f, 0f, -100f, -300f, -500f, -800f, -1200f };

            var validTypes = new HashSet<SurfaceType>(
                (SurfaceType[])Enum.GetValues(typeof(SurfaceType)));

            foreach (float x in testX)
            {
                foreach (float z in testZ)
                {
                    SurfaceType st = TerrainHeight.GetSurfaceType(x, z);
                    TestFramework.AssertTrue(validTypes.Contains(st),
                        $"GetSurfaceType({x:F0},{z:F0}) = {st} 有效枚举值");
                }
            }
        }

        static void TestTerrainHeightDeterminism()
        {
            Debug.Log("  [地形高度确定性]");
            float[,] testPoints = {
                { 0f, 300f }, { 10f, 100f }, { -5f, 0f },
                { 20f, -200f }, { -15f, -600f }
            };

            for (int i = 0; i < testPoints.GetLength(0); i++)
            {
                float x = testPoints[i, 0];
                float z = testPoints[i, 1];
                float a = TerrainHeight.GetTerrainHeight(x, z);
                float b = TerrainHeight.GetTerrainHeight(x, z);
                TestFramework.AssertApprox(a, b, 0.0001f,
                    $"GetTerrainHeight({x:F0},{z:F0}) 确定性");
            }
        }

        static void TestTerrainHeightSmoothness()
        {
            Debug.Log("  [地形高度平滑性]");
            float delta = 0.5f;
            float maxDiff = 3f;
            float[,] testPoints = {
                { 0f, 300f }, { 10f, 100f }, { -5f, 0f },
                { 20f, -200f }, { -15f, -600f }
            };

            for (int i = 0; i < testPoints.GetLength(0); i++)
            {
                float x = testPoints[i, 0];
                float z = testPoints[i, 1];
                float h0 = TerrainHeight.GetTerrainHeight(x, z);
                float hx = TerrainHeight.GetTerrainHeight(x + delta, z);
                float hz = TerrainHeight.GetTerrainHeight(x, z + delta);

                TestFramework.AssertTrue(Mathf.Abs(hx - h0) < maxDiff,
                    $"X方向平滑 @({x:F0},{z:F0}) diff={Mathf.Abs(hx - h0):F3}");
                TestFramework.AssertTrue(Mathf.Abs(hz - h0) < maxDiff,
                    $"Z方向平滑 @({x:F0},{z:F0}) diff={Mathf.Abs(hz - h0):F3}");
            }
        }

        static void TestRoadCuttingShoulder()
        {
            Debug.Log("  [路肩混合过渡]");
            float z = 100f;
            var roadSample = _data.Sample(z);
            float rX = roadSample.centerX;
            float rHW = roadSample.halfWidth;

            float centerH = TerrainHeight.GetTerrainHeight(rX, z);
            float edgeH = TerrainHeight.GetTerrainHeight(rX + rHW * 0.95f, z);
            float shoulderH = TerrainHeight.GetTerrainHeight(rX + rHW * 1.1f, z);
            float offRoadH = TerrainHeight.GetTerrainHeight(rX + rHW + 10f, z);

            TestFramework.AssertTrue(Mathf.Abs(centerH - edgeH) < 2f,
                $"路面中心({centerH:F2})与边缘({edgeH:F2})高度差 < 2");

            float minH = Mathf.Min(centerH, offRoadH);
            float maxH = Mathf.Max(centerH, offRoadH);
            TestFramework.AssertTrue(shoulderH >= minH - 1f && shoulderH <= maxH + 1f,
                $"路肩高度({shoulderH:F2})在路面({centerH:F2})和路外({offRoadH:F2})之间");
        }

        static void TestLakeRegion()
        {
            Debug.Log("  [湖泊区域]");
            float lakeH = TerrainHeight.GetTerrainHeight(40f, 60f);
            var roadSample = _data.Sample(60f);
            float dist = Mathf.Sqrt((40f - roadSample.centerX) * (40f - roadSample.centerX));

            if (dist > roadSample.halfWidth)
            {
                TestFramework.AssertApprox(lakeH, 6f, 1f,
                    $"湖泊中心高度接近6 (actual={lakeH:F2})");
            }

            SurfaceType lakeSurface = TerrainHeight.GetSurfaceType(40f, 60f);
            if (dist > roadSample.halfWidth)
            {
                TestFramework.AssertTrue(lakeSurface == SurfaceType.Ice,
                    $"湖泊中心地表类型为Ice (actual={lakeSurface})");
            }
        }

        static void TestMountainRegion()
        {
            Debug.Log("  [山路段]");
            var startSample = _data.Sample(500f);
            float startH = TerrainHeight.GetTerrainHeight(startSample.centerX, 500f);

            var mountainSample = _data.Sample(-1000f);
            float mountainH = TerrainHeight.GetTerrainHeight(mountainSample.centerX, -1000f);

            TestFramework.AssertTrue(mountainH > startH + 50f,
                $"山路高度({mountainH:F1}) >> 起点高度({startH:F1})");

            SurfaceType mountainSurface = TerrainHeight.GetSurfaceType(mountainSample.centerX, -1000f);
            TestFramework.AssertTrue(mountainSurface == SurfaceType.Gravel,
                $"山路路面为Gravel (actual={mountainSurface})");
        }

        static void TestSummitRegion()
        {
            Debug.Log("  [山顶区域]");
            var summitSample = _data.Sample(-1532f);
            float summitH = TerrainHeight.GetTerrainHeight(summitSample.centerX, -1532f);

            TestFramework.AssertTrue(summitH > 180f,
                $"山顶高度 > 180 (actual={summitH:F1})");

            var mountainSample = _data.Sample(-1000f);
            float mountainH = TerrainHeight.GetTerrainHeight(mountainSample.centerX, -1000f);
            TestFramework.AssertTrue(summitH >= mountainH,
                $"山顶高度({summitH:F1}) >= 山路高度({mountainH:F1})");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // RunAllTests — 运行所有测试
    // ═══════════════════════════════════════════════════════════════

    internal static class MathConsistencyTests
    {
        [MenuItem("Tools/WeiJinRoad/Tests/Run All Tests")]
        public static void RunAll()
        {
            Debug.Log("╔══════════════════════════════════════════╗");
            Debug.Log("║  未尽之路 — 数学一致性测试套件          ║");
            Debug.Log("╚══════════════════════════════════════════╝");

            int totalFailed = 0;
            int totalTests = 0;
            int totalPassed = 0;

            NoiseTest.Run();
            totalFailed += TestFramework.FailedCount;
            totalTests += TestFramework.TotalCount;
            totalPassed += TestFramework.PassedCount;

            RoadSplineTest.Run();
            totalFailed += TestFramework.FailedCount;
            totalTests += TestFramework.TotalCount;
            totalPassed += TestFramework.PassedCount;

            TerrainHeightTest.Run();
            totalFailed += TestFramework.FailedCount;
            totalTests += TestFramework.TotalCount;
            totalPassed += TestFramework.PassedCount;

            Debug.Log("╔══════════════════════════════════════════╗");
            if (totalFailed == 0)
            {
                Debug.Log($"║  ✓ 全部通过: {totalPassed}/{totalTests}                    ║");
            }
            else
            {
                Debug.Log($"║  ✗ 失败: {totalFailed}/{totalTests}, 通过: {totalPassed}       ║");
            }
            Debug.Log("╚══════════════════════════════════════════╝");
        }
    }
}
ENDOFFILE; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-1e48dfa6e79a4d849d34fed09f463fd0/cwd.txt'; exit "$__tr_native_ec"