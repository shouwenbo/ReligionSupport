import sys
import os

# 设置环境变量
os.environ['PADDLEX_HOME'] = 'C:/PaddleOCR_Models'
os.environ['HOME'] = 'C:/PaddleOCR_Models'
os.environ['USERPROFILE'] = 'C:/PaddleOCR_Models'
os.environ['PADDLE_HOME'] = 'C:/PaddleOCR_Models'

from paddleocr import PaddleOCR

# 测试图片
image_path = r"C:\PaddleOCR_Models\temp\debug_subtitle_193114.png"

print(f"测试图片: {image_path}")
print(f"文件存在: {os.path.exists(image_path)}")

# 初始化
ocr = PaddleOCR(use_textline_orientation=False, lang='ch')

# 识别
result = ocr.predict(image_path)

print(f"\n=== 原始结果类型 ===")
print(type(result))

print(f"\n=== 原始结果内容 ===")
print(result)

print(f"\n=== 结果结构 ===")
if isinstance(result, dict):
    print("结果是字典，键:", result.keys())
    for key, value in result.items():
        print(f"  {key}: {type(value)} = {value}")
elif isinstance(result, list):
    print(f"结果是列表，长度: {len(result)}")
    for i, item in enumerate(result):
        print(f"  [{i}]: {type(item)} = {item}")
else:
    print(f"结果类型: {type(result)}")
