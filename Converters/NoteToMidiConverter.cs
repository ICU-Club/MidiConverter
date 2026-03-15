using System.Text;
using System.Text.RegularExpressions;
using MidiConverter.Models;
using MidiConverter.Parsers;

namespace MidiConverter.Converters;

/// <summary>
/// 音高文件转MIDI转换器
/// 将MusicPlayer音高文件转换回标准MIDI格式
/// 支持精准BPM控制（基于源BPM与目标BPM的比例计算）
/// </summary>
public class NoteToMidiConverter
{
    /// <summary>
    /// 转换结果类
    /// </summary>
    public class ConversionResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int NoteCount { get; set; }
        public int ProcessedGroups { get; set; }
        public int TotalGroups { get; set; }
        
        /// <summary>
        /// 实际使用的目标BPM
        /// </summary>
        public int ActualBpm { get; set; }
        
        /// <summary>
        /// 从音高文件解析的源BPM
        /// </summary>
        public int SourceBpm { get; set; }
    }

    /// <summary>
    /// 进度回调委托
    /// </summary>
    public Action<int, int, string>? OnProgress;

    /// <summary>
    /// 将音高文件转换为MIDI文件
    /// </summary>
    /// <param name="noteFilePath">输入音高文件路径</param>
    /// <param name="outputPath">输出MIDI文件路径</param>
    /// <param name="bpm">目标BPM（覆盖文件中的值，默认120）</param>
    /// <param name="instrument">乐器编号（0=钢琴）</param>
    /// <returns>转换结果</returns>
    public ConversionResult Convert(string noteFilePath, string outputPath, int bpm = 120, int instrument = 0)
    {
        var result = new ConversionResult();
        try
        {
            // 验证BPM有效性
            if (bpm <= 0 || bpm > 1000) bpm = 120;
            result.ActualBpm = bpm;

            // 解析音高文件
            var (sourceBpm, noteGroups) = ParseNoteFile(noteFilePath);
            
            if (noteGroups.Count == 0)
            {
                result.ErrorMessage = "音高文件中没有找到有效的音符数据";
                return result;
            }

            result.SourceBpm = sourceBpm;
            result.TotalGroups = noteGroups.Count;

            // 创建MIDI文件（传入源BPM用于计算时间伸缩比例）
            var midiFile = CreateMidiFile(noteGroups, bpm, instrument, sourceBpm);
            
            // 写入文件
            var parser = new MidiParser();
            parser.WriteToFile(midiFile, outputPath);

            result.Success = true;
            result.NoteCount = noteGroups.Sum(g => g.Notes.Count);
            result.ProcessedGroups = noteGroups.Count;
            
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"转换失败: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 解析音高文件
    /// 支持v1.2.0格式（speed|bpm）和旧格式（纯speed）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>元组（源BPM，音符组列表）</returns>
    private (int SourceBpm, List<NoteGroup> Groups) ParseNoteFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath, Encoding.UTF8);
        var groups = new List<NoteGroup>();
        var sourceBpm = 120; // 默认假设120BPM
        var headerRead = false;
        var currentTime = 0.0;
        const double timeStep = 0.25; // 每行代表0.25秒
        int lineCount = 0;

        foreach (var line in lines)
        {
            lineCount++;
            var trimmedLine = line.Trim();
            
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            if (lineCount % 10 == 0)
            {
                OnProgress?.Invoke(lineCount, lines.Length, $"正在解析第 {lineCount}/{lines.Length} 行...");
            }

            // 解析头部（第一行）
            if (!headerRead)
            {
                var parts = trimmedLine.Split('|');
                
                // 尝试解析源BPM（v1.2.0格式：speed|bpm）
                if (parts.Length >= 2 && int.TryParse(parts[1], out var parsedBpm) && parsedBpm > 20 && parsedBpm < 1000)
                {
                    sourceBpm = parsedBpm;
                }
                // 旧格式：只有speed，假设120BPM
                
                headerRead = true;
                continue;
            }

            // 解析音符行
            var noteGroup = new NoteGroup { Time = currentTime };
            var noteNames = trimmedLine.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var noteName in noteNames)
            {
                var trimmedName = noteName.Trim();
                
                // 处理休止符
                if (trimmedName == "0" || trimmedName == "-" || 
                    trimmedName.Equals("rest", StringComparison.OrdinalIgnoreCase))
                {
                    noteGroup.IsRest = true;
                    continue;
                }

                // 解析音符
                if (TryParseNote(trimmedName, out var noteInfo))
                {
                    noteGroup.Notes.Add(noteInfo);
                }
            }

            if (noteGroup.Notes.Count > 0 || noteGroup.IsRest)
            {
                groups.Add(noteGroup);
            }

            currentTime += timeStep;
        }

        OnProgress?.Invoke(lines.Length, lines.Length, "解析完成");
        return (sourceBpm, groups);
    }

    /// <summary>
    /// 尝试解析音符字符串
    /// </summary>
    private bool TryParseNote(string noteName, out NoteInfo noteInfo)
    {
        noteInfo = new NoteInfo();

        if (string.IsNullOrWhiteSpace(noteName))
            return false;

        // 尝试直接解析MIDI数字（0-127）
        if (byte.TryParse(noteName, out var directMidi) && directMidi <= 127)
        {
            noteInfo.Name = MidiNumberToName(directMidi);
            noteInfo.MidiNumber = directMidi;
            return true;
        }

        // 尝试解析音符名称（如 C3, C#4, Db5）
        var parsed = ParseNoteName(noteName);
        if (parsed.HasValue)
        {
            noteInfo.Name = parsed.Value.Name;
            noteInfo.MidiNumber = parsed.Value.MidiNumber;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 解析音符名称（支持 C-1 到 G9）
    /// </summary>
    private (string Name, byte MidiNumber)? ParseNoteName(string noteStr)
    {
        if (string.IsNullOrWhiteSpace(noteStr))
            return null;

        noteStr = noteStr.Trim();
        var match = Regex.Match(noteStr, @"^([A-Ga-g])(#|b|B)?(-?\d+)$");
        if (!match.Success)
            return null;

        var noteBase = match.Groups[1].Value.ToUpperInvariant();
        var accidental = match.Groups[2].Value;
        if (!int.TryParse(match.Groups[3].Value, out var octave))
            return null;

        // 计算音级（C=0, C#/Db=1, ..., B=11）
        var pitchClass = noteBase switch
        {
            "C" => 0, "D" => 2, "E" => 4, "F" => 5, "G" => 7, "A" => 9, "B" => 11,
            _ => -1
        };

        if (pitchClass == -1) return null;

        // 处理升降号
        if (!string.IsNullOrEmpty(accidental))
        {
            if (accidental == "#") pitchClass++;
            else if (accidental == "b" || accidental == "B") pitchClass--;
        }

        pitchClass = ((pitchClass % 12) + 12) % 12;
        var midiNumber = (octave + 1) * 12 + pitchClass;
        
        if (midiNumber < 0 || midiNumber > 127)
            return null;

        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        return ($"{noteNames[pitchClass]}{octave}", (byte)midiNumber);
    }

    /// <summary>
    /// MIDI编号转音符名
    /// </summary>
    private string MidiNumberToName(byte midiNumber)
    {
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        var octave = (midiNumber / 12) - 1;
        return $"{noteNames[midiNumber % 12]}{octave}";
    }

    /// <summary>
    /// 创建MIDI文件
    /// 🔧 关键修复：基于源BPM与目标BPM计算时间伸缩比例
    /// </summary>
    private MidiFile CreateMidiFile(List<NoteGroup> noteGroups, int targetBpm, int instrument, int sourceBpm)
    {
        var midiFile = new MidiFile();
        
        midiFile.Header.Format = 0;  // 单轨格式
        midiFile.Header.TrackCount = 1;
        midiFile.Header.Division = 480;  // 标准分辨率

        var track = new MidiTrack();
        midiFile.Tracks.Add(track);

        // 🔧 v1.2.0 核心修复：
        // 原公式 ticksPerStep = bpm * 2 会导致BPM变化被抵消
        // 新公式：基于源BPM与目标BPM的比例计算步长
        // 在480 Ticks/四分音符下，0.25秒 @ 120 BPM = 240 ticks
        // 比例因子 = 源BPM / 目标BPM
        double scaleFactor = sourceBpm / (double)targetBpm;
        var ticksPerStep = (int)(240.0 * scaleFactor);
        
        if (ticksPerStep < 1) ticksPerStep = 1;
        
        var microsecondsPerQuarter = 60000000 / targetBpm;
        var events = new List<MidiEvent>();

        // 轨道名称
        events.Add(new MidiEvent
        {
            Type = MidiEventType.Meta,
            AbsoluteTime = 0,
            MetaType = 0x03,
            MetaData = Encoding.ASCII.GetBytes($"Converted (Source: {sourceBpm} BPM)")
        });

        // 速度设置（Set Tempo）
        var tempoBytes = new byte[]
        {
            (byte)((microsecondsPerQuarter >> 16) & 0xFF),
            (byte)((microsecondsPerQuarter >> 8) & 0xFF),
            (byte)(microsecondsPerQuarter & 0xFF)
        };
        events.Add(new MidiEvent
        {
            Type = MidiEventType.Meta,
            AbsoluteTime = 0,
            MetaType = 0x51,
            MetaData = tempoBytes
        });

        // 乐器设置
        events.Add(new MidiEvent
        {
            Type = MidiEventType.ProgramChange,
            AbsoluteTime = 0,
            Channel = 0,
            ProgramNumber = (byte)instrument
        });

        // 生成音符事件
        long currentTick = 0;
        var activeNotes = new Dictionary<byte, long>(); // 音符 -> 计划结束时间
        int processedCount = 0;

        foreach (var group in noteGroups)
        {
            processedCount++;
            if (processedCount % 10 == 0 || processedCount == noteGroups.Count)
            {
                OnProgress?.Invoke(processedCount, noteGroups.Count, 
                    $"正在生成MIDI事件 {processedCount}/{noteGroups.Count}...");
            }

            // 🔧 v1.2.0 休止符修复：强制结束所有活跃音符
            if (group.IsRest)
            {
                foreach (var noteNumber in activeNotes.Keys.ToList())
                {
                    events.Add(new MidiEvent
                    {
                        Type = MidiEventType.NoteOff,
                        AbsoluteTime = currentTick,
                        Channel = 0,
                        Note = noteNumber,
                        Velocity = 0
                    });
                }
                activeNotes.Clear();
                
                currentTick += ticksPerStep;
                continue;
            }

            // 处理和弦
            foreach (var note in group.Notes)
            {
                // 如果音符已在播放，先停止（避免重叠）
                if (activeNotes.ContainsKey(note.MidiNumber))
                {
                    events.Add(new MidiEvent
                    {
                        Type = MidiEventType.NoteOff,
                        AbsoluteTime = currentTick,
                        Channel = 0,
                        Note = note.MidiNumber,
                        Velocity = 0
                    });
                    activeNotes.Remove(note.MidiNumber);
                }

                // Note On（力度80）
                events.Add(new MidiEvent
                {
                    Type = MidiEventType.NoteOn,
                    AbsoluteTime = currentTick,
                    Channel = 0,
                    Note = note.MidiNumber,
                    Velocity = 80
                });

                // 计划2个时间步后结束（八分音符）
                activeNotes[note.MidiNumber] = currentTick + (ticksPerStep * 2);
            }

            currentTick += ticksPerStep;
        }

        // 结束所有剩余音符
        foreach (var kvp in activeNotes.OrderBy(k => k.Value))
        {
            events.Add(new MidiEvent
            {
                Type = MidiEventType.NoteOff,
                AbsoluteTime = kvp.Value,
                Channel = 0,
                Note = kvp.Key,
                Velocity = 0
            });
        }

        // 轨道结束标记
        var lastTick = events.Count > 0 ? events.Max(e => e.AbsoluteTime) : 0;
        events.Add(new MidiEvent
        {
            Type = MidiEventType.Meta,
            AbsoluteTime = lastTick,
            MetaType = 0x2F,  // End of Track
            MetaData = Array.Empty<byte>()
        });

        // 计算DeltaTime
        track.Events = CalculateDeltaTimes(events);
        
        return midiFile;
    }

    /// <summary>
    /// 将绝对时间转换为相对时间（DeltaTime）
    /// </summary>
    private List<MidiEvent> CalculateDeltaTimes(List<MidiEvent> events)
    {
        var sorted = events.OrderBy(e => e.AbsoluteTime).ToList();
        long lastTime = 0;
        
        foreach (var evt in sorted)
        {
            evt.DeltaTime = evt.AbsoluteTime - lastTime;
            lastTime = evt.AbsoluteTime;
        }
        
        return sorted;
    }

    /// <summary>
    /// 音符组（一行）
    /// </summary>
    private class NoteGroup
    {
        public double Time { get; set; }
        public List<NoteInfo> Notes { get; set; } = new();
        public bool IsRest { get; set; }
    }

    /// <summary>
    /// 音符信息
    /// </summary>
    private class NoteInfo
    {
        public string Name { get; set; } = "";
        public byte MidiNumber { get; set; }
    }
}
