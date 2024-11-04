# modules/load_assets.py

import os
from moviepy.config import change_settings
# 设置你的 FFmpeg 路径（请确保这个路径下存在 ffmpeg.exe）
change_settings({"FFMPEG_BINARY": "C:\\ffmpeg\\bin\\ffmpeg.exe"})  # ← 按你真实路径改


from moviepy.editor import VideoFileClip, AudioFileClip
import srt

def load_assets(asset_dir):
    """
    读取素材文件并返回对应的 MoviePy 和数据对象
    """
    # 构建路径
    bg_path = os.path.join(asset_dir, "bg.mp4")
    voice_path = os.path.join(asset_dir, "voice.mp3")
    music_path = os.path.join(asset_dir, "music.mp3")
    subtitle_path = os.path.join(asset_dir, "subtitles.srt")
    title_path = os.path.join(asset_dir, "title.txt")

    # 加载素材
    print("✅ 加载背景视频...")
    bg_video = VideoFileClip(bg_path)

    print("✅ 加载人声音频...")
    voice_audio = AudioFileClip(voice_path)

    print("✅ 加载背景音乐...")
    music_audio = AudioFileClip(music_path)

    print("✅ 加载字幕文件...")
    with open(subtitle_path, "r", encoding="utf-8") as f:
        subtitle_data = list(srt.parse(f.read()))

    print("✅ 加载标题文本...")
    with open(title_path, "r", encoding="utf-8") as f:
        title_text = f.read().strip()

    return {
        "bg_video": bg_video,
        "voice_audio": voice_audio,
        "music_audio": music_audio,
        "subtitles": subtitle_data,
        "title_text": title_text
    }
