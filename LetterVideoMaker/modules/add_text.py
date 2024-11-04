from moviepy.editor import VideoFileClip, ImageClip, CompositeVideoClip
from PIL import Image, ImageDraw, ImageFont
import numpy as np
import textwrap

def open_image_with_default_viewer(image_path):
    import platform
    import os
    import subprocess
    system_name = platform.system()
    try:
        if system_name == 'Windows':
            os.startfile(image_path)
        elif system_name == 'Darwin':
            subprocess.run(['open', image_path])
        else:
            subprocess.run(['xdg-open', image_path])
    except Exception as e:
        print(f"打开图片失败: {e}")

def render_text_image(text, font_path, fontsize=60, text_color="white", stroke_color="black", image_size=(1080, 200), max_width=None, max_chars_per_line=None):
    """
    用Pillow生成带有描边和阴影的文本图像，支持透明背景和自动换行
    """
    try:
        # 创建透明背景图像
        image = Image.new("RGBA", image_size, (0, 0, 0, 0))  # 透明背景
        draw = ImageDraw.Draw(image)

        # 使用Pillow字体
        font = ImageFont.truetype(font_path, fontsize)

        # 如果用户自己指定了 max_chars_per_line，直接使用
        if max_chars_per_line is not None:
            wrapped_text = textwrap.wrap(text, width=max_chars_per_line)
        elif max_width:
            # 如果没有指定 max_chars_per_line，则根据宽度计算
            max_chars_per_line = max_width // font.getsize('a')[0]  # 估算每个字符的宽度
            wrapped_text = textwrap.wrap(text, width=max_chars_per_line)
        else:
            wrapped_text = [text]  # 如果没有指定宽度，直接返回文本

        # 打印换行后的文本，检查是否正确换行
        print(f"Wrapped text:\n{wrapped_text}")

        # 计算每行文本的高度
        line_height = fontsize + 10  # 字体大小 + 行间距

        # 计算文本的总高度
        total_text_height = line_height * len(wrapped_text)

        # 计算文字的居中位置
        y_position = (image_size[1] - total_text_height) // 2

        # 绘制每一行
        for line in wrapped_text:
            # 计算每行文本的宽度
            bbox = draw.textbbox((0, 0), line, font=font)
            text_width = bbox[2] - bbox[0]  # bbox[2] 是右边界，bbox[0] 是左边界

            # 计算每行的横向位置
            x_position = (image_size[0] - text_width) // 2

            # 添加描边效果
            # 黑色描边
            draw.text((x_position - 2, y_position - 2), line, font=font, fill=stroke_color)
            draw.text((x_position + 2, y_position - 2), line, font=font, fill=stroke_color)
            draw.text((x_position - 2, y_position + 2), line, font=font, fill=stroke_color)
            draw.text((x_position + 2, y_position + 2), line, font=font, fill=stroke_color)

            # 正常白色文字
            draw.text((x_position, y_position), line, font=font, fill=text_color)

            # 更新y位置，绘制下一行
            y_position += line_height

        return image

    except Exception as e:
        print(f"字体渲染失败: {e}")
        return None

def add_subtitle_and_title(video_clip, subtitles, title_text, video_width=1080, video_height=1440):
    """
    在视频上添加标题和字幕，模拟剪映样式（白字 + 黑描边 + 发光近似）
    """

    # 生成标题图像
    title_image = render_text_image(
        title_text,
        font_path="assets/IDQingHuaXingKai.TTF",  # 直接用字体路径
        fontsize=100,
        text_color="#ffde00",
        stroke_color="black",
        image_size=(video_width - 40, 300),  # 调整标题大小
        # max_width=video_width - 80,  # 自动换行时的最大宽度
        max_chars_per_line=10
    )

    # 将标题图像转换为 ImageClip，确保透明背景
    if title_image:
        title_clip = ImageClip(np.array(title_image)).set_position(('center', 20)).set_duration(video_clip.duration)
        title_clip = title_clip.set_opacity(1)  # 确保透明背景
    else:
        print("标题生成失败，跳过标题显示")
        title_clip = None

    subtitle_clips = []
    bottom_margin = 100  # 与底部的间距

    for sub in subtitles:
        # 打印出字幕内容和最大宽度
        print(f"字幕内容: {sub.content}, 最大宽度: {video_width - 80}")

        # 生成字幕图像
        sub_image = render_text_image(
            sub.content,
            font_path="assets/IDQingHuaXingKai.TTF",  # 使用字体路径
            fontsize=80,
            text_color="white",
            stroke_color="black",
            image_size=(video_width - 80, 300),  # 设置字幕区域的大小
            # max_width=video_width - 100  # 自动换行时的最大宽度
            max_chars_per_line=12
        )

        # 将字幕图像转换为 ImageClip
        if sub_image:
            sub_clip = ImageClip(np.array(sub_image)) \
                .set_start(sub.start.total_seconds()) \
                .set_duration((sub.end - sub.start).total_seconds())

            # 计算字幕的垂直位置（底部对齐）
            subtitle_y = video_height - sub_clip.h - bottom_margin
            sub_clip = sub_clip.set_position(('center', subtitle_y))

            subtitle_clips.append(sub_clip)
        else:
            print(f"字幕 '{sub.content}' 生成失败，跳过此字幕")

    # 合成视频
    final = CompositeVideoClip(
        [video_clip] + [title_clip] + subtitle_clips if title_clip else subtitle_clips,
        size=(video_width, video_height)
    )

    return final