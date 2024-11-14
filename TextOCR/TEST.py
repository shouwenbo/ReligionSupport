import cv2
from paddleocr import PaddleOCR

def process_video(video_path, subtitle_height, from_top=True, second=26):
    # 初始化 PaddleOCR
    ocr = PaddleOCR(use_angle_cls=True, lang='ch')

    # 打开视频文件
    cap = cv2.VideoCapture(video_path)
    
    # 获取视频分辨率宽度
    width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))

    # 获取帧率
    fps = int(cap.get(cv2.CAP_PROP_FPS))

    # 计算指定秒数对应的帧数
    frame_number = fps * second

    # 定位到指定秒数
    cap.set(cv2.CAP_PROP_POS_FRAMES, frame_number)

    # 读取指定帧
    ret, frame = cap.read()

    if ret:
        # 根据参数决定从上截取还是从下截取
        if from_top:
            subtitle_region = frame[0:subtitle_height, 0:width]  # 从上截取
        else:
            subtitle_region = frame[-subtitle_height:, 0:width]  # 从下截取

        # 使用 PaddleOCR 进行 OCR 识别
        result = ocr.ocr(subtitle_region, cls=True)

        # 检查识别结果并迭代
        if result:
            for line in result:
                if line:  # 确保 line 不是 None
                    for word_info in line:
                        print("识别出的字幕文本：", word_info[1][0])
        else:
            print("没有识别到任何字幕文本。")

        # 显示原图像和字幕区域
        cv2.imshow('Original Frame', frame)
        cv2.imshow('Subtitle Region', subtitle_region)

        # 按任意键继续
        cv2.waitKey(0)
        cv2.destroyAllWindows()
    else:
        print("无法读取视频帧。")

    cap.release()

# 调用函数并传入参数

'''
process_video(
    r'F:\food gyoyuk\sp\其他\印\sp\QSLYINYIN\1.mp4', 
    subtitle_height=53, 
    from_top=True, 
    second=26  # 指定秒数
)
'''


process_video(
    r'F:\food gyoyuk\sp\全圣徒圣经教育（受印确认考试）\全圣徒受印确认教育 41年第21次受印确认考试.mp4', 
    subtitle_height=100, 
    from_top=False, 
    second=1900  # 指定秒数
)
