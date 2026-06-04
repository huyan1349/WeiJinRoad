using UnityEngine;

/// <summary>
/// 基于哈希的2D噪声工具类，提供值噪声、分形布朗运动和山脊噪声。
/// 所有方法为纯数学计算，无状态依赖，与 TypeScript 版本 math-noise 保持 1:1 对应。
/// </summary>
public static class Noise
{
    /// <summary>
    /// 二维哈希函数，将整数坐标映射到 [0,1) 伪随机值。
    /// 利用 sin 的周期性溢出产生散列效果。
    /// </summary>
    /// <param name="x">横坐标</param>
    /// <param name="y">纵坐标</param>
    /// <returns>[0,1) 范围的伪随机浮点数</returns>
    public static float Hash2(float x, float y)
    {
        float a = x * 50.0f + y * 120.0f;
        float sinA = Mathf.Sin(a);
        return sinA * 43758.5453123f - Mathf.Floor(sinA * 43758.5453123f);
    }

    /// <summary>
    /// 二维值噪声，在整数网格顶点之间进行 Hermite 平滑插值。
    /// </summary>
    /// <param name="x">横坐标（可为任意实数）</param>
    /// <param name="y">纵坐标（可为任意实数）</param>
    /// <returns>[0,1) 范围的平滑噪声值</returns>
    public static float ValueNoise2D(float x, float y)
    {
        float iX = Mathf.Floor(x);
        float iY = Mathf.Floor(y);
        float fX = x - iX;
        float fY = y - iY;

        // Hermite 平滑插值因子
        float u = fX * fX * (3.0f - 2.0f * fX);
        float v = fY * fY * (3.0f - 2.0f * fY);

        float a = Hash2(iX, iY);
        float b = Hash2(iX + 1, iY);
        float c = Hash2(iX, iY + 1);
        float d = Hash2(iX + 1, iY + 1);

        return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
    }

    /// <summary>
    /// 分形布朗运动（fBm），将多层值噪声按振幅衰减叠加。
    /// </summary>
    /// <param name="x">横坐标</param>
    /// <param name="y">纵坐标</param>
    /// <param name="octaves">叠加层数</param>
    /// <param name="lacunarity">频率倍增系数，默认 2.0</param>
    /// <param name="gain">振幅衰减系数，默认 0.5</param>
    /// <returns>归一化后的噪声值</returns>
    public static float Fbm(float x, float y, int octaves, float lacunarity = 2.0f, float gain = 0.5f)
    {
        float amplitude = 1.0f;
        float frequency = 1.0f;
        float noiseSum = 0.0f;
        float amplitudeSum = 0.0f;

        for (int i = 0; i < octaves; i++)
        {
            noiseSum += ValueNoise2D(x * frequency, y * frequency) * amplitude;
            amplitudeSum += amplitude;
            amplitude *= gain;
            frequency *= lacunarity;
        }

        return noiseSum / amplitudeSum;
    }

    /// <summary>
    /// 山脊噪声（Ridged Noise），对值噪声取绝对值反转后平方，产生尖锐的山脊状纹理。
    /// 每层结果会作为下一层的权重因子，使细节集中在山脊附近。
    /// </summary>
    /// <param name="x">横坐标</param>
    /// <param name="y">纵坐标</param>
    /// <param name="octaves">叠加层数</param>
    /// <param name="lacunarity">频率倍增系数，默认 2.0</param>
    /// <param name="gain">振幅衰减系数，默认 0.5</param>
    /// <returns>归一化后的山脊噪声值</returns>
    public static float RidgedNoise(float x, float y, int octaves, float lacunarity = 2.0f, float gain = 0.5f)
    {
        float amplitude = 1.0f;
        float frequency = 1.0f;
        float noiseSum = 0.0f;
        float amplitudeSum = 0.0f;
        float weight = 1.0f;

        for (int i = 0; i < octaves; i++)
        {
            float n = ValueNoise2D(x * frequency, y * frequency);
            n = 1.0f - Mathf.Abs(n * 2.0f - 1.0f);
            n *= n;
            n *= weight;
            weight = Mathf.Max(0.0f, Mathf.Min(1.0f, n * 2.0f));

            noiseSum += n * amplitude;
            amplitudeSum += amplitude;
            amplitude *= gain;
            frequency *= lacunarity;
        }

        return noiseSum / amplitudeSum;
    }
}
