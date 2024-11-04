# -*- coding: utf-8 -*-

from moviepy.config import change_settings

# 设置 ImageMagick 路径
change_settings({
    "IMAGEMAGICK_BINARY": r"C:\Program Files\ImageMagick-7.1.1-Q16-HDRI\magick.exe"
})

# 是否启用调试模式（仅显示一帧图像）
DEBUG_MODE = True  # ← 改成 False 可关闭调试模式

from modules.load_assets import load_assets
from modules.validate_audio import validate_audio_lengths
from modules.process_video import process_video
from modules.add_text import add_subtitle_and_title
from modules.audio_mix import mix_audio
from modules.debug_tools import preview_frame  # ← 引入调试工具
from modules.cover_video import add_cover_and_title  # ← 引入封面处理函数

def main():
    print("开始加载素材...")
    assets = load_assets("assets")

    print("素材加载成功！")
    print(f"标题：{assets['title_text']}")
    print(f"字幕行数：{len(assets['subtitles'])}")
    print(f"背景视频时长：{assets['bg_video'].duration:.2f} 秒")
    print("所有素材加载完毕！")

    # **封面处理**：首先生成封面并加入视频
    print("\n开始生成封面...")
    cover_path = "assets/cover.jpg"  # 封面路径
    font_path = "assets/IDQingHuaXingKai.TTF"  # 字体路径
    title_text = assets["title_text"]  # 从素材中获取标题文字

    # 生成封面视频
    cover_video = add_cover_and_title(cover_path, title_text, font_path)
    if not cover_video:
        print("封面生成失败！")
        return

    print("\n开始校验音频长度...")
    validate_audio_lengths(assets["voice_audio"], assets["music_audio"])

    print("\n开始处理视频尺寸和时长...")
    processed_video, final_duration = process_video(
        assets["bg_video"],
        assets["voice_audio"],
        assets["music_audio"]
    )
    print(f"处理后视频时长：{processed_video.duration:.2f} 秒，分辨率：{processed_video.w}x{processed_video.h}")

    print("\n开始添加标题和字幕...")
    video_with_text = add_subtitle_and_title(processed_video, assets["subtitles"], assets["title_text"])
    print("标题和字幕添加完成。")

    if DEBUG_MODE:
        preview_frame(video_with_text, second=1.0)
        return

    print("\n开始混合音频...")
    mixed_audio = mix_audio(assets["voice_audio"], assets["music_audio"], final_duration)

    print(f"视频总时长将设置为：{final_duration:.2f} 秒")

    print("\n给视频添加混合音频...")
    final_video = video_with_text.set_duration(final_duration).set_audio(mixed_audio)

    # **合成封面视频和主视频**：将封面和主视频合并
    final_video_with_cover = CompositeVideoClip([cover_video, final_video.set_start(cover_video.duration)])

    print("\n开始导出视频...")
    output_path = "output/final_video.mp4"
    final_video.write_videofile(
        output_path,
        fps=30,
        audio_codec="aac",
        preset="medium",
        threads=4,
        bitrate="4000k",
        verbose=True
    )

    print(f"\n导出完成，文件保存在：{output_path}")

if __name__ == "__main__":
    main()
