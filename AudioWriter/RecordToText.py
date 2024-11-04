import speech_recognition as sr

# 创建一个识别器对象
recognizer = sr.Recognizer()

# 用语音文件创建一个音频文件对象
audio_file = r"F:\food gyoyuk\sp\总会特别教育\214.wav"
with sr.AudioFile(audio_file) as source:
    audio = recognizer.record(source)  # 读取音频文件

try:
    # 使用Google Web Speech API进行语音识别
    text = recognizer.recognize_google(audio, language='zh-CN')
    print("识别结果：" + text)
except sr.UnknownValueError:
    print("抱歉，无法识别")
except sr.RequestError:
    print("抱歉，无法连接到API")