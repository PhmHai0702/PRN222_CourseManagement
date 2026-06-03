const fs = require("fs");
const path = require("path");

function removeDiacritics(str) {
  if (!str) return "";
  return str
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D");
}

function sqlStr(val) {
  return `N'${String(removeDiacritics(val) ?? "").replace(/'/g, "''")}'`;
}

const playlists = JSON.parse(
  fs.readFileSync(path.join(__dirname, "youtube-playlists.json"), "utf8")
);

const courseDefs = [
  // --- CATEGORY 1: Lap trinh Web ---
  {
    key: "nextjs",
    id: "a1b2c3d4-e5f6-7890-abcd-111111111111",
    catName: "Lap trinh Web",
    title: "Khoa hoc Next.js 14 va Web Development",
    description: "Xay dung ung dung Web chuan Production voi Next.js 14 App Router, Server Components va Server Actions.",
    previewImage: "https://images.unsplash.com/photo-1618401471353-b98afee0b2eb?w=800",
    level: 2,
    status: 1,
    courseType: 1,
    modulePrefix: "Next.js 14 thuc chien"
  },
  {
    key: "typescript",
    id: "a1b2c3d4-e5f6-7890-abcd-222222222222",
    catName: "Lap trinh Web",
    title: "Khoa hoc TypeScript Lap Trinh Web Cho Nguoi Moi",
    description: "Lap trinh Web an toan voi Static Typing, Interfaces, Generics va tich hop TypeScript vao ung dung Web.",
    previewImage: "https://raw.githubusercontent.com/github/explore/80688e429a7d4ef2fca1e82350fe8e3517d3494d/topics/typescript/typescript.png",
    level: 1,
    status: 1,
    courseType: 0,
    modulePrefix: "TypeScript can ban"
  },
  {
    key: "htmlcss",
    id: "a1b2c3d4-e5f6-7890-abcd-555555555555",
    catName: "Lap trinh Web",
    title: "Khoa hoc HTML CSS va Responsive Web Design",
    description: "Hoc giao dien Web tu con so 0, nam vung HTML5, CSS3, Flexbox, CSS Grid va thiet ke Responsive Web.",
    previewImage: "https://images.unsplash.com/photo-1507238691740-187a5b1d37b8?w=800",
    level: 1,
    status: 1,
    courseType: 0,
    modulePrefix: "HTML CSS nhap mon",
    fallbackKey: "javascript"
  },
  {
    key: "vuejs",
    id: "a1b2c3d4-e5f6-7890-abcd-666666666666",
    catName: "Lap trinh Web",
    title: "Khoa hoc VueJS 3 va Frontend Framework",
    description: "Xay dung giao dien Web hien dai voi Vue 3 Composition API, Pinia State Management va Vue Router.",
    previewImage: "https://raw.githubusercontent.com/github/explore/80688e429a7d4ef2fca1e82350fe8e3517d3494d/topics/vue/vue.png",
    level: 2,
    status: 1,
    courseType: 1,
    modulePrefix: "VueJS 3 thuc hanh",
    fallbackKey: "react"
  },

  // --- CATEGORY 2: JavaScript ---
  {
    key: "javascript",
    id: "840a7353-06d2-41d9-3c87-08dd4874f647",
    catName: "JavaScript",
    title: "Khoa hoc JavaScript tu co ban den nang cao",
    description: "Khoa hoc lap trinh JavaScript tu co ban toi nang cao danh cho nguoi moi bat dau. Nam vung cu phap, DOM, Async/Await va ES6+.",
    previewImage: "https://techvccloud.mediacdn.vn/2018/11/23/js-15429579443112042672363-crop-1542957949936317424252.png",
    level: 1,
    status: 1,
    courseType: 0,
    modulePrefix: "JavaScript co ban"
  },
  {
    key: "js_async",
    id: "a1b2c3d4-e5f6-7890-abcd-777777777777",
    catName: "JavaScript",
    title: "Khoa hoc JavaScript Async, Promise va ES6+",
    description: "Chuyen sau ve bat dong bo trong JavaScript, Promise, Async/Await, Event Loop va cac tinh nang moi cua ES6+.",
    previewImage: "https://images.unsplash.com/photo-1579468118864-1b9ea3c0db4a?w=800",
    level: 2,
    status: 1,
    courseType: 1,
    modulePrefix: "JavaScript Async nang cao",
    fallbackKey: "javascript"
  },

  // --- CATEGORY 3: React ---
  {
    key: "react",
    id: "cb7df934-cab9-46e4-0afa-08dd511c66f5",
    catName: "React",
    title: "Khoa hoc ReactJS tu co ban den nang cao",
    description: "Khoa hoc lap trinh ReactJS hien dai voi Hooks, Component Lifecycle, State Management va ket noi REST API.",
    previewImage: "https://thuanbui.me/wp-content/uploads/2021/08/react-js.png",
    level: 2,
    status: 1,
    courseType: 1,
    modulePrefix: "ReactJS co ban"
  },
  {
    key: "redux",
    id: "a1b2c3d4-e5f6-7890-abcd-888888888888",
    catName: "React",
    title: "Khoa hoc Redux Toolkit va Quan Ly State Trong React",
    description: "Quan ly trang thai ung dung React quy mo lon voi Redux Toolkit, RTK Query va Middleware.",
    previewImage: "https://raw.githubusercontent.com/github/explore/80688e429a7d4ef2fca1e82350fe8e3517d3494d/topics/redux/redux.png",
    level: 2,
    status: 1,
    courseType: 0,
    modulePrefix: "Redux Toolkit thuc hanh",
    fallbackKey: "react"
  },
  {
    key: "react_native",
    id: "a1b2c3d4-e5f6-7890-abcd-999999999999",
    catName: "React",
    title: "Khoa hoc React Native Lap Trinh Mobile Cross-Platform",
    description: "Xay dung ung dung di dong iOS va Android bang React Native, Expo va RESTful API integration.",
    previewImage: "https://images.unsplash.com/photo-1512941937669-90a1b58e7e9c?w=800",
    level: 2,
    status: 1,
    courseType: 1,
    modulePrefix: "React Native Mobile",
    fallbackKey: "react"
  },

  // --- CATEGORY 4: Backend ---
  {
    key: "java",
    id: "96fc4ec7-f56f-479a-f36b-08dd4b00dfad",
    catName: "Backend",
    title: "Khoa hoc Lap trinh Java va Core Backend",
    description: "Khoa hoc lap trinh Java tu co ban toi OOP nang cao, chuan bi kien thuc xay dung he thong Backend enterprise.",
    previewImage: "https://logos-world.net/wp-content/uploads/2022/07/Java-Logo.png",
    level: 1,
    status: 1,
    courseType: 0,
    modulePrefix: "Java co ban"
  },
  {
    key: "dotnet",
    id: "a1b2c3d4-e5f6-7890-abcd-333333333333",
    catName: "Backend",
    title: "Khoa hoc C# va ASP.NET Core Web API",
    description: "Huong dan lap trinh Backend voi C# .NET 8, Entity Framework Core, RESTful API va SQL Server.",
    previewImage: "https://raw.githubusercontent.com/github/explore/80688e429a7d4ef2fca1e82350fe8e3517d3494d/topics/dotnet/dotnet.png",
    level: 2,
    status: 1,
    courseType: 1,
    modulePrefix: "ASP.NET Core Backend"
  },
  {
    key: "python",
    id: "a1b2c3d4-e5f6-7890-abcd-444444444444",
    catName: "Backend",
    title: "Khoa hoc Lap trinh Python Backend va Scripting",
    description: "Hoc ngon ngu Python tu co ban den cau truc du lieu, xu ly du lieu va xay dung dich vu Backend.",
    previewImage: "https://raw.githubusercontent.com/github/explore/80688e429a7d4ef2fca1e82350fe8e3517d3494d/topics/python/python.png",
    level: 1,
    status: 1,
    courseType: 0,
    modulePrefix: "Python lap trinh"
  },
  {
    key: "nodejs",
    id: "a1b2c3d4-e5f6-7890-abcd-aaaaaaaaaaaa",
    catName: "Backend",
    title: "Khoa hoc Node.js Express va Building RESTful API",
    description: "Lap trinh Server-side voi Node.js, Express framework, MongoDB/PostgreSQL va JWT Authentication.",
    previewImage: "https://raw.githubusercontent.com/github/explore/80688e429a7d4ef2fca1e82350fe8e3517d3494d/topics/nodejs/nodejs.png",
    level: 2,
    status: 1,
    courseType: 1,
    modulePrefix: "NodeJS Backend thuc hanh",
    fallbackKey: "javascript"
  },
  {
    key: "cpp",
    id: "a3798d62-96fb-4e1d-bfdc-261a5c4f9e0b",
    catName: "Backend",
    title: "Khoa hoc C++ va Cau Truc Du Lieu Thuat Toan Backend",
    description: "Nam vung ngon ngu C++, con tro, bo nho, va thuat toan nen tang cho ky su Backend.",
    previewImage: "https://raw.githubusercontent.com/github/explore/80688e429a7d4ef2fca1e82350fe8e3517d3494d/topics/cpp/cpp.png",
    level: 1,
    status: 1,
    courseType: 0,
    modulePrefix: "C++ nhap mon"
  }
];

