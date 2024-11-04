from moviepy.editor import CompositeVideoClip, vfx

def process_video(bg_video, voice_audio, music_audio, target_width=1080, target_height=1440, extra_duration=5):
    """
    调整背景视频尺寸和时长。
    1. 尺寸调整为 target_width x target_height，中心裁剪拉满。
    2. 视频时长 = 人声音频长度 + extra_duration。
    3. 背景视频通过速度缩放匹配目标时长。
    返回处理好的视频 clip 对象，以及目标时长。
    """
    # ✅ 计算目标视频时长
    target_duration = voice_audio.duration + extra_duration

    # ✅ 缩放背景视频到合适尺寸
    video = bg_video.resize(height=target_height)
    if video.w < target_width:
        video = video.resize(width=target_width)

    # ✅ 裁剪中心区域
    x_center = video.w // 2
    y_center = video.h // 2
    x1 = max(0, x_center - target_width // 2)
    y1 = max(0, y_center - target_height // 2)
    video = video.crop(x1=x1, y1=y1, width=target_width, height=target_height)

    # ✅ 调整播放速度，使背景视频时长 = 目标时长
    original_duration = video.duration
    speed_factor = original_duration / target_duration
    video = video.fx(vfx.speedx, speed_factor)

    # ✅ 设置精确时长（避免误差）
    video = video.set_duration(target_duration)

    return video, target_duration
