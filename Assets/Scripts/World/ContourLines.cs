using System.Collections.Generic;
using UnityEngine;

namespace WeiJinRoad.World
{
    /// <summary>
    /// 等高线层级数据
    /// </summary>
    public class ContourLevel
    {
        /// <summary>等高线高度等级</summary>
        public float Level;
        /// <summary>等高线路径点列表（每条路径是一个 Vector2 列表）</summary>
        public List<List<Vector2>> Paths = new List<List<Vector2>>();
        /// <summary>是否为主等高线（每100单位加粗）</summary>
        public bool IsMajor;
    }

    /// <summary>
    /// Marching Squares 等高线生成器
    ///
    /// 在指定区域采样地形高度，使用 Marching Squares 算法生成等高线。
    /// 参考 TerrainHeight.GetTerrainHeight() 进行高度采样。
    /// 用于 MapUI 等高线渲染。
    /// </summary>
    public static class ContourLines
    {
        // =================================================================
        // 常量 — 与 TypeScript 版本完全一致
        // =================================================================

        /// <summary>等高线间隔（高度单位）</summary>
        public const float ContourInterval = 30f;
        /// <summary>采样步长（世界坐标单位）</summary>
        public const float SampleStep = 10f;
        /// <summary>最少点数，少于此数的路径被过滤</summary>
        public const int MinPathLength = 4;

        // =================================================================
        // Marching Squares 核心算法
        // =================================================================

        /// <summary>
        /// 在网格单元上执行 Marching Squares，生成等高线段
        /// </summary>
        /// <param name="x0">网格左下角X</param>
        /// <param name="z0">网格左下角Z</param>
        /// <param name="step">网格步长</param>
        /// <param name="h00">左下角高度</param>
        /// <param name="h10">右下角高度</param>
        /// <param name="h01">左上角高度</param>
        /// <param name="h11">右上角高度</param>
        /// <param name="level">等高线高度等级</param>
        /// <returns>线段列表，每条线段为 [x1, z1, x2, z2]</returns>
        private static List<float[]> MarchSquare(
            float x0, float z0, float step,
            float h00, float h10, float h01, float h11,
            float level)
        {
            var segments = new List<float[]>(4);

            int b00 = h00 >= level ? 1 : 0;
            int b10 = h10 >= level ? 1 : 0;
            int b01 = h01 >= level ? 1 : 0;
            int b11 = h11 >= level ? 1 : 0;

            int caseIndex = b00 | (b10 << 1) | (b01 << 2) | (b11 << 3);

            if (caseIndex == 0 || caseIndex == 15)
                return segments;

            float x1 = x0 + step;
            float z1 = z0 + step;

            // 四条边上的插值点
            Vector2 top = new Vector2(Lerp(x0, x1, h00, h10, level), z0);
            Vector2 bottom = new Vector2(Lerp(x0, x1, h01, h11, level), z1);
            Vector2 left = new Vector2(x0, Lerp(z0, z1, h00, h01, level));
            Vector2 right = new Vector2(x1, Lerp(z0, z1, h10, h11, level));

            switch (caseIndex)
            {
                case 1:  segments.Add(MakeSeg(left, top)); break;
                case 2:  segments.Add(MakeSeg(top, right)); break;
                case 3:  segments.Add(MakeSeg(left, right)); break;
                case 4:  segments.Add(MakeSeg(bottom, left)); break;
                case 5:  segments.Add(MakeSeg(left, top)); segments.Add(MakeSeg(bottom, right)); break;
                case 6:  segments.Add(MakeSeg(top, bottom)); break;
                case 7:  segments.Add(MakeSeg(bottom, right)); break;
                case 8:  segments.Add(MakeSeg(right, bottom)); break;
                case 9:  segments.Add(MakeSeg(left, bottom)); break;
                case 10: segments.Add(MakeSeg(top, left)); segments.Add(MakeSeg(right, bottom)); break;
                case 11: segments.Add(MakeSeg(right, bottom)); break;
                case 12: segments.Add(MakeSeg(left, right)); break;
                case 13: segments.Add(MakeSeg(top, right)); break;
                case 14: segments.Add(MakeSeg(left, top)); break;
            }

            return segments;
        }

        /// <summary>
        /// 线性插值，在高度空间中找到等高线与边的交点
        /// </summary>
        private static float Lerp(float a, float b, float ha, float hb, float level)
        {
            if (Mathf.Abs(hb - ha) < 0.001f) return a;
            float t = (level - ha) / (hb - ha);
            return a + t * (b - a);
        }