const lines = [
  "SET XACT_ABORT ON;",
  "BEGIN TRANSACTION;",
  "",
  "-- Clean existing categories and reset exact 4 unaccented target categories",
  "UPDATE Categories SET Name = N'Lap trinh Web', Description = N'Cac khoa hoc ve phat trien ung dung Web Frontend, Next.js, HTML/CSS, TypeScript' WHERE Name LIKE N'%Web%';",
  "UPDATE Categories SET Name = N'JavaScript', Description = N'Cac khoa hoc ve ngon ngu lap trinh JavaScript tu co ban den nang cao' WHERE Name LIKE N'%JavaScript%';",
  "UPDATE Categories SET Name = N'React', Description = N'Cac khoa hoc ve thu vien ReactJS, Redux, React Hooks va Single Page Application' WHERE Name LIKE N'%React%';",
  "UPDATE Categories SET Name = N'Backend', Description = N'Cac khoa hoc lap trinh Backend voi Java, .NET C#, Python, Node.js va Web API' WHERE Name LIKE N'%Backend%';",
  "",
  "IF NOT EXISTS (SELECT 1 FROM Categories WHERE Name = N'Lap trinh Web')",
  "    INSERT INTO Categories (Name, Description) VALUES (N'Lap trinh Web', N'Cac khoa hoc ve phat trien ung dung Web Frontend, Next.js, HTML/CSS, TypeScript');",
  "IF NOT EXISTS (SELECT 1 FROM Categories WHERE Name = N'JavaScript')",
  "    INSERT INTO Categories (Name, Description) VALUES (N'JavaScript', N'Cac khoa hoc ve ngon ngu lap trinh JavaScript tu co ban den nang cao');",
  "IF NOT EXISTS (SELECT 1 FROM Categories WHERE Name = N'React')",
  "    INSERT INTO Categories (Name, Description) VALUES (N'React', N'Cac khoa hoc ve thu vien ReactJS, Redux, React Hooks va Single Page Application');",
  "IF NOT EXISTS (SELECT 1 FROM Categories WHERE Name = N'Backend')",
  "    INSERT INTO Categories (Name, Description) VALUES (N'Backend', N'Cac khoa hoc lap trinh Backend voi Java, .NET C#, Python, Node.js va Web API');",
  "",
  "DECLARE @CatWeb int = (SELECT TOP 1 Id FROM Categories WHERE Name = N'Lap trinh Web');",
  "DECLARE @CatJS int = (SELECT TOP 1 Id FROM Categories WHERE Name = N'JavaScript');",
  "DECLARE @CatReact int = (SELECT TOP 1 Id FROM Categories WHERE Name = N'React');",
  "DECLARE @CatBackend int = (SELECT TOP 1 Id FROM Categories WHERE Name = N'Backend');",
  ""
];

