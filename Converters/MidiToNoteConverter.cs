using System.Text;
using MidiConverter.Models;
using MidiConverter.Parsers;

namespace MidiConverter.Converters;

/// <summary>
/// MIDI转音高文件转换器
/// 将标准MIDI文件转换为MusicPlayer插件可播放的音高文本格式
/// </summary>
public class MidiToNoteConverter
{
    /// <summary>
    /// MusicPlayer支持的标准音域（C4-C6）
    /// 超出此范围的音符将被映射或跳过
    /// </summary>
    private static readonly HashSet<string> ValidNotes = new(StringComparer.OrdinalIgnoreCase)
    {
        "C4", "C#4", "D4", "D#4", "E4", "F4", "F#4", "G4", "G#4", "A4", "A#4", "B4",
        "C5", "C#5", "D5", "D#5", "E5", "F5", "F#5", "G5", "G#5", "A5", "A#5", "B5",
        "C6"
    };

    /// <summary>
    /// 转换结果类
    /// </summary>
    public class ConversionResult
    {
        /// <summary>是否成功</summary>
        public bool Success { get; set; }
        
        /// <summary>错误信息（失败时）</summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>有效音符数</summary>
        public int NoteCount { get; set; }
        
        /// <summary>跳过的音符数（超出范围）</summary>
        public int SkippedNotes { get; set; }
        
        /// <summary>检测到的BPM</summary>
        public int Tempo { get; set; }
        
        /// <summary>已处理的音符组数</summary>
        public int ProcessedGroups { get; set; }
        
        /// <summary>总音符组数</summary>
        public int TotalGroups { get; set; }
    }

    /// <summary>
    /// 进度回调委托
    /// </summary>
    public Action<int, int, string>? OnProgress;

