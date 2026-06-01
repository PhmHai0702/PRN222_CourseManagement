const fs = require("fs");
const path = require("path");

const playlists = [
  {
    key: "javascript",
    file: "yt_PL_-VfJajZj0VgpFpEVFzS5Z-lkXtBe-x5.html",
    listId: "PL_-VfJajZj0VgpFpEVFzS5Z-lkXtBe-x5",
  },
  {
    key: "java",
    file: "yt_java-watch.html",
    listId: "PLPt6-BtUI22pxpe6PZc5H6XAgPrusA6fDQ",
  },
  {
    key: "react",
    file: "yt_react-watch.html",
    listId: "PLPt6-BtUI22oD3xfWy9Vl9kINNxqAnTjb",
  },
];

function extractInitialData(html) {
  const markers = ["var ytInitialData = ", "ytInitialData = ", "a.ytInitialData = "];
  const found = markers
    .map((marker) => ({ marker, start: html.indexOf(marker) }))
    .filter((item) => item.start !== -1)
    .sort((left, right) => left.start - right.start)[0];

  if (!found) throw new Error("ytInitialData marker not found");

  let index = found.start + found.marker.length;
  let depth = 0;
  let inString = false;
  let escape = false;
  let jsonStart = -1;

  for (; index < html.length; index++) {
    const char = html[index];
    if (jsonStart === -1) {
      if (char === "{") {
        jsonStart = index;
        depth = 1;
      }
      continue;
    }

    if (escape) {
      escape = false;
      continue;
    }

    if (char === "\\") {
      escape = true;
      continue;
    }

    if (char === '"') {
      inString = !inString;
      continue;
    }

    if (inString) continue;

    if (char === "{") depth++;
    if (char === "}") depth--;

    if (depth === 0) {
      return JSON.parse(html.slice(jsonStart, index + 1));
    }
  }

  throw new Error("ytInitialData JSON did not terminate");
}

function findPlaylistVideos(value, listId, videos = []) {
  if (!value || typeof value !== "object") return videos;

  if (value.playlistVideoRenderer) {
    const item = value.playlistVideoRenderer;
    const title =
      item.title?.runs?.map((run) => run.text).join("") ||
      item.title?.simpleText ||
      "";
    const videoId = item.videoId;
    const indexText =
      item.index?.simpleText ||
      item.index?.runs?.map((run) => run.text).join("") ||
      "";
    const duration =
      item.lengthText?.simpleText ||
      item.thumbnailOverlays
        ?.map((overlay) => overlay.thumbnailOverlayTimeStatusRenderer?.text?.simpleText)
        .find(Boolean) ||
      "";

    if (videoId && title) {
      videos.push({
        index: Number.parseInt(indexText, 10) || videos.length + 1,
        title,
        videoId,
        url: `https://www.youtube.com/watch?v=${videoId}`,
        duration,
      });
    }
  }

  if (value.lockupViewModel) {
    const item = value.lockupViewModel;
    const playlistId = findPlaylistId(item);
    const title = item.metadata?.lockupMetadataViewModel?.title?.content || "";
    const thumbnailUrl = item.contentImage?.thumbnailViewModel?.image?.sources?.[0]?.url || "";
    const videoId =
      findVideoId(item) ||
      thumbnailUrl.match(/\/vi\/([^/]+)\//)?.[1] ||
      thumbnailUrl.match(/vi_webp\/([^/]+)\//)?.[1] ||
      "";
    const duration =
      item.contentImage?.thumbnailViewModel?.overlays
        ?.flatMap((overlay) => overlay.thumbnailBottomOverlayViewModel?.badges || [])
        .map((badge) => badge.thumbnailBadgeViewModel?.text)
        .find(Boolean) || "";

    if (videoId && title && (!playlistId || playlistId === listId)) {
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
    for (const item of value) findPlaylistVideos(item, listId, videos);
    return videos;
  }

  for (const child of Object.values(value)) findPlaylistVideos(child, listId, videos);
  return videos;
}

function findPlaylistId(value) {
  if (!value || typeof value !== "object") return "";
  if (value.playlistId && typeof value.playlistId === "string") return value.playlistId;
  if (Array.isArray(value)) {
    for (const item of value) {
      const playlistId = findPlaylistId(item);
      if (playlistId) return playlistId;
    }
    return "";
  }
  for (const child of Object.values(value)) {
    const playlistId = findPlaylistId(child);
    if (playlistId) return playlistId;
  }
  return "";
}

function findVideoId(value) {
  if (!value || typeof value !== "object") return "";
  if (value.videoId && typeof value.videoId === "string") return value.videoId;
  if (Array.isArray(value)) {
    for (const item of value) {
      const videoId = findVideoId(item);
      if (videoId) return videoId;
    }
    return "";
  }
  for (const child of Object.values(value)) {
    const videoId = findVideoId(child);
    if (videoId) return videoId;
  }
  return "";
}

const root = __dirname;
const result = {};

for (const playlist of playlists) {
  const html = fs.readFileSync(path.join(root, playlist.file), "utf8");
  const data = extractInitialData(html);
  const videos = findPlaylistVideos(data, playlist.listId)
    .filter((video, index, arr) => arr.findIndex((item) => item.videoId === video.videoId) === index)
    .sort((left, right) => left.index - right.index);

  result[playlist.key] = {
    listId: playlist.listId,
    count: videos.length,
    videos,
  };
}

fs.writeFileSync(
  path.join(root, "youtube-playlists.json"),
  `${JSON.stringify(result, null, 2)}\n`,
  "utf8"
);

for (const [key, playlist] of Object.entries(result)) {
  console.log(`${key}: ${playlist.count} videos`);
  for (const video of playlist.videos.slice(0, 5)) {
    console.log(`  ${video.index}. ${video.title} (${video.videoId})`);
  }
}
