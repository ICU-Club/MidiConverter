# MidiConverter MIDI音乐转换器

- **作者**: 星梦
- **出处**: TShock官方群？
- 纯C#实现的MIDI解析与转换工具，支持MIDI与MusicPlayer音高文件双向转换。
- 支持精准BPM控制（音高转MIDI时保留原速并可倍速调整），自动处理C4-C6音域限制。
- 支持批量转换与进度显示。

## 指令

| 语法 | 别名 | 权限 | 说明 |
|--------------------------|:--:|:------------------:|:------------------------------------------------------|
| /mid转音高 <文件名> [速度] [短间隔] [长间隔] [-y] | m2n | midiconverter.convert | 将MIDI转换为MusicPlayer音高文件(C4-C6范围)，支持强制覆盖参数 |
| /宽范围转换 <文件名> [速度] [短间隔] [长间隔] [-y] | w2n | midiconverter.convert.wide | 将MIDI转换为宽音域音高文件（不限制C4-C6） |
| /音高转mid <文件名> [BPM] [乐器] [-y] | n2m | midiconverter.convert.back | 音高文件转回MIDI，支持BPM倍速调整（如原120BPM转60BPM即慢一倍） |
| /批量转换 <模式> <通配符> [参数...] [-y] | batch | midiconverter.batch | 批量转换文件，模式：midi2note/wide/note2midi |
| /转换列表 | clist | midiconverter.list | 查看各输入/输出文件夹中的文件列表 |
| /转换帮助 | chelp | midiconverter.help | 显示详细的使用帮助与参数说明 |
| /reload | 无 | tshock.cfg.reload | 重载插件配置（包含语言文件热重载） |

## 文件夹结构

插件会自动在 `TShock/MidiConverter/` 下创建以下目录：
- **MIDI输入/** - 放置待转换的.mid文件（标准模式）
- **宽范围MIDI输入/** - 放置待转换的.mid文件（宽音域模式）
- **音高输入/** - 放置待反向转换的.txt音高文件
- **宽范围输出/** - 宽范围转换的.txt输出目录
- **MIDI输出/** - 反向转换的.mid输出目录
- **错误日志/** - 转换失败的详细日志记录
- **Lang/** - 语言文件存放目录（默认zh-CN.json）

## 配置

配置文件位置：`tshock/MidiConverter/config.json`

```json5
{
  "defaultSpeed": 200,          // MusicPlayer播放速度（第一行写入值）
  "defaultShortInterval": 0.1,    // 和弦判定阈值（秒），小于此值视为同时发声
  "defaultLongInterval": 0.3,     // 休止符判定阈值（秒），大于此值插入停顿
  "defaultBpm": 120,              // 默认MIDI BPM（反向转换用）
  "defaultInstrument": 0,         // 默认乐器编号（0=钢琴，反向转换用）
  "autoMapOutOfRange": true,      // 超出C4-C6的音高是否自动映射到最近有效音
  "language": "zh-CN",            // 界面语言（当前仅支持zh-CN）
  "verboseLogging": false         // 是否启用详细日志记录
}

```

## 使用示例

### 基础转换（MIDI → 音高）
1. 将 `song.mid` 放入 `TShock/MidiConverter/MIDI输入/`
2. 执行 `/mid转音高 song` 或 `/mid转音高 song 200 0.1 0.3`
3. 生成的 `song.txt` 位于 `TShock/Songs/`，可直接被MusicPlayer插件使用

### 反向转换（音高 → MIDI）与倍速
1. 执行 `/音高转mid song 60`（假设原音高文件来自120BPM的MIDI）
2. 输出文件时长将为原曲的 2倍（慢一倍），并显示："速度变化: 慢 2.00 倍 (时长延长为 200%)"
3. 执行 `/音高转mid song 240` 则为快一倍，时长减半

### 批量转换
- `/批量转换 midi2note *.mid -y` - 转换所有mid到MusicPlayer目录并强制覆盖
- `/批量转换 wide boss*.mid` - 批量宽范围转换匹配文件
- `/批量转换 note2midi *.txt 140 -y` - 将所有音高文件以140BPM转回MIDI


## 更新日志

### v1.2.0
- 重构BPM计算算法，修复音高转MIDI时小范围BPM调整无效的问题（基于源BPM与目标BPM比例计算ticksPerStep）
- 修复休止符处理逻辑，确保停顿效果正确（强制结束所有活跃音符）
- 音高文件头格式更新为 `speed|originalBpm`
- 反向转换结果增强显示：源BPM、目标BPM、速度变化比例与时长变化百分比
- 添加 `/reload` 配置热重载支持

### v1.1.0
- 修复 `ConvertNoteToMidi` 方法参数不匹配错误（移除多余的config.DefaultBpm参数）
- 添加批量转换模式（midi2note/wide/note2midi）
- 增强错误日志记录（包含详细堆栈跟踪）

### v1.0.0
- 初始版本发布
- 支持MIDI与音高文件双向转换
- 支持C4-C6音域限制与自动映射

## 反馈

- 优先发issue -> ZKyore部分成员共同维护的插件库：`https://github.com/ICU-Club`
- 次优先：TShock官方群：`816771079` → 星梦：`1011819146`