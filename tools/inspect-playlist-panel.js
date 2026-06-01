const fs = require("fs");

function extractInitialData(file) {
  const source = fs.readFileSync(file, "utf8");
  const markers = ["var ytInitialData = ", "ytInitialData = ", "a.ytInitialData = "];

  for (const marker of markers) {
    const start = source.indexOf(marker);
    if (start < 0) continue;

    const jsonStart = start + marker.length;
    let depth = 0;
    let inString = false;
    let escaped = false;

    for (let index = jsonStart; index < source.length; index += 1) {
      const char = source[index];

      if (inString) {
        if (escaped) escaped = false;
        else if (char === "\\") escaped = true;
        else if (char === "\"") inString = false;
        continue;
      }

      if (char === "\"") inString = true;
      else if (char === "{") depth += 1;
      else if (char === "}") {
        depth -= 1;
        if (depth === 0) return JSON.parse(source.slice(jsonStart, index + 1));
      }
    }
  }

  throw new Error(`Cannot find ytInitialData in ${file}`);
}

function textOf(value) {
  if (!value) return "";
  if (typeof value === "string") return value;
  if (value.simpleText) return value.simpleText;
  if (value.content) return value.content;
  if (value.runs) return value.runs.map((run) => run.text || "").join("");
  return "";
}

function collectPlaylistPanelVideos(value, path = [], videos = []) {
  if (!value || typeof value !== "object") return videos;

  if (value.playlistPanelVideoRenderer) {
    const item = value.playlistPanelVideoRenderer;
    videos.push({
      path: path.join("."),
      videoId: item.videoId,
      title: textOf(item.title),
      duration: textOf(item.lengthText),
    });
  }

  if (Array.isArray(value)) {
    value.forEach((item, index) => collectPlaylistPanelVideos(item, path.concat(index), videos));
  } else {
    Object.entries(value).forEach(([key, item]) => collectPlaylistPanelVideos(item, path.concat(key), videos));
  }

  return videos;
}

for (const file of ["tools/yt_java-watch.html", "tools/yt_react-watch.html"]) {
  const videos = collectPlaylistPanelVideos(extractInitialData(file));
  console.log(`${file}: ${videos.length}`);
  for (const [index, video] of videos.slice(0, 40).entries()) {
    console.log(`${index + 1}. ${video.videoId} | ${video.duration} | ${video.title}`);
    console.log(`   ${video.path}`);
  }
}
