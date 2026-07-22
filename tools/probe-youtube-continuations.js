const fs = require("fs");

const apiKey = "AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";
const clientVersion = "2.20260715.04.00";

const files = ["yt_java-watch.html", "yt_react-watch.html"];

function decodeEscapes(text) {
  return text.replace(/\\u0026/g, "&");
}

function collectTokens(html) {
  const tokens = new Set();
  const regex = /"token":"([^"]+)"/g;
  let match;
  while ((match = regex.exec(html))) {
    tokens.add(decodeEscapes(match[1]));
  }
  return [...tokens];
}

function textOf(value) {
  if (!value) return "";
  if (value.simpleText) return value.simpleText;
  if (value.content) return value.content;
  if (value.runs) return value.runs.map((run) => run.text || "").join("");
  return "";
}

function collectPanelVideos(value, videos = []) {
  if (!value || typeof value !== "object") return videos;
  if (value.playlistPanelVideoRenderer) {
    const item = value.playlistPanelVideoRenderer;
    videos.push({
      title: textOf(item.title),
      videoId: item.videoId,
    });
  }
  if (Array.isArray(value)) {
    for (const item of value) collectPanelVideos(item, videos);
    return videos;
  }
  for (const item of Object.values(value)) collectPanelVideos(item, videos);
  return videos;
}

async function postContinuation(endpoint, token) {
  const response = await fetch(`https://www.youtube.com/youtubei/v1/${endpoint}?key=${apiKey}`, {
    method: "POST",
    headers: {
      "content-type": "application/json",
      "x-youtube-client-name": "1",
      "x-youtube-client-version": clientVersion,
    },
    body: JSON.stringify({
      context: {
        client: {
          hl: "vi",
          gl: "VN",
          clientName: "WEB",
          clientVersion,
        },
      },
      continuation: token,
    }),
  });

  if (!response.ok) return [];
  const data = await response.json();
  return collectPanelVideos(data);
}

(async () => {
  for (const file of files) {
    const html = fs.readFileSync(`tools/${file}`, "utf8");
    const tokens = collectTokens(html).filter((token) => token.startsWith("CBQ"));
    console.log(file, tokens.length, "candidate tokens");
    for (const token of tokens.slice(0, 10)) {
      let videos = await postContinuation("next", token);
      if (!videos.length) videos = await postContinuation("browse", token);
      if (videos.length) {
        console.log("TOKEN", token.slice(0, 60), "videos", videos.length);
        console.log(videos.slice(0, 5));
      }
    }
  }
})().catch((error) => {
  console.error(error);
  process.exit(1);
});
