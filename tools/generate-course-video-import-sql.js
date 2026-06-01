const fs = require("fs");
const path = require("path");

const courseMap = {
  javascript: {
    id: "840a7353-06d2-41d9-3c87-08dd4874f647",
    modulePrefix: "JavaScript co ban",
  },
  java: {
    id: "96fc4ec7-f56f-479a-f36b-08dd4b00dfad",
    modulePrefix: "Java co ban",
  },
  react: {
    id: "cb7df934-cab9-46e4-0afa-08dd511c66f5",
    modulePrefix: "ReactJS co ban",
  },
};

function sqlString(value) {
  return `N'${String(value ?? "").replace(/'/g, "''")}'`;
}

function moduleTitle(prefix, start, end) {
  return `${prefix} - Bai ${start} den ${end}`;
}

const playlists = JSON.parse(
  fs.readFileSync(path.join(__dirname, "youtube-playlists.json"), "utf8")
);

const lines = [
  "SET XACT_ABORT ON;",
  "BEGIN TRANSACTION;",
  "",
];

for (const [key, config] of Object.entries(courseMap)) {
  const videos = playlists[key]?.videos || [];
  if (!videos.length) {
    throw new Error(`No videos found for ${key}`);
  }

  lines.push(`-- ${key}: ${videos.length} videos`);
  lines.push(`DECLARE @${key}CourseId uniqueidentifier = '${config.id}';`);
  lines.push(
    `UPDATE l SET Status = 0 FROM Lessons l INNER JOIN Modules m ON m.Id = l.ModuleId WHERE m.CourseId = @${key}CourseId;`
  );
  lines.push(`UPDATE Modules SET Status = 0 WHERE CourseId = @${key}CourseId;`);
  lines.push(`UPDATE Courses SET PreviewVideoUrl = ${sqlString(videos[0].url)} WHERE Id = @${key}CourseId;`);

  for (let offset = 0; offset < videos.length; offset += 10) {
    const group = videos.slice(offset, offset + 10);
    const moduleOrder = Math.floor(offset / 10) + 1;
    const title = moduleTitle(config.modulePrefix, offset + 1, offset + group.length);
    lines.push("");
    lines.push(
      `INSERT INTO Modules (Title, [Order], Status, CourseId) VALUES (${sqlString(title)}, ${moduleOrder}, 1, @${key}CourseId);`
    );
    lines.push(`DECLARE @${key}Module${moduleOrder} int = CONVERT(int, SCOPE_IDENTITY());`);

    for (const [index, video] of group.entries()) {
      const order = index + 1;
      const description = video.duration
        ? `Video ${video.index} - thoi luong ${video.duration}`
        : `Video ${video.index}`;
      lines.push(
        `INSERT INTO Lessons (Title, Description, UrlVideo, [Order], VideoDuration, Status, ModuleId) VALUES (` +
          `${sqlString(video.title)}, ${sqlString(description)}, ${sqlString(video.url)}, ${order}, NULL, 1, @${key}Module${moduleOrder});`
      );
    }
  }

  lines.push("");
}

lines.push("COMMIT TRANSACTION;");
lines.push("");

const output = path.join(__dirname, "import-youtube-playlists.sql");
fs.writeFileSync(output, `${lines.join("\n")}\n`, "utf8");
console.log(output);