        /// <summary>
        /// 从两个端点创建线段数组 [x1, z1, x2, z2]
        /// </summary>
        private static float[] MakeSeg(Vector2 p1, Vector2 p2)
        {
            return new float[] { p1.x, p1.y, p2.x, p2.y };
        }

        // =================================================================
        // 线段连接
        // =================================================================

        /// <summary>
        /// 将线段连接成连续路径，过滤过短的路径
        /// </summary>
        /// <param name="segments">线段列表</param>
        /// <returns>连续路径列表</returns>
        private static List<List<Vector2>> ConnectSegments(List<float[]> segments)
        {
            var paths = new List<List<Vector2>>();
            if (segments.Count == 0) return paths;

            var used = new HashSet<int>();
            float threshold = SampleStep * 1.5f;

            for (int start = 0; start < segments.Count; start++)
            {
                if (used.Contains(start)) continue;

                var path = new List<Vector2>();
                var currentEnd = new Vector2(segments[start][2], segments[start][3]);
                path.Add(new Vector2(segments[start][0], segments[start][1]));
                path.Add(currentEnd);
                used.Add(start);

                bool found = true;
                while (found)
                {
                    found = false;
                    for (int i = 0; i < segments.Count; i++)
                    {
                        if (used.Contains(i)) continue;

                        float ax = segments[i][0], az = segments[i][1];
                        float bx = segments[i][2], bz = segments[i][3];

                        if (Mathf.Abs(ax - currentEnd.x) < threshold && Mathf.Abs(az - currentEnd.y) < threshold)
                        {
                            path.Add(new Vector2(bx, bz));
                            currentEnd = new Vector2(bx, bz);
                            used.Add(i);
                            found = true;
                            break;
                        }
                        if (Mathf.Abs(bx - currentEnd.x) < threshold && Mathf.Abs(bz - currentEnd.y) < threshold)
                        {
                            path.Add(new Vector2(ax, az));
                            currentEnd = new Vector2(ax, az);
                            used.Add(i);
                            found = true;
                            break;
                        }
                    }
                }

                // 过滤过短的路径
                if (path.Count >= MinPathLength)
                {
                    paths.Add(path);
                }
            }

            return paths;
        }

        // =================================================================
        // 主入口
        // =================================================================

        /// <summary>
        /// 生成等高线数据
        /// </summary>
        /// <param name="xMin">地图X最小值（世界坐标）</param>
        /// <param name="zMin">地图Z最小值（世界坐标）</param>
        /// <param name="xMax">地图X最大值</param>
        /// <param name="zMax">地图Z最大值</param>
        /// <returns>等高线层级列表</returns>
        public static List<ContourLevel> GenerateContourLines(
            float xMin, float zMin, float xMax, float zMax)
        {
            float step = SampleStep;
            int cols = Mathf.CeilToInt((xMax - xMin) / step);
            int rows = Mathf.CeilToInt((zMax - zMin) / step);

            // 采样高度网格
            var heights = new float[rows + 1, cols + 1];
            for (int r = 0; r <= rows; r++)
            {
                for (int c = 0; c <= cols; c++)
                {
                    float x = xMin + c * step;
                    float z = zMin + r * step;
                    heights[r, c] = TerrainHeight.GetTerrainHeight(x, z);
                }
            }

            // 确定高度范围
            float minH = float.PositiveInfinity;
            float maxH = float.NegativeInfinity;
            for (int r = 0; r <= rows; r++)
            {
                for (int c = 0; c <= cols; c++)
                {
                    if (heights[r, c] < minH) minH = heights[r, c];
                    if (heights[r, c] > maxH) maxH = heights[r, c];
                }
            }

            float startLevel = Mathf.Ceil(minH / ContourInterval) * ContourInterval;
            float endLevel = Mathf.Floor(maxH / ContourInterval) * ContourInterval;

            var result = new List<ContourLevel>();

            for (float level = startLevel; level <= endLevel; level += ContourInterval)
            {
                var allSegments = new List<float[]>();

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        float x = xMin + c * step;
                        float z = zMin + r * step;

                        var segs = MarchSquare(
                            x, z, step,
                            heights[r, c], heights[r, c + 1],
                            heights[r + 1, c], heights[r + 1, c + 1],
                            level
                        );
                        allSegments.AddRange(segs);
                    }
                }

                var paths = ConnectSegments(allSegments);
                if (paths.Count > 0)
                {
                    result.Add(new ContourLevel
                    {
                        Level = level,
                        Paths = paths,
                        IsMajor = Mathf.Approximately(level % 100f, 0f)
                    });
                }
            }

            return result;
        }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-91718804c00448429745d5153eb53d78/cwd.txt'; exit "$__tr_native_ec"