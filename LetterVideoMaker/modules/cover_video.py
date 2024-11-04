from moviepy.editor import VideoFileClip, ImageClip, CompositeVideoClip
from PIL import Image, ImageDraw, ImageFont
import numpy as np
import textwrap

def render_text_image(text, font_path, fontsize=60, text_color="white", stroke_color="black", image_size=(1080, 200), max_width=None, max_chars_per_line=None):
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

def add_cover_and_title(cover_path, title_text, font_path, video_width=1080, video_height=1440):
    """
    给封面图像添加标题
    """
    # 打开封面图像
    cover_image = Image.open(cover_path)

    # 生成标题图像
    title_image = render_text_image(
        title_text,
        font_path=font_path,  # 直接用字体路径
        fontsize=100,  # 字体大小100
        text_color="#ffde00",  # 设置标题颜色为 Hexffde00
        stroke_color="black",
        image_size=(video_width - 40, 200),  # 调整标题大小
        max_chars_per_line=10
    )

    # 将标题图像转换为 ImageClip，确保透明背景
    if title_image:
        title_clip = ImageClip(np.array(title_image)).set_position(('center', 20)).set_duration(5)  # 显示5秒
        title_clip = title_clip.set_opacity(1)  # 确保透明背景
    else:
        print("标题生成失败，跳过标题显示")
        title_clip = None

    # 将封面图像转换为 ImageClip
    cover_clip = ImageClip(np.array(cover_image)).set_duration(5)

    # 合成封面和标题
    final = CompositeVideoClip(
        [cover_clip] + [title_clip] if title_clip else [cover_clip],
        size=(video_width, video_height)
    )

    return final
