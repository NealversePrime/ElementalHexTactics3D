import os
import shutil
import subprocess

ffmpeg = r"C:\Users\Neal Sage\AppData\Local\Microsoft\WinGet\Links\ffmpeg.exe"
video = r"D:\Videos\2026-11-06 05-55-33.mp4"

# Copy font locally to avoid Windows drive letter colon issue in FFmpeg filter parser
local_font = "scratch/font.ttf"
shutil.copy("Assets/Fonts/Font_Bold.ttf", local_font)

outro_img = os.path.abspath("scratch/outro_card.png").replace("\\", "/")
temp_dir = os.path.abspath("scratch/video_edit_temp")
os.makedirs(temp_dir, exist_ok=True)

final_output = r"D:\Videos\ElementalHexTactics3D_LinkedIn_Showcase.mp4"

clips_def = [
    (46.0, 9.5, "TACTICAL HEX MOVEMENT & POSITIONING"),
    (82.0, 9.0, "DYNAMIC SKILLS & REAL-TIME INSPECTOR"),
    (131.0, 10.5, "KINETIC SHIELD SHOVE & COLLISION DAMAGE"),
    (282.0, 9.0, "PRIMORDIAL ALTAR - AWAKEN FLAME TITAN"),
    (308.0, 10.0, "CITADEL GATEWAY & RIGHT COMMAND DOCK"),
]

clip_files = []

for i, (start, dur, label) in enumerate(clips_def):
    out_clip = os.path.join(temp_dir, f"clip_{i}.mp4")
    clip_files.append(out_clip)
    
    # Filter: crop 1920x1080 from y=60, drawtext with built-in dark box, audio fade in/out
    vf = (
        f"crop=1920:1080:0:60,"
        f"drawtext=fontfile='scratch/font.ttf':text='{label}':x=50:y=45:fontsize=24:fontcolor=white:box=1:boxcolor=0x0b0d17@0.88:boxborderw=10"
    )
    af = f"afade=t=in:ss=0:d=0.3,afade=t=out:st={dur-0.3:.2f}:d=0.3"
    
    cmd = [
        ffmpeg, "-y",
        "-ss", str(start),
        "-t", str(dur),
        "-i", video,
        "-vf", vf,
        "-af", af,
        "-c:v", "libx264",
        "-preset", "veryfast",
        "-crf", "18",
        "-c:a", "aac",
        "-b:a", "192k",
        "-r", "60",
        out_clip
    ]
    print(f"Rendering clip {i+1}/{len(clips_def)}: {label}")
    subprocess.run(cmd, check=True)

# 6. Render Outro Card Clip (3.5 seconds)
outro_clip = os.path.join(temp_dir, "clip_outro.mp4")
clip_files.append(outro_clip)
print("Rendering Outro Card clip...")
cmd_outro = [
    ffmpeg, "-y",
    "-loop", "1",
    "-t", "3.5",
    "-i", outro_img,
    "-f", "lavfi",
    "-t", "3.5",
    "-i", "anullsrc=r=48000:cl=stereo",
    "-c:v", "libx264",
    "-preset", "veryfast",
    "-crf", "18",
    "-pix_fmt", "yuv420p",
    "-c:a", "aac",
    "-b:a", "192k",
    "-r", "60",
    outro_clip
]
subprocess.run(cmd_outro, check=True)

# 7. Concatenate all clips
concat_list = os.path.join(temp_dir, "concat.txt")
with open(concat_list, "w") as f:
    for c in clip_files:
        f.write(f"file '{c.replace(chr(92), '/')}'\n")

print("Concatenating clips into final showcase...")
cmd_concat = [
    ffmpeg, "-y",
    "-f", "concat",
    "-safe", "0",
    "-i", concat_list,
    "-c", "copy",
    final_output
]
subprocess.run(cmd_concat, check=True)

print("SUCCESS! Showcase video rendered to:", final_output)
file_size = os.path.getsize(final_output) / (1024 * 1024)
print(f"Final file size: {file_size:.2f} MB")
