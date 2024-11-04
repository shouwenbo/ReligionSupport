from moviepy.editor import TextClip, CompositeVideoClip

def add_subtitle_and_title(video_clip, subtitles, title_text, video_width=1080, video_height=1440):
    """
    在视频上添加标题和字幕，模拟剪映样式（白字 + 黑描边 + 发光近似）。
    """

    # 标题
    title = TextClip(
        title_text,
        fontsize=60,
        color='white',
        font="assets/fonts/IDQingHuaXingKai.TTF",  # 直接用文件路径
        method='caption',
        size=(video_width - 40, None)
    ).set_position(('center', 20)).set_duration(video_clip.duration)

    subtitle_clips = []
    bottom_margin = 100  # 与底部的间距

    for sub in subtitles:
        # 创建字幕 clip（先不加位置）
        sub_clip = TextClip(
            sub.content,
            fontsize=60,
            color='white',
            font='SimHei',
            stroke_color='black',
            stroke_width=2,
            method='caption',
            size=(video_width - 80, None)
        ).set_start(sub.start.total_seconds()) \
         .set_duration((sub.end - sub.start).total_seconds())

        # 计算正确的垂直位置（底部对齐）
        subtitle_y = video_height - sub_clip.h - bottom_margin
        sub_clip = sub_clip.set_position(('center', subtitle_y))

        subtitle_clips.append(sub_clip)

    final = CompositeVideoClip(
        [video_clip, title] + subtitle_clips,
        size=(video_width, video_height)
    )

    return final
