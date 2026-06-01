const fs = require("fs");

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
    if (depth === 0) return JSON.parse(html.slice(jsonStart, index + 1));
  }
  throw new Error("JSON did not terminate");
}

function findLocks(value, path = [], items = []) {
  if (!value || typeof value !== "object") return items;
  if (value.lockupViewModel) {
    const item = value.lockupViewModel;
    const title = item.metadata?.lockupMetadataViewModel?.title?.content || "";
    if (title) items.push({ title, path: path.join(".") });
  }
  if (Array.isArray(value)) {
    value.forEach((item, index) => findLocks(item, [...path, `[${index}]`], items));
    return items;
  }
  for (const [key, child] of Object.entries(value)) {
    findLocks(child, [...path, key], items);
  }
  return items;
}

for (const file of ["yt_java-watch.html", "yt_react-watch.html"]) {
  const data = extractInitialData(fs.readFileSync(`tools/${file}`, "utf8"));
  console.log(file);
  for (const item of findLocks(data).slice(0, 40)) {
    console.log("-", item.title);
    console.log("  ", item.path);
  }
}
