import os
import cv2
from paddleocr import PaddleOCR
import re
import logging

# 禁用 PaddleOCR 的 Debug 输出
logging.getLogger("paddleocr").setLevel(logging.FATAL)

logging.disable(logging.DEBUG)
logging.disable(logging.WARNING)

def extract_chinese_characters(text):
    """从文本中提取所有的中文字符"""
    return ''.join(re.findall(r'[\u4e00-\u9fa5]', text))

def extract_subtitles(video_path, subtitle_height=53, frame_interval=1, from_top=True, output_file="output_subtitles.txt", time_range=None):
    # 初始化 PaddleOCR
    ocr = PaddleOCR(use_angle_cls=False, lang='ch')

    # 打开视频文件
    cap = cv2.VideoCapture(video_path)

    # 获取视频分辨率宽度和高度
    width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))

    # 获取帧率
    frame_rate = int(cap.get(cv2.CAP_PROP_FPS))  # 获取帧率
    total_frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))  # 获取总帧数
    frame_count = 0

    # 计算视频的开始帧和结束帧
    start_frame = 0
    end_frame = total_frames

    if time_range:
        # 解析时间范围参数
        start_time, end_time = time_range.split('-')
        start_minutes, start_seconds = map(int, start_time.split(':'))
        end_minutes, end_seconds = map(int, end_time.split(':'))

        # 计算起始帧和结束帧
        start_frame = (start_minutes * 60 + start_seconds) * frame_rate
        end_frame = (end_minutes * 60 + end_seconds) * frame_rate
        end_frame = min(end_frame, total_frames)  # 确保结束帧不超过总帧数
        total_frames = end_frame - start_frame  # 更新 total_frames 以适应指定的时间范围

    output_text = []
    last_text = ""  # 用于跟踪上一个提取的文本

    # 计算每frame_interval秒对应的帧数
    frames_per_interval = frame_rate * frame_interval

    # 移动到开始帧
    cap.set(cv2.CAP_PROP_POS_FRAMES, start_frame)

    # 记录总处理的帧数
    total_processed = 0

    while cap.isOpened() and frame_count < end_frame - start_frame:
        ret, frame = cap.read()
        if not ret:
            break

        # 每frame_interval秒提取一帧
        if frame_count % frames_per_interval == 0:
            # 根据 from_top 参数确定字幕区域
            if from_top:
                subtitle_region = frame[0:subtitle_height, 0:width]
            else:
                subtitle_region = frame[height - subtitle_height:height, 0:width]

            # 使用 PaddleOCR 进行 OCR 识别
            result = ocr.ocr(subtitle_region, cls=True)

            # 检查 result 是否有效
            if result:  # 确保结果不是 None 或空
                # 处理 OCR 结果并提取文本
                text = ""
                for line in result:
                    if line:  # 确保 line 不为 None
                        for word_info in line:
                            extracted_text = word_info[1][0]  # 获取文本内容
                            
                            # 检查提取的文本是否包含中文字符
                            if contains_chinese(extracted_text):
                                text += extracted_text  # 只在包含中文时才添加到文本中

                current_text = text.strip()  # 获取当前文本并去除空格
                
                # 提取汉字部分进行比较
                current_text_chinese = extract_chinese_characters(current_text)
                last_text_chinese = extract_chinese_characters(last_text)

                # 检查当前提取的文本是否与上一个提取的文本相同
                if current_text_chinese != last_text_chinese:  # 只有当文本不同时才添加
                    output_text.append(current_text)
                    last_text = current_text  # 更新上一个文本

        # 更新总处理帧数
        total_processed += 1

        # 显示进度每10秒一次
        if total_processed % (frame_rate * 10) == 0:
            print(f"处理帧: {total_processed}/{total_frames} ({(total_processed / total_frames) * 100:.2f}%)")

        frame_count += 1

    cap.release()

    # 将提取的字幕文本保存到文件
    with open(output_file, "w", encoding="utf-8") as file:
        # 对每个提取的文本进行处理，确保结尾不带标点的句子后面加上逗号
        processed_text = []
        for i, text in enumerate(output_text):
            # 检查当前文本是否为空
            if text.strip():  # 只处理非空文本
                # 如果不是最后一个文本并且当前文本没有以标点符号结尾
                if i < len(output_text) - 1 and not re.search(r'[。！？、，；：“”‘’《》【】()（）{}{}<>‘’“”.,!?;:\'\"-]', text):
                    processed_text.append(text + "，")  # 在后面加上逗号
                else:
                    processed_text.append(text)  # 直接添加文本

        file.write("".join(processed_text))  # 使用空字符串连接处理后的文本

    print("文字提取完成！")

def contains_chinese(text):
    """检查文本是否包含中文字符"""
    return bool(re.search(r'[\u4e00-\u9fa5]', text))

# 调用函数并传入参数
'''
extract_subtitles(
    video_path=r'F:\food gyoyuk\sp\其他\印\sp\QSLYINYIN\1.mp4',
    subtitle_height=53,  # 字幕区域的高度
    frame_interval=1,  # 每秒提取一帧
    from_top=True,  # 是否从顶部截取字幕区域
    output_file="output_subtitles.txt",  # 输出文件名
    time_range=None  # 提取的时间范围（可选） 比如"16:23-17:23"
)
'''
'''
extract_subtitles(
    r'F:\food gyoyuk\sp\全圣徒圣经教育（受印确认考试）\全圣徒受印确认教育 41年第23次受印确认考试.mp4', 
    subtitle_height=120,  # 字幕区域的高度
    frame_interval=1,  # 每秒提取一帧
    from_top=False,  # 是否从顶部截取字幕区域
    output_file="output_subtitles.txt",  # 输出文件名
    time_range=None  # 提取的时间范围（可选） 比如"16:23-17:23"
)
'''
'''
extract_subtitles(
    r'F:\粮食\\36年启示录印印教育\03 启1章上.mp4', 
    subtitle_height=56,  # 字幕区域的高度
    frame_interval=0.5,  # 每秒提取一帧
    from_top=True,  # 是否从顶部截取字幕区域
    output_file="output_subtitles.txt",  # 输出文件名
    time_range=None  # 提取的时间范围（可选） 比如"16:23-17:23"
)
'''
# extract_subtitles(
#     r'F:\粮食\粮食 & 教育\sp\其他\420122_12枝派老年会特别教育.mp4', 
#     subtitle_height=65,
#     frame_interval=0.75,  # 每秒提取一帧
#     from_top=False,  # 是否从顶部截取字幕区域
#     output_file="output_subtitles.txt",  # 输出文件名
#     time_range=None  # 提取的时间范围（可选） 比如"16:23-17:23"
# )
extract_subtitles(
    r'F:\粮食\12枝派青年会每月定期教育\新42年12枝派青年会月定期教育（3次） 下.mp4', 
    subtitle_height=86,
    frame_interval=0.45,  # 每秒提取一帧0.75
    from_top=False,  # 是否从顶部截取字幕区域
    output_file="output_subtitles.txt",  # 输出文件名
    time_range=None  # 提取的时间范围（可选） 比如"16:23-17:23"
)