using System.Text.Json;
using System.Text.Json.Serialization;

namespace MidiConverter.Configuration;

/// <summary>
/// 插件配置类
/// 负责管理插件的所有配置项，包括转换参数、默认设置等
/// </summary>
public class Config
{
    /// <summary>
    /// 配置文件路径
    /// </summary>
    private static readonly string ConfigPath = Path.Combine(TShock.SavePath, "MidiConverter", "config.json");

    /// <summary>
    /// MusicPlayer播放速度（音高文件第一行写入值）
    /// 默认200，影响MusicPlayer中的播放速度
    /// </summary>
    [JsonPropertyName("defaultSpeed")]
    public int DefaultSpeed { get; set; } = 200;

    /// <summary>
    /// 和弦判定阈值（秒）
    /// 小于此时间间隔的音符将被视为同时发声（放在同一行）
    /// </summary>
    [JsonPropertyName("defaultShortInterval")]
    public float DefaultShortInterval { get; set; } = 0.1f;

    /// <summary>
    /// 休止符判定阈值（秒）
    /// 大于此时间间隔将插入休止符（0）
    /// </summary>
    [JsonPropertyName("defaultLongInterval")]
    public float DefaultLongInterval { get; set; } = 0.3f;

    /// <summary>
    /// 默认MIDI BPM（每分钟节拍数）
    /// 用于音高文件转MIDI时的默认速度
    /// </summary>
    [JsonPropertyName("defaultBpm")]
    public int DefaultBpm { get; set; } = 120;

    /// <summary>
    /// 默认乐器编号
    /// 0=钢琴，详见General MIDI标准
    /// </summary>
    [JsonPropertyName("defaultInstrument")]
    public int DefaultInstrument { get; set; } = 0;

    /// <summary>
    /// 是否自动映射超出范围的音高
    /// 开启后将C4-C6范围外的音符映射到最近的有效音
    /// </summary>
    [JsonPropertyName("autoMapOutOfRange")]
    public bool AutoMapOutOfRange { get; set; } = true;

    /// <summary>
    /// 界面语言代码
    /// 当前仅支持"zh-CN"（简体中文）
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// 是否启用详细日志记录
    /// 开启后会在控制台输出更多调试信息
    /// </summary>
    [JsonPropertyName("verboseLogging")]
    public bool VerboseLogging { get; set; } = false;

    /// <summary>
    /// 加载配置文件
    /// 如果文件不存在则创建默认配置
    /// </summary>
    /// <returns>配置实例</returns>
    public static Config Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<Config>(json);
                if (config != null)
                {
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            TShock.Log.Error($"[MidiConverter] 加载配置失败: {ex.Message}");
        }

        // 创建默认配置
        var defaultConfig = new Config();
        defaultConfig.Save();
        return defaultConfig;
    }

    /// <summary>
    /// 保存配置到文件
    /// 自动创建所需目录
    /// </summary>
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            TShock.Log.Error($"[MidiConverter] 保存配置失败: {ex.Message}");
        }
    }
}
