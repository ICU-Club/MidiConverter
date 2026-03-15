using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using MidiConverter.Configuration;
using MidiConverter.Converters;
using MidiConverter.Localization;
using MidiConverter.Common;

namespace MidiConverter.Main;

/// <summary>
/// MidiConverter插件主类
/// 提供MIDI与音高文件的双向转换功能
/// </summary>
[ApiVersion(2, 1)]
public class MidiConverterPlugin : TerrariaPlugin
{
    #region 插件信息

    public override string Author => "MidiConverter Team";
    public override string Description => Lang.GetString("plugin.description");
    public override string Name => "MidiConverter";
    public override Version Version => new Version(1, 2, 0); // v1.2.0 修复BPM计算和休止符

    #endregion

    #region 路径配置

    /// <summary>
    /// 插件基础目录
    /// </summary>
    public static string BasePath => Path.Combine(TShock.SavePath, "MidiConverter");
    
    public static string MidiInputPath => Path.Combine(BasePath, "MIDI输入");
    public static string WideRangeMidiInputPath => Path.Combine(BasePath, "宽范围MIDI输入");
    public static string NoteInputPath => Path.Combine(BasePath, "音高输入");
    public static string WideRangeOutputPath => Path.Combine(BasePath, "宽范围输出");
    public static string MidiOutputPath => Path.Combine(BasePath, "MIDI输出");
    public static string LogPath => Path.Combine(BasePath, "错误日志");
    public static string MusicPlayerSongsPath => Path.Combine(TShock.SavePath, "Songs");

    #endregion

    #region 配置实例

    /// <summary>
    /// 当前配置实例
    /// </summary>
    public static Config? Configuration { get; private set; }

    /// <summary>
    /// 命令列表（用于Dispose时注销）
    /// </summary>
    private readonly Command[] _commands;

    #endregion

    public MidiConverterPlugin(Main game) : base(game)
    {
        _commands = new Command[]
        {
            new Command(Permissions.Convert, this.ConvertMidiToNote, "mid转音高", "m2n"),
            new Command(Permissions.ConvertWide, this.ConvertWideRangeMidi, "宽范围转换", "w2n"),
            new Command(Permissions.ConvertBack, this.ConvertNoteToMidi, "音高转mid", "n2m"),
            new Command(Permissions.Batch, this.BatchConvert, "批量转换", "batch"),
            new Command(Permissions.List, this.ListFiles, "转换列表", "clist"),
            new Command(Permissions.Help, this.ShowHelp, "转换帮助", "chelp"),
        };
    }

    /// <summary>
    /// 插件初始化
    /// </summary>
    public override void Initialize()
    {
        // 加载配置
        Configuration = Config.Load();
        
        // 加载语言
        Lang.LoadLanguage(Configuration.Language);
        
        // 创建必要目录
        EnsureDirectoriesExist();
        
        // 注册命令
        foreach (var cmd in _commands)
        {
            Commands.ChatCommands.Add(cmd);
        }

        // 注册重载钩子
        GeneralHooks.ReloadEvent += OnReload;
        
        TShock.Log.Info(Lang.GetString("[MidiConverter] 插件已加载，版本 {0}", Version));
    }

