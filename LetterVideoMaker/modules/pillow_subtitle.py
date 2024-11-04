from moviepy.editor import CompositeVideoClip, ImageClip
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import numpy as np
import os


def generate_text_image(
    text: str,
    font_path: str,
    font_size: int,
    image_size: tuple[int, int],
    font_color: str = "white",
    stroke_width: int = 4,
    stroke_fill: str = "black",
    glow: bool = True,
    glow_radius: int = 15,
    glow_intensity: int = 4,
) -> Image.Image:
    """生成带描边和发光效果的文字图像"""
    W, H = image_size
    font = ImageFont.truetype(font_path, font_size)
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 文本尺寸
    text_bbox = draw.textbbox((0, 0), text, font=font, stroke_width=stroke_width)
    text_w = text_bbox[2] - text_bbox[0]
    text_h = text_bbox[3] - text_bbox[1]

    # 居中位置
    x = (W - text_w) // 2
    y = (H - text_h) // 2

    # 发光层（重复叠加模糊效果）
    if glow:
        glow_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        glow_draw = ImageDraw.Draw(glow_layer)
        for i in range(glow_intensity):
            glow_draw.text((x, y), text, font=font, fill=font_color)
        glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(radius=glow_radius))
        img = Image.alpha_composite(img, glow_layer)

    # 主文字层
    draw.text((x, y), text, font=font, fill=font_color, stroke_width=stroke_width, stroke_fill=stroke_fill)
    return img


def add_subtitle_with_pillow(video_clip, subtitles, font_path: str, font_size: int = 50):
    """使用 Pillow 渲染字幕文字，生成字幕图层"""
    subtitle_clips = []

    for sub in subtitles:
        # 生成字幕图像
        img = generate_text_image(
            text=sub.content,
            font_path=font_path,
            font_size=font_size,
            image_size=(video_clip.w, 200),
            font_color="white",
            stroke_width=4,
            stroke_fill="black",
            glow=True,
            glow_radius=10,
            glow_intensity=2
        )

        # 转为 MoviePy 的 ImageClip
        frame = np.array(img)
        clip = ImageClip(frame, ismask=False).set_duration((sub.end - sub.start).total_seconds())
        clip = clip.set_start(sub.start.total_seconds())
        clip = clip.set_position(("center", video_clip.h - 250))

        subtitle_clips.append(clip)

    return CompositeVideoClip([video_clip] + subtitle_clips)
