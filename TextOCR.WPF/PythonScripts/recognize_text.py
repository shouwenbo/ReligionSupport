import sys
import os

# 必须在导入 PaddleOCR 之前设置环境变量，避免中文用户名问题
os.environ['PADDLEX_HOME'] = 'C:/PaddleOCR_Models'
os.environ['HOME'] = 'C:/PaddleOCR_Models'
os.environ['USERPROFILE'] = 'C:/PaddleOCR_Models'
os.environ['PADDLE_HOME'] = 'C:/PaddleOCR_Models'

import cv2
from paddleocr import PaddleOCR
import re

def recognize_text(image_path, output_path):
    """识别单张图片中的文字"""
    try:
        # 初始化 PaddleOCR
        ocr = PaddleOCR(use_textline_orientation=False, lang='ch')
        
        # 读取图片并识别 - 使用新版 API，不带 cls 参数
        result = ocr.ocr(image_path)
        
        # 调试输出
        print(f"DEBUG: result type = {type(result)}", file=sys.stderr)
        print(f"DEBUG: result = {result}", file=sys.stderr)
        
        # 提取文字
        text = ""
        if result:
            for line in result:
                if line:
                    for word_info in line:
                        if isinstance(word_info, (list, tuple)) and len(word_info) >= 2:
                            extracted_text = word_info[1][0] if isinstance(word_info[1], (list, tuple)) else str(word_info[1])
                            print(f"DEBUG: extracted = '{extracted_text}'", file=sys.stderr)
                            text += extracted_text + " "
        
        text = text.strip()
        print(f"DEBUG: final text = '{text}'", file=sys.stderr)
        
        # 保存结果
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(text)
        
        print(text)
        return 0
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        return 1

def contains_chinese(text):
    """检查文本是否包含中文字符"""
    return bool(re.search(r'[\u4e00-\u9fa5]', text))

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: recognize_text.py <image_path> <output_path>", file=sys.stderr)
        sys.exit(1)
    
    image_path = sys.argv[1]
    output_path = sys.argv[2]
    
    sys.exit(recognize_text(image_path, output_path))
