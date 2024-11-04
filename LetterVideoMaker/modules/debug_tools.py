# modules/debug_tools.py
from PIL import Image
import numpy as np

def preview_frame(video_clip, second=1.0):
    """
    从视频中截取指定时间的一帧，并用系统默认图像查看器打开。
    不会保存文件，只做预览用。
    """
    print(f"\n🧪 调试预览：截取第 {second} 秒画面...")
    frame = video_clip.get_frame(second)
    Image.fromarray(np.uint8(frame)).show()
    print("✅ 图像已使用默认查看器打开（不保存）")
