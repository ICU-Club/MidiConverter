using System.Text;
using System.Text.Json;

namespace MidiConverter.Localization;

/// <summary>
/// 本地化语言管理器
/// 支持多语言，当前仅实现简体中文（zh-CN）
/// </summary>
public static class Lang
{
    /// <summary>
    /// 翻译字典
    /// </summary>
    private static readonly Dictionary<string, string> Translations = new();
    
    /// <summary>
    /// 语言文件目录
    /// </summary>
    private static readonly string LangDirectory;
    
    /// <summary>
    /// 当前语言代码
    /// </summary>
    private static string _currentLanguage = "zh-CN";

    /// <summary>
    /// 静态构造函数，初始化目录并加载默认语言
    /// </summary>
    static Lang()
    {
        LangDirectory = Path.Combine(TShock.SavePath, "MidiConverter", "Lang");
        EnsureDirectoryExists(LangDirectory);
        LoadLanguage("zh-CN");
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"[MidiConverter] 创建目录失败 {path}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    /// <param name="key">键名</param>
    /// <returns>本地化文本，不存在则返回键名</returns>
    public static string GetString(string key)
    {
        if (Translations.TryGetValue(key, out var value))
            return value;
        
        return key; // 回退到键名
    }

    /// <summary>
    /// 获取本地化字符串（带参数格式化）
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="args">格式化参数</param>
    /// <returns>格式化后的本地化文本</returns>
    public static string GetString(string key, params object?[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }

    /// <summary>
    /// 加载指定语言
    /// </summary>
    /// <param name="languageCode">语言代码（如"zh-CN"）</param>
    public static void LoadLanguage(string languageCode)
    {
        _currentLanguage = languageCode;
        Translations.Clear();

        EnsureDirectoryExists(LangDirectory);
        
        var langFile = Path.Combine(LangDirectory, $"{languageCode}.json");
        
        if (!File.Exists(langFile))
        {
            CreateDefaultLanguageFile(langFile);
        }

        try
        {
            var json = File.ReadAllText(langFile, Encoding.UTF8);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    Translations[kvp.Key] = kvp.Value;
                }
            }
        }
        catch (Exception ex)
        {
            TShock.Log.Error($"[MidiConverter] 加载语言文件失败: {ex.Message}");
            LoadDefaultTranslations();
        }
    }

