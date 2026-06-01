import json
from pathlib import Path
import re
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

sys.path.insert(0, str(Path(__file__).parent / "vendor"))

import yt_dlp


PLAYLISTS = {
    "javascript": "https://www.youtube.com/watch?v=0SJE9dYdpps&list=PL_-VfJajZj0VgpFpEVFzS5Z-lkXtBe-x5",
    "java": "https://www.youtube.com/playlist?list=PLPt6-BtUI22rxpe6PZc5H6XAgPusA6fDQ",
    "react": "https://www.youtube.com/watch?v=NclbvXqvnyA&list=PLPt6-BtUI22oD3xfWy9Vl9kINNxqAnTjb",
    "dotnet": "https://www.youtube.com/watch?v=-GHF0aAvKEI&list=PLRLJQuuRRcFIalTD5F6XKOJxOt8QgCNAg",
    "nextjs": "https://www.youtube.com/playlist?list=PLFfVmM19UNqn1ZIWvxn1artfz-C6dgAFb",
    "python": "https://www.youtube.com/playlist?list=PL33lvabfss1xczCv2BA0SaNJHu_VXsFtg",
    "cpp": "https://www.youtube.com/playlist?list=PL33lvabfss1xagFyyQPRcppjFKMQ7IvJM",
    "typescript": "https://www.youtube.com/playlist?list=PLncHg6Kn2JT5emvXmG6kgeGkrQjRqxsb4",
}

FALLBACK_SEARCHES = {
    "react": [
        "ytsearch100:React js Gà Lại Lập Trình",
        "ytsearch100:React JS từ cơ bản đến nâng cao Gà Lại Lập Trình",
        "ytsearch100:React js tuhoc.cc Gà Lại Lập Trình",
        "ytsearch100:React js 14 Gà Lại Lập Trình",
        "ytsearch100:React js 18 Gà Lại Lập Trình",
        "ytsearch100:React js 26 Gà Lại Lập Trình",
    ],
}

FALLBACK_SEARCHES["dotnet"] = [
    "ytsearch150:Học lập trình .NET phần Học lập trình cùng Nam",
    "ytsearch150:Learn .NET programming part Học lập trình cùng Nam",
    "ytsearch150:Học lập trình .NET nền tảng Học lập trình cùng Nam",
]
FALLBACK_SEARCHES["cpp"] = [
    "ytsearch150:Khóa học lập trình C++ Cơ bản HowKteam",
    "ytsearch150:Lập trình C++ cơ bản HowKteam",
]


def duration_text(seconds):
    if not seconds:
        return ""
    seconds = int(seconds)
    minutes, second = divmod(seconds, 60)
    hours, minute = divmod(minutes, 60)
    if hours:
        return f"{hours}:{minute:02d}:{second:02d}"
    return f"{minute}:{second:02d}"


def normalize_entry(entry, index):
    video_id = entry.get("id")
    title = entry.get("title") or f"Video {index}"
    return {
        "index": index,
        "title": title,
        "videoId": video_id,
        "url": f"https://www.youtube.com/watch?v={video_id}",
        "duration": duration_text(entry.get("duration")),
    }


def lesson_number(title):
    match = re.search(r"(?:phần|phan|part|bài|bai|#)\s*(\d+(?:\.\d+)?)", title, re.IGNORECASE)
    if match:
        return float(match.group(1))

    prefix = title.split(" ", 1)[0].strip(".")
    try:
        return float(prefix)
    except ValueError:
        return 9999


def is_react_course_video(entry):
    title = (entry.get("title") or "").lower()
    channel_id = entry.get("channel_id")
    channel = (entry.get("channel") or entry.get("uploader") or "").lower()
    if channel_id != "UC1ngP5TuY-4ZMaf88jfi-kQ" and "gà lại lập trình" not in channel:
        return False
    return "react" in title and lesson_number(title) < 1000


def is_dotnet_course_video(entry):
    title = (entry.get("title") or "").lower()
    channel = (entry.get("channel") or entry.get("uploader") or "").lower()
    is_course_title = "học lập trình .net" in title or "learn .net programming" in title
    return is_course_title and "học lập trình cùng nam" in channel


def is_cpp_course_video(entry):
    title = (entry.get("title") or "").lower()
    channel = (entry.get("channel") or entry.get("uploader") or "").lower()
    return "khóa học lập trình c++ cơ bản" in title and ("howkteam" in title or "k team" in channel)


def is_expected_fallback_video(key, entry):
    if key == "react":
        return is_react_course_video(entry)
    if key == "dotnet":
        return is_dotnet_course_video(entry)
    if key == "cpp":
        return is_cpp_course_video(entry)
    return True


def unique_entries(entries):
    seen = set()
    result = []
    for entry in entries:
        video_id = entry.get("id")
        if not video_id or video_id in seen:
            continue
        seen.add(video_id)
        result.append(entry)
    return result


def main():
    result = {}
    options = {
        "extract_flat": "in_playlist",
        "quiet": True,
        "skip_download": True,
        "ignoreerrors": True,
        "noplaylist": False,
    }

    with yt_dlp.YoutubeDL(options) as ydl:
        for key, url in PLAYLISTS.items():
            info = ydl.extract_info(url, download=False)
            if not info:
                info = {"id": "", "title": key, "entries": []}

            entries = [entry for entry in info.get("entries", []) if entry and entry.get("id")]
            if not entries and key in FALLBACK_SEARCHES:
                entries = []
                for query in FALLBACK_SEARCHES[key]:
                    search_info = ydl.extract_info(query, download=False)
                    entries.extend(
                        entry
                        for entry in search_info.get("entries", [])
                        if entry and entry.get("id") and is_expected_fallback_video(key, entry)
                    )
                entries = unique_entries(entries)
                entries.sort(key=lambda entry: (lesson_number(entry.get("title") or ""), entry.get("title") or ""))

            videos = [normalize_entry(entry, index + 1) for index, entry in enumerate(entries)]
            result[key] = {
                "listId": info.get("id"),
                "title": info.get("title"),
                "count": len(videos),
                "videos": videos,
            }
            print(f"{key}: {len(videos)} videos")
            for video in videos[:8]:
                print(f"  {video['index']}. {video['title']} ({video['videoId']})")

    output = Path(__file__).parent / "youtube-playlists.json"
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