    /// <summary>
    /// 将MIDI文件转换为音高文件
    /// </summary>
    /// <param name="midiPath">输入MIDI文件路径</param>
    /// <param name="outputPath">输出音高文件路径</param>
    /// <param name="speed">MusicPlayer播放速度（写入文件第一行）</param>
    /// <param name="shortInterval">短间隔阈值（秒），小于此值的音符视为和弦</param>
    /// <param name="longInterval">长间隔阈值（秒），大于此值插入休止符</param>
    /// <param name="limitToValidRange">是否限制在C4-C6范围内</param>
    /// <returns>转换结果</returns>
    public ConversionResult Convert(
        string midiPath, 
        string outputPath, 
        int speed = 200, 
        float shortInterval = 0.1f, 
        float longInterval = 0.3f,
        bool limitToValidRange = true)
    {
        var result = new ConversionResult();

        try
        {
            // 解析MIDI文件
            var parser = new MidiParser();
            var midiFile = parser.ParseFile(midiPath);
            var noteEvents = midiFile.GetAllNoteEvents();
            
            if (noteEvents.Count == 0)
            {
                result.ErrorMessage = "MIDI文件中没有找到音符事件";
                return result;
            }

            var ticksPerQuarter = midiFile.Header.TicksPerQuarterNote;
            var bpm = midiFile.GetTempoAt(0);
            result.Tempo = bpm;

            // 按时间分组（处理和弦）
            var groupedNotes = GroupNotesByTime(noteEvents, shortInterval, ticksPerQuarter, bpm);
            result.TotalGroups = groupedNotes.Count;

            var outputLines = new List<string>();
            
            // 🔧 v1.2.0格式：记录speed和原始BPM，用|分隔
            // 格式：speed|originalBpm，如 "200|120"
            outputLines.Add($"{speed}|{bpm}");

            double previousTime = -1;
            int skippedCount = 0;
            int validNoteCount = 0;
            int processedGroups = 0;

            foreach (var group in groupedNotes)
            {
                var time = group.Key;
                var notes = group.Value;
                processedGroups++;

                // 报告进度
                if (processedGroups % 10 == 0 || processedGroups == 1 || processedGroups == groupedNotes.Count)
                {
                    OnProgress?.Invoke(processedGroups, groupedNotes.Count, 
                        $"正在处理第 {processedGroups}/{groupedGroups.Count} 组...");
                }

                // 处理时间间隔：插入休止符
                if (previousTime >= 0)
                {
                    var timeDiff = time - previousTime;
                    if (timeDiff > longInterval)
                    {
                        // 计算需要多少个0.25秒单位的休止符
                        var restCount = (int)Math.Round((timeDiff - shortInterval) / 0.25);
                        for (int i = 0; i < restCount && i < 20; i++) // 限制最多20个连续休止符
                        {
                            outputLines.Add("0");
                        }
                    }
                }

                // 转换音符
                var noteNames = new List<string>();
                foreach (var note in notes)
                {
                    var noteName = MidiNoteToName(note.Note);
                    
                    if (limitToValidRange)
                    {
                        // 标准模式：限制在C4-C6
                        if (!IsValidNote(noteName))
                        {
                            var closestNote = FindClosestValidNote(note.Note);
                            if (closestNote != null)
                            {
                                noteNames.Add(closestNote);
                                validNoteCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                        else
                        {
                            noteNames.Add(noteName);
                            validNoteCount++;
                        }
                    }
                    else
                    {
                        // 宽范围模式：保留所有音高
                        noteNames.Add(noteName);
                        validNoteCount++;
                    }
                }

                if (noteNames.Count > 0)
                {
                    outputLines.Add(string.Join(",", noteNames));
                }

                previousTime = time;
            }

            // 写入文件
            File.WriteAllLines(outputPath, outputLines, Encoding.UTF8);

            result.Success = true;
            result.NoteCount = validNoteCount;
            result.SkippedNotes = skippedCount;
            result.ProcessedGroups = processedGroups;

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"转换失败: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 将MIDI音符编号转换为标准音符名
    /// </summary>
    /// <param name="midiNote">MIDI音符编号（0-127）</param>
    /// <returns>音符名（如"C4", "F#5"）</returns>
    private string MidiNoteToName(byte midiNote)
    {
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        var octave = (midiNote / 12) - 1;
        var noteIndex = midiNote % 12;
        return $"{noteNames[noteIndex]}{octave}";
    }

    /// <summary>
    /// 按时间分组（识别和弦）
    /// 将时间差小于shortInterval的音符视为同时发声
    /// </summary>
    private SortedList<double, List<NoteEvent>> GroupNotesByTime(
        List<NoteEvent> notes, 
        float shortInterval, 
        int ticksPerQuarter, 
        int bpm)
    {
        var result = new SortedList<double, List<NoteEvent>>();
        
        foreach (var note in notes.OrderBy(n => n.StartTime))
        {
            var timeInSeconds = note.GetStartTimeInSeconds(ticksPerQuarter, bpm);
            
            // 查找是否已有接近的时间组
            var found = false;
            foreach (var kvp in result)
            {
                if (Math.Abs(kvp.Key - timeInSeconds) < shortInterval)
                {
                    kvp.Value.Add(note);
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                result[timeInSeconds] = new List<NoteEvent> { note };
            }
        }
        
        return result;
    }

    /// <summary>
    /// 检查音符是否在有效范围内（C4-C6）
    /// </summary>
    private bool IsValidNote(string noteName)
    {
        return ValidNotes.Contains(noteName);
    }

    /// <summary>
    /// 查找最接近的有效音符
    /// 将超出C4-C6范围的音符映射到边界
    /// </summary>
    /// <param name="midiNote">MIDI音符编号</param>
    /// <returns>映射后的音符名，无法映射时返回null</returns>
    private string? FindClosestValidNote(byte midiNote)
    {
        // 太低映射到C4，太高映射到C6
        if (midiNote < 48) return "C4";
        if (midiNote > 96) return "C6";
        
        // 在有效范围内找最接近的（C4=60, C6=84）
        var validMidiNumbers = new[] { 60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84 };
        var closest = validMidiNumbers.OrderBy(v => Math.Abs(v - midiNote)).First();
        
        return MidiNoteToName((byte)closest);
    }
}
