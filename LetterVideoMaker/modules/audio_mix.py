from moviepy.editor import CompositeAudioClip

def mix_audio(voice_audio, music_audio, final_duration):
    """
    混合人声音频和背景音乐，并截断到最终视频时长。
    """
    # 截断背景音乐
    music_audio = music_audio.subclip(0, final_duration)

    # 设置背景音乐音量（可根据需要调节）
    music_audio = music_audio.volumex(0.3)

    # 合并音频
    mixed = CompositeAudioClip([music_audio, voice_audio])

    return mixed
