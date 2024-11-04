from PIL import Image, ImageDraw, ImageFont
import numpy as np
import matplotlib.pyplot as plt
import os
import platform
import subprocess

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

def render_text_image(text, font_path, fontsize=48, text_color="white", bg_color="black", image_size=(800, 200), output_path="font_test_pillow.png", preview=True):
    try:
        # 创建空白背景图像
        image = Image.new("RGB", image_size, bg_color)
        draw = ImageDraw.Draw(image)

        # 轻量化路径
        font = ImageFont.truetype(font_path, fontsize)

        # 处理文字的宽度和高度
        if hasattr(draw, 'textbbox'):  # 使用新版本 Pillow 的方法
            bbox = draw.textbbox((0, 0), text, font=font)
            text_width = bbox[2] - bbox[0]
            text_height = bbox[3] - bbox[1]
        else:  # 如果是旧版本 Pillow
            text_width, text_height = draw.textsize(text, font=font)

        # 计算文字的居中位置
        position = ((image_size[0] - text_width) // 2, (image_size[1] - text_height) // 2)
        draw.text(position, text, font=font, fill=text_color)

        # 保存
        image.save(output_path)
        print(f"图像已保存到: {output_path}")

        # 预览
        if preview:
            np_image = np.array(image)
            plt.imshow(np_image)
            plt.axis("off")
            plt.title("字体渲染预览")
            plt.show()

            open_image_with_default_viewer(output_path)

    except Exception as e:
        print(f"字体渲染失败: {e}")

if __name__ == "__main__":
    render_text_image(
        text="清华行楷字体测试",
        font_path=r"assets\\IDQingHuaXingKai.TTF",  # 按照你字体路径修改
        fontsize=60,
        text_color="white",
        bg_color="black",
        image_size=(1000, 200),
        output_path="font_test_pillow.png",
        preview=True
    )
