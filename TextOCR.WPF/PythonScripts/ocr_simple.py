# -*- coding: utf-8 -*-
import sys
import io

# 强制设置标准输出和错误输出为UTF-8编码
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

from paddleocr import PaddleOCR

try:
    # 初始化PaddleOCR - 使用3.3.1官方推荐参数
    ocr = PaddleOCR(
        use_doc_orientation_classify=False,
        use_doc_unwarping=False,
        use_textline_orientation=False
    )

    # 获取图片路径
    img_path = sys.argv[1]

    # 执行OCR识别 - 使用predict方法
    result = ocr.predict(img_path)
    
    # 提取文本 - 根据官方文档，result是一个列表，每个元素是Result对象
    texts = []
    if result:
        for page_res in result:
            # page_res.rec_texts 是识别出的文本列表
            if hasattr(page_res, 'rec_texts') and page_res.rec_texts:
                texts.extend(page_res.rec_texts)
            # 如果result是字典格式（兼容性处理）
            elif isinstance(page_res, dict) and 'rec_texts' in page_res:
                texts.extend(page_res['rec_texts'])
    
    # 输出拼接后的文本
    output = ''.join(texts)
    print(output)
    
except Exception as e:
    # 输出错误信息到stderr
    print(f"错误: {str(e)}", file=sys.stderr)
    import traceback
    traceback.print_exc(file=sys.stderr)
    sys.exit(1)
