from moviepy.editor import TextClip
from moviepy.config import change_settings
import os
import platform
import subprocess

change_settings({
    "IMAGEMAGICK_BINARY": r"C:\Program Files\ImageMagick-7.1.1-Q16-HDRI\magick.exe"
})

def open_image_with_default_viewer(image_path):
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

def test_font_render(font_name):
    try:
        clip = TextClip(
            "你好，测试字体",
            fontsize=60,
            font=font_name,
            method='caption',
            color='black',
            bg_color='white'
        )
        image_path = "font_test.png"
        clip.save_frame(image_path, t=0)
        print("测试帧已保存：", image_path)
        open_image_with_default_viewer(image_path)
    except Exception as e:
        print("字体渲染失败:", e)

if __name__ == "__main__":
    font_name = "IDQingHuaXingKai"  # 你的字体名
    test_font_render(font_name)
