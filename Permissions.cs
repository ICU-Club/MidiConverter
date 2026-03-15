namespace MidiConverter.Common;

/// <summary>
/// 插件权限常量定义类
/// 所有权限节点均在此集中定义，便于管理和维护
/// </summary>
public static class Permissions
{
    /// <summary>
    /// 基础权限前缀
    /// </summary>
    private const string Prefix = "midiconverter";

    /// <summary>
    /// MIDI转音高文件权限
    /// 允许使用/mid转音高命令
    /// </summary>
    public const string Convert = $"{Prefix}.convert";

    /// <summary>
    /// 宽范围转换权限
    /// 允许使用/宽范围转换命令（不限制C4-C6音域）
    /// </summary>
    public const string ConvertWide = $"{Prefix}.convert.wide";

    /// <summary>
    /// 音高转MIDI权限
    /// 允许使用/音高转mid命令
    /// </summary>
    public const string ConvertBack = $"{Prefix}.convert.back";

    /// <summary>
    /// 查看文件列表权限
    /// 允许使用/转换列表命令
    /// </summary>
    public const string List = $"{Prefix}.list";

    /// <summary>
    /// 查看帮助权限
    /// 允许使用/转换帮助命令
    /// </summary>
    public const string Help = $"{Prefix}.help";

    /// <summary>
    /// 管理员权限
    /// 允许重载配置等管理操作
    /// </summary>
    public const string Admin = $"{Prefix}.admin";

    /// <summary>
    /// 批量转换权限
    /// 允许使用/批量转换命令
    /// </summary>
    public const string Batch = $"{Prefix}.batch";
}