for (const course of courseDefs) {
  let videoList = (playlists[course.key] && playlists[course.key].videos) || [];
  if (!videoList.length && course.fallbackKey) {
    videoList = (playlists[course.fallbackKey] && playlists[course.fallbackKey].videos) || [];
  }
  if (!videoList.length) {
    console.warn(`No videos found for ${course.key}`);
    continue;
  }

  const catVarMap = {
    "Lap trinh Web": "@CatWeb",
    "JavaScript": "@CatJS",
    "React": "@CatReact",
    "Backend": "@CatBackend"
  };
  const catVar = catVarMap[course.catName];
  const previewUrl = videoList[0].url;

  lines.push(`-- ========================================================`);
  lines.push(`-- Course: ${course.title} (${videoList.length} videos)`);
  lines.push(`-- ========================================================`);
  lines.push(`DECLARE @c_${course.key.replace(/[^a-zA-Z0-9]/g, "_")} uniqueidentifier = '${course.id}';`);
  lines.push(`IF EXISTS (SELECT 1 FROM Courses WHERE Id = @c_${course.key.replace(/[^a-zA-Z0-9]/g, "_")})`);
  lines.push(`BEGIN`);
  lines.push(`    UPDATE Courses SET Title = ${sqlStr(course.title)}, Description = ${sqlStr(course.description)}, PreviewImage = ${sqlStr(course.previewImage)}, PreviewVideoUrl = ${sqlStr(previewUrl)}, Level = ${course.level}, Status = ${course.status}, CategoryId = ${catVar}, CourseType = ${course.courseType} WHERE Id = @c_${course.key.replace(/[^a-zA-Z0-9]/g, "_")};`);
  lines.push(`END`);
  lines.push(`ELSE`);
  lines.push(`BEGIN`);
  lines.push(`    INSERT INTO Courses (Id, Title, Description, PreviewImage, PreviewVideoUrl, Level, Status, CategoryId, CourseType) VALUES (@c_${course.key.replace(/[^a-zA-Z0-9]/g, "_")}, ${sqlStr(course.title)}, ${sqlStr(course.description)}, ${sqlStr(course.previewImage)}, ${sqlStr(previewUrl)}, ${course.level}, ${course.status}, ${catVar}, ${course.courseType});`);
  lines.push(`END;`);
  lines.push(``);
  lines.push(`-- Clear old lessons & modules for this course before re-importing clean unaccented structure`);
  lines.push(`UPDATE l SET Status = 0 FROM Lessons l INNER JOIN Modules m ON m.Id = l.ModuleId WHERE m.CourseId = @c_${course.key.replace(/[^a-zA-Z0-9]/g, "_")};`);
  lines.push(`UPDATE Modules SET Status = 0 WHERE CourseId = @c_${course.key.replace(/[^a-zA-Z0-9]/g, "_")};`);
  lines.push(``);

  for (let offset = 0; offset < videoList.length; offset += 10) {
    const group = videoList.slice(offset, offset + 10);
    const moduleOrder = Math.floor(offset / 10) + 1;
    const title = `${course.modulePrefix} - Phan ${moduleOrder} (Bai ${offset + 1} - ${offset + group.length})`;
    lines.push(`INSERT INTO Modules (Title, [Order], Status, CourseId) VALUES (${sqlStr(title)}, ${moduleOrder}, 1, @c_${course.key.replace(/[^a-zA-Z0-9]/g, "_")});`);
    lines.push(`DECLARE @m_${course.key.replace(/[^a-zA-Z0-9]/g, "_")}_${moduleOrder} int = CONVERT(int, SCOPE_IDENTITY());`);

    for (const [idx, video] of group.entries()) {
      const lessonOrder = idx + 1;
      const desc = video.duration
        ? `Bai hoc ${video.index} - Thoi luong: ${video.duration}`
        : `Bai hoc ${video.index}`;
      lines.push(
        `INSERT INTO Lessons (Title, Description, UrlVideo, [Order], VideoDuration, Status, ModuleId) VALUES (${sqlStr(video.title)}, ${sqlStr(desc)}, ${sqlStr(video.url)}, ${lessonOrder}, NULL, 1, @m_${course.key.replace(/[^a-zA-Z0-9]/g, "_")}_${moduleOrder});`
      );
    }
    lines.push(``);
  }
}

lines.push("-- Remove orphan empty categories if any");
lines.push("DELETE FROM Categories WHERE Id NOT IN (@CatWeb, @CatJS, @CatReact, @CatBackend) AND Id NOT IN (SELECT DISTINCT CategoryId FROM Courses);");
lines.push("COMMIT TRANSACTION;");
lines.push("PRINT 'Unaccented Categories, Courses, Modules, and Lessons imported successfully!';");
lines.push("");

const outputFile = path.join(__dirname, "import_unaccented_courses.sql");
fs.writeFileSync(outputFile, lines.join("\n"), "utf8");
console.log(`Successfully generated unaccented SQL script at: ${outputFile}`);