    /// <summary>
    /// 创建默认语言文件（简体中文）
    /// </summary>
    private static void CreateDefaultLanguageFile(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            EnsureDirectoryExists(directory!);

            var defaultTranslations = new Dictionary<string, string>
            {
                // 通用
                ["plugin.name"] = "MidiConverter",
                ["plugin.description"] = "MIDI与音高文件双向转换插件",
                ["plugin.author"] = "MidiConverter Team",
                
                // 命令帮助
                ["cmd.convert.usage"] = "用法: /mid转音高 <文件名> [速度] [短间隔] [长间隔] [-y]",
                ["cmd.convert.example"] = "示例: /mid转音高 mysong 200 0.1 0.3 -y",
                ["cmd.convert.desc"] = "说明: 将MIDI转换为MusicPlayer可用的音高文件(C4-C6范围)",
                
                ["cmd.wide.usage"] = "用法: /宽范围转换 <文件名> [速度] [短间隔] [长间隔] [-y]",
                ["cmd.wide.example"] = "示例: /宽范围转换 mysong 200 0.1 0.3 -y",
                ["cmd.wide.desc"] = "说明: 将MIDI转换为更宽音高范围的音高文件",
                
                ["cmd.back.usage"] = "用法: /音高转mid <文件名> [BPM] [乐器] [-y]",
                ["cmd.back.example"] = "示例: /音高转mid mysong 120 0 -y",
                ["cmd.back.desc"] = "说明: 将音高文件转换回MIDI格式",

                ["cmd.batch.usage"] = "用法: /批量转换 <模式> <通配符> [参数...] [-y]",
                ["cmd.batch.example"] = "示例: /批量转换 midi2note *.mid -y",
                ["cmd.batch.desc"] = "说明: 批量转换多个文件（模式: midi2note/wide/note2midi）",
                
                // 成功消息
                ["msg.convert.success"] = "✅ 转换成功！",
                ["msg.wide.success"] = "✅ 宽范围转换成功！",
                ["msg.back.success"] = "✅ 反向转换成功！",
                ["msg.batch.success"] = "✅ 批量转换完成！成功: {0}, 失败: {1}, 跳过: {2}",
                ["msg.output.file"] = "输出文件: {0}",
                ["msg.note.count"] = "音符数: {0}, 速度: {1}",
                ["msg.note.bpm"] = "音符数: {0}, BPM: {1}, 乐器: {2}",
                ["msg.skipped.notes"] = "注意: 跳过了 {0} 个超出范围的音符",
                ["msg.file.exists"] = "⚠️ 文件已存在: {0}",
                ["msg.file.overwrite"] = "使用参数 -y 或 --force 强制覆盖",
                ["msg.file.force"] = "⚠️ 强制覆盖模式，原文件将被替换",
                ["msg.progress"] = "进度: {0}% ({1}/{2})",
                ["msg.batch.nofiles"] = "⚠️ 未找到匹配的文件: {0}",
using System.Text;
using System.Text.Json;

namespace MidiConverter.Localization;

/// <summary>
/// 本地化语言管理器
/// 支持多语言，当前仅实现简体中文（zh-CN）
/// </summary>
public static class Lang
{
    /// <summary>
    /// 翻译字典
    /// </summary>
    private static readonly Dictionary<string, string> Translations = new();
    
    /// <summary>
    /// 语言文件目录
    /// </summary>
    private static readonly string LangDirectory;
    
    /// <summary>
    /// 当前语言代码
    /// </summary>
    private static string _currentLanguage = "zh-CN";

    /// <summary>
    /// 静态构造函数，初始化目录并加载默认语言
    /// </summary>
    static Lang()
    {
        LangDirectory = Path.Combine(TShock.SavePath, "MidiConverter", "Lang");
        EnsureDirectoryExists(LangDirectory);
        LoadLanguage("zh-CN");
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"[MidiConverter] 创建目录失败 {path}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    /// <param name="key">键名</param>
    /// <returns>本地化文本，不存在则返回键名</returns>
    public static string GetString(string key)
    {
        if (Translations.TryGetValue(key, out var value))
            return value;
        
        return key; // 回退到键名
    }

    /// <summary>
    /// 获取本地化字符串（带参数格式化）
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="args">格式化参数</param>
    /// <returns>格式化后的本地化文本</returns>
    public static string GetString(string key, params object?[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }

    /// <summary>
    /// 加载指定语言
    /// </summary>
    /// <param name="languageCode">语言代码（如"zh-CN"）</param>
    public static void LoadLanguage(string languageCode)
    {
        _currentLanguage = languageCode;
        Translations.Clear();

        EnsureDirectoryExists(LangDirectory);
        
        var langFile = Path.Combine(LangDirectory, $"{languageCode}.json");
        
        if (!File.Exists(langFile))
        {
            CreateDefaultLanguageFile(langFile);
        }

        try
        {
            var json = File.ReadAllText(langFile, Encoding.UTF8);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    Translations[kvp.Key] = kvp.Value;
                }
            }
        }
        catch (Exception ex)
        {
            TShock.Log.Error($"[MidiConverter] 加载语言文件失败: {ex.Message}");
            LoadDefaultTranslations();
        }
    }

    /// <summary>
    /// 创建默认语言文件（简体中文）
    /// </summary>
    private static void CreateDefaultLanguageFile(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            EnsureDirectoryExists(directory!);

            var defaultTranslations = new Dictionary<string, string>
            {
                // 通用
                ["plugin.name"] = "MidiConverter",
                ["plugin.description"] = "MIDI与音高文件双向转换插件",
                ["plugin.author"] = "MidiConverter Team",
                
                // 命令帮助
                ["cmd.convert.usage"] = "用法: /mid转音高 <文件名> [速度] [短间隔] [长间隔] [-y]",
                ["cmd.convert.example"] = "示例: /mid转音高 mysong 200 0.1 0.3 -y",
                ["cmd.convert.desc"] = "说明: 将MIDI转换为MusicPlayer可用的音高文件(C4-C6范围)",
                
                ["cmd.wide.usage"] = "用法: /宽范围转换 <文件名> [速度] [短间隔] [长间隔] [-y]",
                ["cmd.wide.example"] = "示例: /宽范围转换 mysong 200 0.1 0.3 -y",
                ["cmd.wide.desc"] = "说明: 将MIDI转换为更宽音高范围的音高文件",
                
                ["cmd.back.usage"] = "用法: /音高转mid <文件名> [BPM] [乐器] [-y]",
                ["cmd.back.example"] = "示例: /音高转mid mysong 120 0 -y",
                ["cmd.back.desc"] = "说明: 将音高文件转换回MIDI格式",

                ["cmd.batch.usage"] = "用法: /批量转换 <模式> <通配符> [参数...] [-y]",
                ["cmd.batch.example"] = "示例: /批量转换 midi2note *.mid -y",
                ["cmd.batch.desc"] = "说明: 批量转换多个文件（模式: midi2note/wide/note2midi）",
                
                // 成功消息
                ["msg.convert.success"] = "✅ 转换成功！",
                ["msg.wide.success"] = "✅ 宽范围转换成功！",
                ["msg.back.success"] = "✅ 反向转换成功！",
                ["msg.batch.success"] = "✅ 批量转换完成！成功: {0}, 失败: {1}, 跳过: {2}",
                ["msg.output.file"] = "输出文件: {0}",
                ["msg.note.count"] = "音符数: {0}, 速度: {1}",
                ["msg.note.bpm"] = "音符数: {0}, BPM: {1}, 乐器: {2}",
                ["msg.skipped.notes"] = "注意: 跳过了 {0} 个超出范围的音符",
                ["msg.file.exists"] = "⚠️ 文件已存在: {0}",
                ["msg.file.overwrite"] = "使用参数 -y 或 --force 强制覆盖",
                ["msg.file.force"] = "⚠️ 强制覆盖模式，原文件将被替换",
                ["msg.progress"] = "进度: {0}% ({1}/{2})",
                ["msg.batch.nofiles"] = "⚠️ 未找到匹配的文件: {0}",
                
                // 错误消息
                ["msg.error.file.notfound"] = "❌ 未找到文件: {0}",
                ["msg.error.folder.empty"] = "请将文件放入: {0}",
                ["msg.error.convert.failed"] = "❌ 转换失败！",
                ["msg.error.convert.exception"] = "❌ 转换异常！",
                ["msg.error.no.notes"] = "文件中没有找到音符数据",
                ["msg.error.invalid.midi"] = "无效的MIDI文件",
                ["msg.error.invalid.note"] = "无效的音高文件",
                ["msg.error.unknown.mode"] = "❌ 未知模式: {0}，可用模式: midi2note, wide, note2midi",
                
                // 帮助信息
                ["help.title"] = "=== MidiConverter 使用帮助 ===",
                ["help.version"] = "版本: v{0} | 作者: {1}",
                ["help.folders.title"] = "📁 文件夹说明:",
                ["help.folders.midi"] = "  • MIDI输入 - 放入需要转换为MusicPlayer格式的MIDI文件",
                ["help.folders.wide"] = "  • 宽范围MIDI输入 - 放入需要宽范围转换的MIDI文件",
                ["help.folders.note"] = "  • 音高输入 - 放入需要转回MIDI的音高文件",
                ["help.folders.wideout"] = "  • 宽范围输出 - 宽范围转换的输出位置",
                ["help.folders.midiout"] = "  • MIDI输出 - 反向转换的输出位置",
                ["help.folders.log"] = "  • 错误日志 - 转换错误的详细日志",
                
                ["help.commands.title"] = "🎵 可用命令:",
                ["help.commands.convert"] = "  /mid转音高 <文件名> [速度] [短间隔] [长间隔] [-y]",
                ["help.commands.convert.desc"] = "    → 将MIDI转换为MusicPlayer可用的音高文件(C4-C6)",
                ["help.commands.wide"] = "  /宽范围转换 <文件名> [速度] [短间隔] [长间隔] [-y]",
                ["help.commands.wide.desc"] = "    → 将MIDI转换为更宽音高范围的音高文件",
                ["help.commands.back"] = "  /音高转mid <文件名> [BPM] [乐器] [-y]",
                ["help.commands.back.desc"] = "    → 将音高文件转换回MIDI格式",
                ["help.commands.batch"] = "  /批量转换 <模式> <通配符> [参数...] [-y]",
                ["help.commands.batch.desc"] = "    → 批量转换文件（模式: midi2note/wide/note2midi）",
                ["help.commands.list"] = "  /转换列表 - 查看各文件夹中的文件",
                ["help.commands.help"] = "  /转换帮助 - 显示此帮助信息",
                
                ["help.params.title"] = "💡 参数说明:",
                ["help.params.speed"] = "  速度 - 播放速度 (默认200)",
                ["help.params.short"] = "  短间隔 - 小于此值的音符放在同一行 (默认0.1秒)",
                ["help.params.long"] = "  长间隔 - 大于此值的间隔插入休止符 (默认0.3秒)",
                ["help.params.bpm"] = "  BPM - MIDI速度 (默认120)",
                ["help.params.instrument"] = "  乐器 - MIDI乐器编号 (默认0=钢琴)",
                ["help.params.force"] = "  -y, --force - 强制覆盖已存在的文件",
                
                ["help.batch.title"] = "🚀 批量转换示例:",
using System.Text;
using System.Text.Json;

namespace MidiConverter.Localization;

/// <summary>
/// 本地化语言管理器
/// 支持多语言，当前仅实现简体中文（zh-CN）
/// </summary>
public static class Lang
{
    /// <summary>
    /// 翻译字典
    /// </summary>
    private static readonly Dictionary<string, string> Translations = new();
    
    /// <summary>
    /// 语言文件目录
    /// </summary>
    private static readonly string LangDirectory;
    
    /// <summary>
    /// 当前语言代码
    /// </summary>
    private static string _currentLanguage = "zh-CN";

    /// <summary>
    /// 静态构造函数，初始化目录并加载默认语言
    /// </summary>
    static Lang()
    {
        LangDirectory = Path.Combine(TShock.SavePath, "MidiConverter", "Lang");
        EnsureDirectoryExists(LangDirectory);
        LoadLanguage("zh-CN");
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"[MidiConverter] 创建目录失败 {path}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    /// <param name="key">键名</param>
    /// <returns>本地化文本，不存在则返回键名</returns>
    public static string GetString(string key)
    {
        if (Translations.TryGetValue(key, out var value))
            return value;
        
        return key; // 回退到键名
    }

    /// <summary>
    /// 获取本地化字符串（带参数格式化）
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="args">格式化参数</param>
    /// <returns>格式化后的本地化文本</returns>
    public static string GetString(string key, params object?[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }

    /// <summary>
    /// 加载指定语言
    /// </summary>
    /// <param name="languageCode">语言代码（如"zh-CN"）</param>
    public static void LoadLanguage(string languageCode)
    {
        _currentLanguage = languageCode;
        Translations.Clear();

        EnsureDirectoryExists(LangDirectory);
        
        var langFile = Path.Combine(LangDirectory, $"{languageCode}.json");
        
        if (!File.Exists(langFile))
        {
            CreateDefaultLanguageFile(langFile);
        }

        try
        {
            var json = File.ReadAllText(langFile, Encoding.UTF8);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    Translations[kvp.Key] = kvp.Value;
                }
            }
        }
        catch (Exception ex)
        {
            TShock.Log.Error($"[MidiConverter] 加载语言文件失败: {ex.Message}");
            LoadDefaultTranslations();
        }
    }

    /// <summary>
    /// 创建默认语言文件（简体中文）
    /// </summary>
    private static void CreateDefaultLanguageFile(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            EnsureDirectoryExists(directory!);

            var defaultTranslations = new Dictionary<string, string>
            {
                // 通用
                ["plugin.name"] = "MidiConverter",
                ["plugin.description"] = "MIDI与音高文件双向转换插件",
                ["plugin.author"] = "MidiConverter Team",
                
                // 命令帮助
                ["cmd.convert.usage"] = "用法: /mid转音高 <文件名> [速度] [短间隔] [长间隔] [-y]",
                ["cmd.convert.example"] = "示例: /mid转音高 mysong 200 0.1 0.3 -y",
                ["cmd.convert.desc"] = "说明: 将MIDI转换为MusicPlayer可用的音高文件(C4-C6范围)",
                
                ["cmd.wide.usage"] = "用法: /宽范围转换 <文件名> [速度] [短间隔] [长间隔] [-y]",
                ["cmd.wide.example"] = "示例: /宽范围转换 mysong 200 0.1 0.3 -y",
                ["cmd.wide.desc"] = "说明: 将MIDI转换为更宽音高范围的音高文件",
                
                ["cmd.back.usage"] = "用法: /音高转mid <文件名> [BPM] [乐器] [-y]",
                ["cmd.back.example"] = "示例: /音高转mid mysong 120 0 -y",
                ["cmd.back.desc"] = "说明: 将音高文件转换回MIDI格式",

                ["cmd.batch.usage"] = "用法: /批量转换 <模式> <通配符> [参数...] [-y]",
                ["cmd.batch.example"] = "示例: /批量转换 midi2note *.mid -y",
                ["cmd.batch.desc"] = "说明: 批量转换多个文件（模式: midi2note/wide/note2midi）",
                
                // 成功消息
                ["msg.convert.success"] = "✅ 转换成功！",
                ["msg.wide.success"] = "✅ 宽范围转换成功！",
                ["msg.back.success"] = "✅ 反向转换成功！",
                ["msg.batch.success"] = "✅ 批量转换完成！成功: {0}, 失败: {1}, 跳过: {2}",
                ["msg.output.file"] = "输出文件: {0}",
                ["msg.note.count"] = "音符数: {0}, 速度: {1}",
                ["msg.note.bpm"] = "音符数: {0}, BPM: {1}, 乐器: {2}",
                ["msg.skipped.notes"] = "注意: 跳过了 {0} 个超出范围的音符",
                ["msg.file.exists"] = "⚠️ 文件已存在: {0}",
                ["msg.file.overwrite"] = "使用参数 -y 或 --force 强制覆盖",
                ["msg.file.force"] = "⚠️ 强制覆盖模式，原文件将被替换",
                ["msg.progress"] = "进度: {0}% ({1}/{2})",
                ["msg.batch.nofiles"] = "⚠️ 未找到匹配的文件: {0}",
                
                // 错误消息
                ["msg.error.file.notfound"] = "❌ 未找到文件: {0}",
                ["msg.error.folder.empty"] = "请将文件放入: {0}",
                ["msg.error.convert.failed"] = "❌ 转换失败！",
                ["msg.error.convert.exception"] = "❌ 转换异常！",
                ["msg.error.no.notes"] = "文件中没有找到音符数据",
                ["msg.error.invalid.midi"] = "无效的MIDI文件",
                ["msg.error.invalid.note"] = "无效的音高文件",
                ["msg.error.unknown.mode"] = "❌ 未知模式: {0}，可用模式: midi2note, wide, note2midi",
                
                // 帮助信息
                ["help.title"] = "=== MidiConverter 使用帮助 ===",
                ["help.version"] = "版本: v{0} | 作者: {1}",
                ["help.folders.title"] = "📁 文件夹说明:",
                ["help.folders.midi"] = "  • MIDI输入 - 放入需要转换为MusicPlayer格式的MIDI文件",
                ["help.folders.wide"] = "  • 宽范围MIDI输入 - 放入需要宽范围转换的MIDI文件",
                ["help.folders.note"] = "  • 音高输入 - 放入需要转回MIDI的音高文件",
                ["help.folders.wideout"] = "  • 宽范围输出 - 宽范围转换的输出位置",
                ["help.folders.midiout"] = "  • MIDI输出 - 反向转换的输出位置",
                ["help.folders.log"] = "  • 错误日志 - 转换错误的详细日志",
                
                ["help.commands.title"] = "🎵 可用命令:",
                ["help.commands.convert"] = "  /mid转音高 <文件名> [速度] [短间隔] [长间隔] [-y]",
                ["help.commands.convert.desc"] = "    → 将MIDI转换为MusicPlayer可用的音高文件(C4-C6)",
                ["help.commands.wide"] = "  /宽范围转换 <文件名> [速度] [短间隔] [长间隔] [-y]",
                ["help.commands.wide.desc"] = "    → 将MIDI转换为更宽音高范围的音高文件",
                ["help.commands.back"] = "  /音高转mid <文件名> [BPM] [乐器] [-y]",
                ["help.commands.back.desc"] = "    → 将音高文件转换回MIDI格式",
                ["help.commands.batch"] = "  /批量转换 <模式> <通配符> [参数...] [-y]",
                ["help.commands.batch.desc"] = "    → 批量转换文件（模式: midi2note/wide/note2midi）",
                ["help.commands.list"] = "  /转换列表 - 查看各文件夹中的文件",
                ["help.commands.help"] = "  /转换帮助 - 显示此帮助信息",
                
                ["help.params.title"] = "💡 参数说明:",
                ["help.params.speed"] = "  速度 - 播放速度 (默认200)",
                ["help.params.short"] = "  短间隔 - 小于此值的音符放在同一行 (默认0.1秒)",
                ["help.params.long"] = "  长间隔 - 大于此值的间隔插入休止符 (默认0.3秒)",
                ["help.params.bpm"] = "  BPM - MIDI速度 (默认120)",
                ["help.params.instrument"] = "  乐器 - MIDI乐器编号 (默认0=钢琴)",
                ["help.params.force"] = "  -y, --force - 强制覆盖已存在的文件",
                
                ["help.batch.title"] = "🚀 批量转换示例:",
                ["help.batch.example1"] = "  /批量转换 midi2note *.mid",
                ["help.batch.example1.desc"] = "    → 转换所有.mid文件到MusicPlayer目录",
                ["help.batch.example2"] = "  /批量转换 wide song*.mid",
                ["help.batch.example2.desc"] = "    → 批量宽范围转换匹配的文件",
                ["help.batch.example3"] = "  /批量转换 note2midi *.txt 120 -y",
                ["help.batch.example3.desc"] = "    → 强制转换所有.txt文件到MIDI",
                
                ["help.notes.title"] = "⚠️ 注意:",
                ["help.notes.range"] = "  • MusicPlayer只支持C4-C6范围的音符",
                ["help.notes.skip"] = "  • 超出范围的音符会被自动跳过或映射到最近的有效音符",
                ["help.notes.console"] = "  • 所有命令都支持在控制台中使用",
                ["help.notes.progress"] = "  • 转换大文件时会显示进度百分比",
                
                // 文件列表
                ["list.title"] = "=== MidiConverter 文件列表 ===",
                ["list.folder.midi"] = "\n[MIDI输入] 文件夹: {0}",
                ["list.folder.wide"] = "\n[宽范围MIDI输入] 文件夹: {0}",
                ["list.folder.note"] = "\n[音高输入] 文件夹: {0}",
                ["list.folder.songs"] = "\n[MusicPlayer歌曲] 文件夹: {0}",
                ["list.empty"] = "  (空)",
                ["list.more"] = "  ... 还有 {0} 个文件",
                
                // 权限
                ["perm.convert"] = "允许使用MIDI转音高命令",
                ["perm.convert.wide"] = "允许使用宽范围转换命令",
                ["perm.convert.back"] = "允许使用音高转MIDI命令",
                ["perm.batch"] = "允许使用批量转换命令",
                ["perm.list"] = "允许查看文件列表",
                ["perm.help"] = "允许查看帮助信息",
                ["perm.admin"] = "管理员权限（重载配置等）",

                // 重载消息
                ["reload.success"] = "[MidiConverter] 配置已重载！",
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(defaultTranslations, options);
            File.WriteAllText(filePath, json, Encoding.UTF8);
            
            TShock.Log.Info($"[MidiConverter] 已创建默认语言文件: {filePath}");
        }
        catch (Exception ex)
        {
            TShock.Log.Error($"[MidiConverter] 创建语言文件失败: {ex.Message}");
            LoadDefaultTranslations();
        }
    }

    /// <summary>
    /// 加载内存中的默认翻译（备用）
    /// </summary>
    private static void LoadDefaultTranslations()
    {
        Translations.Clear();
        Translations["plugin.name"] = "MidiConverter";
        Translations["plugin.description"] = "MIDI与音高文件双向转换插件";
        Translations["msg.convert.success"] = "✅ 转换成功！";
        Translations["msg.error.file.notfound"] = "❌ 未找到文件: {0}";
        Translations["msg.error.convert.failed"] = "❌ 转换失败！";
        Translations["reload.success"] = "[MidiConverter] 配置已重载！";
    }
}