    /// <summary>
    /// 插件卸载
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var cmd in _commands)
            {
                Commands.ChatCommands.Remove(cmd);
            }
            GeneralHooks.ReloadEvent -= OnReload;
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// 配置重载处理
    /// </summary>
    private void OnReload(ReloadEventArgs args)
    {
        try
        {
            Configuration = Config.Load();
            EnsureDirectoriesExist();
            Lang.LoadLanguage(Configuration.Language);
            
            args.Player.SendSuccessMessage(Lang.GetString("reload.success"));
            TShock.Log.Info($"[MidiConverter] 配置已重载，语言: {Configuration.Language}");
        }
        catch (Exception ex)
        {
            args.Player.SendErrorMessage($"[MidiConverter] 重载失败: {ex.Message}");
            TShock.Log.Error($"[MidiConverter] 重载异常: {ex}");
        }
    }

    /// <summary>
    /// 确保所有工作目录存在
    /// </summary>
    private void EnsureDirectoriesExist()
    {
        try
        {
            Directory.CreateDirectory(BasePath);
            Directory.CreateDirectory(MidiInputPath);
            Directory.CreateDirectory(WideRangeMidiInputPath);
            Directory.CreateDirectory(NoteInputPath);
            Directory.CreateDirectory(WideRangeOutputPath);
            Directory.CreateDirectory(MidiOutputPath);
            Directory.CreateDirectory(LogPath);
            Directory.CreateDirectory(MusicPlayerSongsPath);
            
            var langPath = Path.Combine(BasePath, "Lang");
            Directory.CreateDirectory(langPath);
        }
        catch (Exception ex)
        {
            TShock.Log.Error($"[MidiConverter] 创建目录失败: {ex.Message}");
        }
    }

    #region 命令处理

    /// <summary>
    /// MIDI转音高命令处理
    /// </summary>
    private void ConvertMidiToNote(CommandArgs args)
    {
        var player = args.Player;
        
        if (args.Parameters.Count == 0)
        {
            SendUsage(player, "cmd.convert");
            return;
        }

        var fileName = args.Parameters[0];
        var config = Configuration ?? new Config();
        
        bool forceOverwrite = args.Parameters.Any(p => p == "-y" || p == "--force" || p == "-f");
        var cleanParams = args.Parameters.Where(p => p != "-y" && p != "--force" && p != "-f").ToList();
        
        var speed = cleanParams.Count > 1 && int.TryParse(cleanParams[1], out var s) ? s : config.DefaultSpeed;
        var shortInterval = cleanParams.Count > 2 && float.TryParse(cleanParams[2], out var si) ? si : config.DefaultShortInterval;
        var longInterval = cleanParams.Count > 3 && float.TryParse(cleanParams[3], out var li) ? li : config.DefaultLongInterval;

        var midiPath = FindMidiFile(fileName, MidiInputPath);
        if (midiPath == null)
        {
            player.SendErrorMessage(Lang.GetString("msg.error.file.notfound", fileName));
            player.SendInfoMessage(Lang.GetString("msg.error.folder.empty", MidiInputPath));
            return;
        }

        var outputFileName = Path.GetFileNameWithoutExtension(fileName) + ".txt";
        var outputPath = Path.Combine(MusicPlayerSongsPath, outputFileName);

        if (File.Exists(outputPath) && !forceOverwrite)
        {
            player.SendWarningMessage(Lang.GetString("msg.file.exists", outputFileName));
            player.SendInfoMessage(Lang.GetString("msg.file.overwrite"));
            return;
        }

        try
        {
            player.SendInfoMessage($"正在转换: {Path.GetFileName(midiPath)}...");
            if (forceOverwrite && File.Exists(outputPath))
                player.SendWarningMessage(Lang.GetString("msg.file.force"));
            
            var converter = new MidiToNoteConverter();
            SetupProgress(converter, player);
            
            var result = converter.Convert(midiPath, outputPath, speed, shortInterval, longInterval, true);

            if (result.Success)
            {
                player.SendSuccessMessage(Lang.GetString("msg.convert.success"));
                player.SendSuccessMessage(Lang.GetString("msg.output.file", outputPath));
                player.SendInfoMessage(Lang.GetString("msg.note.count", result.NoteCount, result.Tempo));
                if (result.SkippedNotes > 0)
                    player.SendWarningMessage(Lang.GetString("msg.skipped.notes", result.SkippedNotes));
            }
            else
            {
                HandleError(player, "MidiToNote", fileName, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            HandleException(player, "MidiToNote", fileName, ex);
        }
    }

    /// <summary>
    /// 宽范围转换命令处理
    /// </summary>
    private void ConvertWideRangeMidi(CommandArgs args)
    {
        var player = args.Player;
        
        if (args.Parameters.Count == 0)
        {
            SendUsage(player, "cmd.wide");
            return;
        }

        var fileName = args.Parameters[0];
        var config = Configuration ?? new Config();
        
        bool forceOverwrite = args.Parameters.Any(p => p == "-y" || p == "--force" || p == "-f");
        var cleanParams = args.Parameters.Where(p => p != "-y" && p != "--force" && p != "-f").ToList();
        
        var speed = cleanParams.Count > 1 && int.TryParse(cleanParams[1], out var s) ? s : config.DefaultSpeed;
        var shortInterval = cleanParams.Count > 2 && float.TryParse(cleanParams[2], out var si) ? si : config.DefaultShortInterval;
        var longInterval = cleanParams.Count > 3 && float.TryParse(cleanParams[3], out var li) ? li : config.DefaultLongInterval;

        var midiPath = FindMidiFile(fileName, WideRangeMidiInputPath);
        if (midiPath == null)
        {
            player.SendErrorMessage(Lang.GetString("msg.error.file.notfound", fileName));
            player.SendInfoMessage(Lang.GetString("msg.error.folder.empty", WideRangeMidiInputPath));
            return;
        }

        var outputFileName = Path.GetFileNameWithoutExtension(fileName) + ".txt";
        var outputPath = Path.Combine(WideRangeOutputPath, outputFileName);

        if (File.Exists(outputPath) && !forceOverwrite)
        {
            player.SendWarningMessage(Lang.GetString("msg.file.exists", outputFileName));
            player.SendInfoMessage(Lang.GetString("msg.file.overwrite"));
            return;
        }

        try
        {
            player.SendInfoMessage($"正在宽范围转换: {Path.GetFileName(midiPath)}...");
            
            var converter = new MidiToNoteConverter();
            SetupProgress(converter, player);
            
            var result = converter.Convert(midiPath, outputPath, speed, shortInterval, longInterval, false);

            if (result.Success)
            {
                player.SendSuccessMessage(Lang.GetString("msg.wide.success"));
                player.SendSuccessMessage(Lang.GetString("msg.output.file", outputPath));
                player.SendInfoMessage(Lang.GetString("msg.note.count", result.NoteCount, result.Tempo));
            }
            else
            {
                HandleError(player, "WideRangeMidi", fileName, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            HandleException(player, "WideRangeMidi", fileName, ex);
        }
    }

    /// <summary>
    /// 音高转MIDI命令处理（v1.2.0 增强版）
    /// </summary>
    private void ConvertNoteToMidi(CommandArgs args)
    {
        var player = args.Player;
        
        if (args.Parameters.Count == 0)
        {
            SendUsage(player, "cmd.back");
            return;
        }

        var fileName = args.Parameters[0];
        var config = Configuration ?? new Config();
        
        bool forceOverwrite = args.Parameters.Any(p => p == "-y" || p == "--force" || p == "-f");
        var cleanParams = args.Parameters.Where(p => p != "-y" && p != "--force" && p != "-f").ToList();
        
        var bpm = cleanParams.Count > 1 && int.TryParse(cleanParams[1], out var b) ? b : config.DefaultBpm;
        var instrument = cleanParams.Count > 2 && int.TryParse(cleanParams[2], out var i) ? i : config.DefaultInstrument;

        var notePath = FindNoteFile(fileName, NoteInputPath);
        if (notePath == null)
        {
            player.SendErrorMessage(Lang.GetString("msg.error.file.notfound", fileName));
            player.SendInfoMessage(Lang.GetString("msg.error.folder.empty", NoteInputPath));
            return;
        }

        var outputFileName = Path.GetFileNameWithoutExtension(fileName) + ".mid";
        var outputPath = Path.Combine(MidiOutputPath, outputFileName);

        if (File.Exists(outputPath) && !forceOverwrite)
        {
            player.SendWarningMessage(Lang.GetString("msg.file.exists", outputFileName));
            player.SendInfoMessage(Lang.GetString("msg.file.overwrite"));
            return;
        }

        try
        {
            player.SendInfoMessage($"正在反向转换: {Path.GetFileName(notePath)}...");
            
            var converter = new NoteToMidiConverter();
            SetupProgress(converter, player);
            
            // v1.2.0 修复：移除多余的第5个参数
            var result = converter.Convert(notePath, outputPath, bpm, instrument);

            if (result.Success)
            {
                player.SendSuccessMessage(Lang.GetString("msg.back.success"));
                player.SendSuccessMessage(Lang.GetString("msg.output.file", outputPath));
                
                // 增强显示：源BPM、目标BPM、速度变化
                player.SendInfoMessage($"音符数: {result.NoteCount}, 源BPM: {result.SourceBpm}, 目标BPM: {result.ActualBpm}, 乐器: {instrument}");
                
                if (result.SourceBpm != result.ActualBpm && result.SourceBpm > 0)
                {
                    double ratio = (double)result.ActualBpm / result.SourceBpm;
                    string change = ratio > 1 ? $"快 {ratio:F2} 倍" : $"慢 {1/ratio:F2} 倍";
                    string timeChange = ratio > 1 ? $"时长缩短为 {1/ratio:F0}%" : $"时长延长为 {ratio*100:F0}%";
                    player.SendInfoMessage($"速度变化: {change} ({timeChange})");
                }
                
                TShock.Log.Info($"[MidiConverter] {player.Name} 反向转换成功 -> {outputFileName} (源:{result.SourceBpm}->目标:{result.ActualBpm})");
            }
            else
            {
                HandleError(player, "NoteToMidi", fileName, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            HandleException(player, "NoteToMidi", fileName, ex);
        }
    }

    /// <summary>
    /// 批量转换命令处理
    /// </summary>
    private void BatchConvert(CommandArgs args)
    {
        var player = args.Player;
        
        if (args.Parameters.Count < 2)
        {
            SendUsage(player, "cmd.batch");
            return;
        }

        var mode = args.Parameters[0].ToLower();
        var pattern = args.Parameters[1];
        bool forceOverwrite = args.Parameters.Any(p => p == "-y" || p == "--force" || p == "-f");
        var cleanParams = args.Parameters.Where(p => p != "-y" && p != "--force" && p != "-f").ToList();
        var config = Configuration ?? new Config();

        // 确定输入输出路径
        var (inputFolder, outputFolder, searchPattern) = mode switch
        {
            "midi2note" or "m2n" => (MidiInputPath, MusicPlayerSongsPath, pattern.Contains('.') ? pattern : pattern + ".mid"),
            "wide" or "w2n" => (WideRangeMidiInputPath, WideRangeOutputPath, pattern.Contains('.') ? pattern : pattern + ".mid"),
            "note2midi" or "n2m" => (NoteInputPath, MidiOutputPath, pattern.Contains('.') ? pattern : pattern + ".txt"),
            _ => (null, null, null)
        };

        if (inputFolder == null)
        {
            player.SendErrorMessage(Lang.GetString("msg.error.unknown.mode", mode));
            return;
        }

        if (!Directory.Exists(inputFolder))
        {
            player.SendErrorMessage(Lang.GetString("msg.error.file.notfound", inputFolder));
            return;
        }

        var files = Directory.GetFiles(inputFolder, searchPattern);
        if (files.Length == 0)
        {
            player.SendWarningMessage(Lang.GetString("msg.batch.nofiles", searchPattern));
            return;
        }

        player.SendInfoMessage($"🚀 开始批量转换，共找到 {files.Length} 个文件...");
        
        int successCount = 0, failCount = 0, skipCount = 0;
        int current = 0;

        foreach (var file in files)
        {
            current++;
            var fn = Path.GetFileNameWithoutExtension(file);
            var ext = (mode == "note2midi" || mode == "n2m") ? ".mid" : ".txt";
            var outPath = Path.Combine(outputFolder, fn + ext);
            
            player.SendInfoMessage($"[{current}/{files.Length}] 正在转换: {Path.GetFileName(file)}");
            
            if (File.Exists(outPath) && !forceOverwrite)
            {
                player.SendWarningMessage($"跳过已存在: {fn}{ext}");
                skipCount++;
                continue;
            }
            
            try
            {
                bool success = false;
                
                if (mode == "midi2note" || mode == "m2n")
                {
                    var conv = new MidiToNoteConverter();
                    var res = conv.Convert(file, outPath, config.DefaultSpeed, config.DefaultShortInterval, config.DefaultLongInterval, true);
                    success = res.Success;
                }
                else if (mode == "wide" || mode == "w2n")
                {
                    var conv = new MidiToNoteConverter();
                    var res = conv.Convert(file, outPath, 
                        cleanParams.Count > 2 && int.TryParse(cleanParams[2], out var s) ? s : config.DefaultSpeed,
                        cleanParams.Count > 3 && float.TryParse(cleanParams[3], out var si) ? si : config.DefaultShortInterval,
                        cleanParams.Count > 4 && float.TryParse(cleanParams[4], out var li) ? li : config.DefaultLongInterval,
                        false);
                    success = res.Success;
                }
                else if (mode == "note2midi" || mode == "n2m")
                {
                    var conv = new NoteToMidiConverter();
                    var res = conv.Convert(file, outPath,
                        cleanParams.Count > 2 && int.TryParse(cleanParams[2], out var b) ? b : config.DefaultBpm,
                        cleanParams.Count > 3 && int.TryParse(cleanParams[3], out var i) ? i : config.DefaultInstrument);
                    success = res.Success;
                }
                
                if (success)
                {
                    successCount++;
                    player.SendSuccessMessage($"  ✅ {Path.GetFileName(file)} -> {fn}{ext}");
                }
                else
                {
                    failCount++;
                    player.SendErrorMessage($"  ❌ 转换失败: {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                failCount++;
                player.SendErrorMessage($"  ❌ 异常: {ex.Message}");
                LogError("BatchConvert", file, ex.Message);
            }
        }

        player.SendMessage("=== 批量转换完成 ===", Microsoft.Xna.Framework.Color.Yellow);
        player.SendSuccessMessage(Lang.GetString("msg.batch.success", successCount, failCount, skipCount));
    }

    /// <summary>
    /// 文件列表命令
    /// </summary>
    private void ListFiles(CommandArgs args)
    {
        var player = args.Player;
        var sb = new StringBuilder();
        
        sb.AppendLine(Lang.GetString("list.title"));
        
        AppendFileList(sb, "list.folder.midi", MidiInputPath, "*.mid");
        AppendFileList(sb, "list.folder.wide", WideRangeMidiInputPath, "*.mid");
        AppendFileList(sb, "list.folder.note", NoteInputPath, "*.txt");
        AppendFileList(sb, "list.folder.songs", MusicPlayerSongsPath, "*.txt");

        player.SendMessage(sb.ToString(), Microsoft.Xna.Framework.Color.Yellow);
    }

    /// <summary>
    /// 帮助命令
    /// </summary>
    private void ShowHelp(CommandArgs args)
    {
        var player = args.Player;
        var sb = new StringBuilder();
        
        sb.AppendLine(Lang.GetString("help.title"));
        sb.AppendLine(Lang.GetString("help.version", Version, Author));
        
        sb.AppendLine(Lang.GetString("help.folders.title"));
        sb.AppendLine(Lang.GetString("help.folders.midi"));
        sb.AppendLine(Lang.GetString("help.folders.wide"));
        sb.AppendLine(Lang.GetString("help.folders.note"));
        sb.AppendLine(Lang.GetString("help.folders.wideout"));
        sb.AppendLine(Lang.GetString("help.folders.midiout"));
        sb.AppendLine(Lang.GetString("help.folders.log"));
        
        sb.AppendLine(Lang.GetString("help.commands.title"));
        sb.AppendLine(Lang.GetString("help.commands.convert"));
        sb.AppendLine(Lang.GetString("help.commands.convert.desc"));
        sb.AppendLine(Lang.GetString("help.commands.wide"));
        sb.AppendLine(Lang.GetString("help.commands.wide.desc"));
        sb.AppendLine(Lang.GetString("help.commands.back"));
        sb.AppendLine(Lang.GetString("help.commands.back.desc"));
        sb.AppendLine(Lang.GetString("help.commands.batch"));
        sb.AppendLine(Lang.GetString("help.commands.batch.desc"));
        sb.AppendLine(Lang.GetString("help.commands.list"));
        sb.AppendLine(Lang.GetString("help.commands.help"));
        
        sb.AppendLine(Lang.GetString("help.params.title"));
        sb.AppendLine(Lang.GetString("help.params.speed"));
        sb.AppendLine(Lang.GetString("help.params.short"));
        sb.AppendLine(Lang.GetString("help.params.long"));
        sb.AppendLine(Lang.GetString("help.params.bpm"));
        sb.AppendLine(Lang.GetString("help.params.instrument"));
        sb.AppendLine(Lang.GetString("help.params.force"));
        
        sb.AppendLine(Lang.GetString("help.batch.title"));
        sb.AppendLine(Lang.GetString("help.batch.example1"));
        sb.AppendLine(Lang.GetString("help.batch.example1.desc"));
        sb.AppendLine(Lang.GetString("help.batch.example2"));
        sb.AppendLine(Lang.GetString("help.batch.example2.desc"));
        sb.AppendLine(Lang.GetString("help.batch.example3"));
        sb.AppendLine(Lang.GetString("help.batch.example3.desc"));
        
        sb.AppendLine(Lang.GetString("help.notes.title"));
        sb.AppendLine(Lang.GetString("help.notes.range"));
        sb.AppendLine(Lang.GetString("help.notes.skip"));
        sb.AppendLine(Lang.GetString("help.notes.console"));
        sb.AppendLine(Lang.GetString("help.notes.progress"));

        player.SendMessage(sb.ToString(), Microsoft.Xna.Framework.Color.LightGreen);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 设置转换器进度回调
    /// </summary>
    private void SetupProgress(MidiToNoteConverter converter, TSPlayer player)
    {
        int lastProgress = 0;
        converter.OnProgress = (current, total, msg) =>
        {
            var progress = (int)((double)current / total * 100);
            if (progress - lastProgress >= 20 || current == 1 || current == total)
            {
                player.SendInfoMessage(Lang.GetString("msg.progress", progress, current, total));
                lastProgress = progress;
            }
        };
    }

    /// <summary>
    /// 设置转换器进度回调（重载）
    /// </summary>
    private void SetupProgress(NoteToMidiConverter converter, TSPlayer player)
    {
        int lastProgress = 0;
        converter.OnProgress = (current, total, msg) =>
        {
            var progress = (int)((double)current / total * 100);
            if (progress - lastProgress >= 20 || current == 1 || current == total)
            {
                player.SendInfoMessage(Lang.GetString("msg.progress", progress, current, total));
                lastProgress = progress;
            }
        };
    }

    /// <summary>
    /// 发送使用说明
    /// </summary>
    private void SendUsage(TSPlayer player, string prefix)
    {
        player.SendInfoMessage(Lang.GetString($"{prefix}.usage"));
        player.SendInfoMessage(Lang.GetString($"{prefix}.example"));
        player.SendInfoMessage(Lang.GetString($"{prefix}.desc"));
        if (prefix != "cmd.batch") 
            player.SendInfoMessage(Lang.GetString("help.params.force"));
    }

    /// <summary>
    /// 查找MIDI文件（支持.mid和.midi扩展名）
    /// </summary>
    private string? FindMidiFile(string fileName, string folderPath)
    {
        foreach (var ext in new[] { "", ".mid", ".midi" })
        {
            var path = Path.Combine(folderPath, fileName + ext);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    /// <summary>
    /// 查找音高文件（支持.txt扩展名）
    /// </summary>
    private string? FindNoteFile(string fileName, string folderPath)
    {
        foreach (var ext in new[] { "", ".txt" })
        {
            var path = Path.Combine(folderPath, fileName + ext);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    /// <summary>
    /// 附加文件列表到StringBuilder
    /// </summary>
    private void AppendFileList(StringBuilder sb, string folderKey, string path, string pattern)
    {
        sb.AppendLine(Lang.GetString(folderKey, path));
        if (!Directory.Exists(path))
        {
            sb.AppendLine(Lang.GetString("list.empty"));
            return;
        }
        
        var files = Directory.GetFiles(path, pattern);
        if (files.Length == 0)
        {
            sb.AppendLine(Lang.GetString("list.empty"));
        }
        else
        {
            foreach (var f in files.Take(10))
                sb.AppendLine($"  - {Path.GetFileName(f)}");
            if (files.Length > 10)
                sb.AppendLine(Lang.GetString("list.more", files.Length - 10));
        }
    }

    /// <summary>
    /// 处理转换错误
    /// </summary>
    private void HandleError(TSPlayer player, string operation, string fileName, string? error)
    {
        player.SendErrorMessage(Lang.GetString("msg.error.convert.failed"));
        player.SendErrorMessage($"错误: {error}");
        LogError(operation, fileName, error ?? "未知错误");
    }

    /// <summary>
    /// 处理异常
    /// </summary>
    private void HandleException(TSPlayer player, string operation, string fileName, Exception ex)
    {
        player.SendErrorMessage(Lang.GetString("msg.error.convert.exception"));
        var errorMsg = GetDetailedError(ex);
        LogError(operation, fileName, errorMsg);
        TShock.Log.Error($"[MidiConverter] {operation}异常: {ex.Message}");
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    private void LogError(string operation, string fileName, string errorDetails)
    {
        try
        {
            var logFile = Path.Combine(LogPath, $"error_{DateTime.Now:yyyyMMdd}.log");
            var separator = new string('=', 50);
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 操作: {operation}, 文件: {fileName}\n{errorDetails}\n{separator}\n";
            File.AppendAllText(logFile, logEntry);
        }
        catch { /* 忽略日志写入失败 */ }
    }

    /// <summary>
    /// 获取详细错误信息
    /// </summary>
    private string GetDetailedError(Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"异常类型: {ex.GetType().FullName}");
        sb.AppendLine($"异常消息: {ex.Message}");
        sb.AppendLine($"堆栈跟踪:\n{ex.StackTrace}");
        
        if (ex.InnerException != null)
        {
            sb.AppendLine($"\n内部异常: {ex.InnerException.GetType().FullName}");
            sb.AppendLine($"消息: {ex.InnerException.Message}");
        }
        
        return sb.ToString();
    }

    #endregion
}
