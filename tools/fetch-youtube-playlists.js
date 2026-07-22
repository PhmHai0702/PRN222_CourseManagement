const fs = require("fs");
const path = require("path");

const apiKey = "AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";
const clientVersion = "2.20260715.04.00";

const playlists = [
  {
    key: "javascript",
    videoId: "0SJE9dYdpps",
    listId: "PL_-VfJajZj0VgpFpEVFzS5Z-lkXtBe-x5",
  },
  {
    key: "java",
    videoId: "9tQ-GGE010s",
    listId: "PLPt6-BtUI22pxpe6PZc5H6XAgPrusA6fDQ",
  },
  {
    key: "react",
    videoId: "NclbvXqvnyA",
    listId: "PLPt6-BtUI22oD3xfWy9Vl9kINNxqAnTjb",
  },
];

function context() {
  return {
    client: {
      hl: "vi",
      gl: "VN",
      clientName: "WEB",
      clientVersion,
    },
  };
}

async function postJson(url, body) {
  const response = await fetch(url, {
    method: "POST",
    headers: {
      "content-type": "application/json",
      "x-youtube-client-name": "1",
      "x-youtube-client-version": clientVersion,
    },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`);
  }

  return response.json();
}

function textOf(value) {
  if (!value) return "";
  if (typeof value === "string") return value;
  if (value.simpleText) return value.simpleText;
  if (value.content) return value.content;
  if (value.runs) return value.runs.map((run) => run.text || "").join("");
  return "";
}

function collectPanelVideos(value, videos = []) {
  if (!value || typeof value !== "object") return videos;

  if (value.playlistPanelVideoRenderer) {
    const item = value.playlistPanelVideoRenderer;
    const title = textOf(item.title);
    const videoId = item.videoId;
    const duration = textOf(item.lengthText);
    if (videoId && title) {
      videos.push({
        index: videos.length + 1,
        title,
        videoId,
        url: `https://www.youtube.com/watch?v=${videoId}`,
        duration,
      });
    }
  }

  if (Array.isArray(value)) {
    for (const item of value) collectPanelVideos(item, videos);
    return videos;
  }

  for (const item of Object.values(value)) collectPanelVideos(item, videos);
  return videos;
}

function findContinuation(value) {
  if (!value || typeof value !== "object") return "";

  const continuation =
    value.continuationCommand?.token ||
    value.nextContinuationData?.continuation ||
    value.reloadContinuationData?.continuation;

  if (continuation) return continuation;

  if (Array.isArray(value)) {
    for (const item of value) {
      const token = findContinuation(item);
      if (token) return token;
    }
    return "";
  }

  for (const item of Object.values(value)) {
    const token = findContinuation(item);
    if (token) return token;
  }

  return "";
}

async function fetchPlaylist(playlist) {
  const nextUrl = `https://www.youtube.com/youtubei/v1/next?key=${apiKey}`;
  const data = await postJson(nextUrl, {
    context: context(),
    videoId: playlist.videoId,
    playlistId: playlist.listId,
    params: "OAE%3D",
  });

  const videos = collectPanelVideos(data)
    .filter((video, index, arr) => arr.findIndex((item) => item.videoId === video.videoId) === index);

  return {
    listId: playlist.listId,
    count: videos.length,
    videos: videos.map((video, index) => ({ ...video, index: index + 1 })),
  };
}

(async () => {
  const result = {};
  for (const playlist of playlists) {
    result[playlist.key] = await fetchPlaylist(playlist);
  }

  fs.writeFileSync(
    path.join(__dirname, "youtube-playlists.json"),
    `${JSON.stringify(result, null, 2)}\n`,
    "utf8"
  );

  for (const [key, playlist] of Object.entries(result)) {
    console.log(`${key}: ${playlist.count} videos`);
    for (const video of playlist.videos.slice(0, 8)) {
      console.log(`  ${video.index}. ${video.title} (${video.videoId})`);
    }
  }
})().catch((error) => {
  console.error(error);
  process.exit(1);
});
