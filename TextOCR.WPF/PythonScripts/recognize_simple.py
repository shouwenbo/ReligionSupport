import sys
import os
import cv2

# 必须在导入 PaddleOCR 之前设置环境变量
os.environ['PADDLEX_HOME'] = 'C:/PaddleOCR_Models'
os.environ['HOME'] = 'C:/PaddleOCR_Models'
os.environ['USERPROFILE'] = 'C:/PaddleOCR_Models'
os.environ['PADDLE_HOME'] = 'C:/PaddleOCR_Models'

from paddleocr import PaddleOCR
import re

def contains_chinese(text):
    """检查文本是否包含中文字符"""
    return bool(re.search(r'[\u4e00-\u9fa5]', text))

def recognize_text_simple(image_path, output_path):
    """识别单张图片中的文字 - 使用PaddleOCR 3.1.0官方推荐方式"""
    try:
        print(f"[PaddleOCR] 开始识别图片: {image_path}", file=sys.stderr)
        
        # 检查图片是否存在
        if not os.path.exists(image_path):
            print(f"[PaddleOCR] 错误：图片不存在 {image_path}", file=sys.stderr)
            return 1
        
        # 读取图片验证
        img = cv2.imread(image_path)
        if img is None:
            print(f"[PaddleOCR] 错误：无法读取图片", file=sys.stderr)
            return 1
        
        print(f"[PaddleOCR] 图片尺寸: {img.shape}", file=sys.stderr)
        
        # 初始化 PaddleOCR - 使用官方推荐的最简配置
        print(f"[PaddleOCR] 初始化 OCR 引擎 (版本 3.1.0)...", file=sys.stderr)
        ocr = PaddleOCR(lang='ch')
        
        print(f"[PaddleOCR] 开始识别...", file=sys.stderr)
        # 使用 predict() 方法（官方文档推荐）
        result = ocr.predict(input=image_path)
        
        print(f"[PaddleOCR] 识别完成，结果类型: {type(result)}", file=sys.stderr)
        
        # 提取文字 - 根据官方返回格式
        text = ""
        
        if result:
            print(f"[PaddleOCR] 结果数量: {len(result)}", file=sys.stderr)
            for idx, res in enumerate(result):
                # 每个 res 对象应该有 text 属性
                if hasattr(res, 'text'):
                    extracted_text = res.text
                    print(f"[PaddleOCR] 第 {idx} 条结果: '{extracted_text}'", file=sys.stderr)
                    text += extracted_text + " "
                elif isinstance(res, dict) and 'text' in res:
                    extracted_text = res['text']
                    print(f"[PaddleOCR] 第 {idx} 条结果: '{extracted_text}'", file=sys.stderr)
                    text += extracted_text + " "
                else:
                    print(f"[PaddleOCR] 第 {idx} 条结果类型: {type(res)}, 内容: {res}", file=sys.stderr)
        else:
            print(f"[PaddleOCR] 结果为空或None", file=sys.stderr)
        
        text = text.strip()
        print(f"[PaddleOCR] 最终识别结果: '{text}'", file=sys.stderr)
        
        # 保存结果
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(text)
        print(f"[PaddleOCR] 结果已保存到: {output_path}", file=sys.stderr)
        
        # 输出到标准输出
        print(text)
        return 0
        
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        import traceback
        traceback.print_exc(file=sys.stderr)
        return 1

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: recognize_simple.py <image_path> <output_path>", file=sys.stderr)
        sys.exit(1)
    
    image_path = sys.argv[1]
    output_path = sys.argv[2]
    
    sys.exit(recognize_text_simple(image_path, output_path))
