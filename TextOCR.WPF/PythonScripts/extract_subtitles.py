# -*- coding: utf-8 -*-
import sys
import io
import os
import argparse

# 强制设置标准输出和错误输出为UTF-8编码
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

# 必须在导入 PaddleOCR 之前设置环境变量，避免中文用户名问题
os.environ['PADDLEX_HOME'] = 'C:/PaddleOCR_Models'
os.environ['HOME'] = 'C:/PaddleOCR_Models'
os.environ['USERPROFILE'] = 'C:/PaddleOCR_Models'
os.environ['PADDLE_HOME'] = 'C:/PaddleOCR_Models'

import cv2
from paddleocr import PaddleOCR
import re

def extract_chinese_characters(text):
    """从文本中提取所有的中文字符"""
    return ''.join(re.findall(r'[\u4e00-\u9fa5]', text))

def contains_chinese(text):
    """检查文本是否包含中文字符"""
    return bool(re.search(r'[\u4e00-\u9fa5]', text))

def extract_subtitles(video_path, subtitle_height, frame_interval, from_top, output_file, time_range=None):
    """从视频中提取字幕"""
    try:
        # 初始化 PaddleOCR - 与 ocr_simple.py 一致（3.3.1 官方推荐参数）
        print("正在初始化 PaddleOCR...", flush=True)
        ocr = PaddleOCR(
            use_doc_orientation_classify=False,
            use_doc_unwarping=False,
            use_textline_orientation=False
        )
        print("PaddleOCR 初始化完成", flush=True)

        # 打开视频文件
        cap = cv2.VideoCapture(video_path)
        if not cap.isOpened():
            raise Exception(f"无法打开视频文件: {video_path}")

        # 获取视频参数
        width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        frame_rate = cap.get(cv2.CAP_PROP_FPS)
        total_frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
        
        print(f"视频信息: {width}x{height}, {frame_rate:.2f}fps, 总帧数: {total_frames}", flush=True)

        # 计算时间范围
        start_frame = 0
        end_frame = total_frames

        if time_range:
            start_time, end_time = time_range.split('-')
            start_minutes, start_seconds = map(int, start_time.split(':'))
            end_minutes, end_seconds = map(int, end_time.split(':'))

            start_frame = int((start_minutes * 60 + start_seconds) * frame_rate)
            end_frame = int((end_minutes * 60 + end_seconds) * frame_rate)
            end_frame = min(end_frame, total_frames)

        # 计算需要处理的帧
        frames_per_interval = max(1, int(frame_rate * frame_interval))
        frames_to_process = list(range(start_frame, end_frame, frames_per_interval))
        total_frames_to_process = len(frames_to_process)
        
        print(f"帧间隔: {frame_interval}秒 ({frames_per_interval}帧)", flush=True)
        print(f"需要识别的帧数: {total_frames_to_process}", flush=True)
        print("=" * 50, flush=True)

        output_text = []
        last_text = ""
        processed_count = 0

        # 移动到开始帧
        cap.set(cv2.CAP_PROP_POS_FRAMES, start_frame)

        for frame_idx in frames_to_process:
            # 设置帧位置
            cap.set(cv2.CAP_PROP_POS_FRAMES, frame_idx)
            ret, frame = cap.read()
            
            if not ret:
                print(f"警告: 无法读取第 {frame_idx} 帧", file=sys.stderr, flush=True)
                continue

            # 提取字幕区域（不保存临时文件，直接在内存中处理）
            if from_top:
                subtitle_region = frame[0:subtitle_height, 0:width]
            else:
                subtitle_region = frame[height - subtitle_height:height, 0:width]

            # OCR 识别 - 与 ocr_simple.py 一致（使用 predict 方法）
            try:
                result = ocr.predict(subtitle_region)
                
                # 提取文本 - 与 ocr_simple.py 相同的解析逻辑
                texts = []
                if result:
                    for page_res in result:
                        if hasattr(page_res, 'rec_texts') and page_res.rec_texts:
                            texts.extend(page_res.rec_texts)
                        elif isinstance(page_res, dict) and 'rec_texts' in page_res:
                            texts.extend(page_res['rec_texts'])
                
                # 业务逻辑 - 与 TextOCR.py 一致（只保留中文字符）
                current_text = ""
                for text in texts:
                    if contains_chinese(text):
                        current_text += text
                
                current_text = current_text.strip()
                current_text_chinese = extract_chinese_characters(current_text)
                last_text_chinese = extract_chinese_characters(last_text)

                # 去重逻辑 - 与 TextOCR.py 一致（只有当文本不同时才添加）
                if current_text_chinese != last_text_chinese:
                    output_text.append(current_text)
                    last_text = current_text
                    
            except Exception as ocr_error:
                print(f"帧 {frame_idx} OCR 识别失败: {ocr_error}", file=sys.stderr, flush=True)

            # 更新进度
            processed_count += 1
            progress = (processed_count / total_frames_to_process) * 100
            
            # 每处理一帧都输出进度（实时更新）
            print(f"处理帧: {processed_count}/{total_frames_to_process} ({progress:.1f}%) - 已识别字幕: {len(output_text)} 条", flush=True)

        cap.release()
        
        print("=" * 50, flush=True)
        print(f"识别完成！共识别出 {len(output_text)} 条字幕", flush=True)

        # 保存结果
        with open(output_file, "w", encoding="utf-8") as file:
            processed_text = []
            for i, text in enumerate(output_text):
                if text.strip():
                    # 如果不是最后一条且没有标点符号，添加逗号
                    if i < len(output_text) - 1 and not re.search(r'[。！？、，；：""''《》【】()（）{}{}<>''"".,!?;:\'\"-]', text):
                        processed_text.append(text + "，")
                    else:
                        processed_text.append(text)

            file.write("".join(processed_text))

        print(f"字幕已保存到: {output_file}", flush=True)
        return 0
        
    except Exception as e:
        print(f"错误: {str(e)}", file=sys.stderr, flush=True)
        import traceback
        traceback.print_exc(file=sys.stderr)
        return 1

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='从视频中提取字幕')
    parser.add_argument('video_path', help='视频文件路径')
    parser.add_argument('--subtitle_height', type=int, default=80, help='字幕区域高度')
    parser.add_argument('--frame_interval', type=float, default=0.5, help='帧间隔（秒）')
    parser.add_argument('--from_top', type=str, default='False', help='从顶部截取')
    parser.add_argument('--output_file', default='output_subtitles.txt', help='输出文件路径')
    parser.add_argument('--time_range', default=None, help='时间范围，格式: MM:SS-MM:SS')

    args = parser.parse_args()
    
    from_top_bool = args.from_top.lower() == 'true'
    
    try:
        exit_code = extract_subtitles(
            args.video_path,
            args.subtitle_height,
            args.frame_interval,
            from_top_bool,
            args.output_file,
            args.time_range
        )
        sys.exit(exit_code)
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)
