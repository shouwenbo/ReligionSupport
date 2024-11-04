# modules/validate_audio.py

def validate_audio_lengths(voice_audio, music_audio, margin_seconds=5):
    """
    校验背景音乐长度是否比人声音频长 margin_seconds 秒以上。
    参数：
        voice_audio: MoviePy AudioFileClip 对象，表示人声音频
        music_audio: MoviePy AudioFileClip 对象，表示背景音乐
        margin_seconds: 额外时长，默认5秒
    返回：
        True 如果校验通过
    抛出：
        ValueError 如果音乐长度不足
    """
    voice_duration = voice_audio.duration
    music_duration = music_audio.duration

    print(f"人声音频长度：{voice_duration:.2f}秒")
    print(f"背景音乐长度：{music_duration:.2f}秒")
    required_length = voice_duration + margin_seconds

    if music_duration < required_length:
        raise ValueError(f"背景音乐长度不足，要求至少比人声音频长 {margin_seconds} 秒，但当前音乐只有 {music_duration:.2f} 秒。")

    print("✅ 背景音乐长度校验通过。")
    return True
