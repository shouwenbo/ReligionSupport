# test_load_assets.py
from modules.load_assets import load_assets
from modules.validate_audio import validate_audio_lengths

assets = load_assets("assets")

print("素材加载成功！")
print(f"标题：{assets['title_text']}")
print(f"字幕行数：{len(assets['subtitles'])}")
print(f"背景视频时长：{assets['bg_video'].duration:.2f} 秒")
print("所有素材加载完毕！")

validate_audio_lengths(assets["voice_audio"], assets["music_audio"])
print("所有素材校验通过，准备进入下一步...")
