using CourseManagement.Business.Services;
using CourseManagement.Business.Services.IService;
using CourseManagement.DataAccess.Data;
using CourseManagement.DataAccess.Repositorys.IRepositorys;
using CourseManagement.DataAccess.Repositorys;
using CourseManagement.Model.Model;
using CourseManagementAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using CourseManagementAPI.Mappings;
using CourseManagementAPI.Hubs;
using Amazon.S3;
using CourseManagement.Model.Mail;
using CourseManagement.Model.Constant;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.Configure<IdentityOptions>(options => {
    options.Password.RequireDigit = false; // Không yêu c?u s?
    options.Password.RequireLowercase = false; // Không yêu c?u ch? thu?ng
    options.Password.RequireUppercase = false; // Không yêu c?u ch? hoa
    options.Password.RequireNonAlphanumeric = false; // Không yêu c?u ký t? d?c bi?t
    options.Password.RequiredLength = 6; // Ð? dài t?i thi?u (có th? thay d?i)
    options.Password.RequiredUniqueChars = 0; // Không yêu c?u ký t? duysnh?t
});

var configuration = builder.Configuration;
var connectionString = builder.Configuration.GetConnectionString("DBContext")
    ?? "Server=localhost\\MSSQLSERVER02;Database=FUNewsManagement;User Id=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True";

builder.Services.AddDbContext<CourseManagementDb>(options =>
{
    options.UseSqlServer(connectionString);
});
builder.Services.AddSignalR();

builder.Services.AddIdentityApiEndpoints<AppUser>().
    AddRoles<IdentityRole>().
    AddEntityFrameworkStores<CourseManagementDb>();

builder.Services.AddScoped<MinioFileService>();
builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
builder.Services.AddScoped<ModuleRepository>();
builder.Services.AddScoped<ILessonRepository, LessonRepository>();
builder.Services.AddScoped<LessonRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<CourseRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<DocumentRepository>();

builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<ICourseRecommendationService, CourseRecommendationService>();
builder.Services.AddScoped<ILearningDashboardService, LearningDashboardService>();
builder.Services.AddScoped<ICourseComparisonService, CourseComparisonService>();
builder.Services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
builder.Services.AddHttpClient();

builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<MailService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    option.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
    option.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    option.OperationFilter<SecurityRequirementsOperationFilter>();
});

var corsOrigins = new[]
{
    builder.Configuration["BackendUrl"],
    builder.Configuration["FrontendUrl"],
    "https://localhost:7195",
    "http://localhost:5187"
}.Where(origin => !string.IsNullOrWhiteSpace(origin))
 .Select(origin => origin!)
 .ToArray();

builder.Services.AddCors(option => option.AddPolicy("wasm",
    policy => policy.WithOrigins(corsOrigins)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()
    .WithExposedHeaders("Content-Disposition")
    ));


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CourseManagementDb>();
    dbContext.Database.Migrate();
    SeedHistoricalCourses(dbContext, app.Environment.ContentRootPath);
    SeedSampleBlogs(dbContext);
}

app.MapIdentityApi<AppUser>();

app.UseCors("wasm");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<CommentHub>("/commentHub"); // SignalR Hub
});

app.Run();

static void SeedHistoricalCourses(CourseManagementDb dbContext, string contentRootPath)
{
    var demoCourseIds = new[]
    {
        Guid.Parse("1e4c1e1f-55f3-4b77-b289-5cd3f4f94501"),
        Guid.Parse("2a8b6c09-51cd-4cf9-9a1f-b69c67d1b232"),
        Guid.Parse("36d6fb8b-65fb-46c8-bbf8-3a9eceddbbe3"),
        Guid.Parse("47f1e626-6ee2-48ad-8ab0-4369debb45c4")
    };

    var demoCourses = dbContext.Courses.Where(course => demoCourseIds.Contains(course.Id)).ToList();
    if (demoCourses.Count > 0)
    {
        dbContext.Courses.RemoveRange(demoCourses);
        dbContext.SaveChanges();
    }

    var javascriptCourseId = Guid.Parse("840a7353-06d2-41d9-3c87-08dd4874f647");
    var javaCourseId = Guid.Parse("96fc4ec7-f56f-479a-f36b-08dd4b00dfad");
    var reactCourseId = Guid.Parse("cb7df934-cab9-46e4-0afa-08dd511c66f5");
    var dotnetCourseId = Guid.Parse("6fd20469-65df-4d53-b1d8-52e07ac2a9e2");
    var nextjsCourseId = Guid.Parse("b6f5c7f2-b9f0-4a28-8a0f-016be71427dd");
    var pythonCourseId = Guid.Parse("e99bbf0c-2b8e-4f0d-8b3d-d381b77f5d3a");
    var cppCourseId = Guid.Parse("a3798d62-96fb-4e1d-bfdc-261a5c4f9e0b");
    var typescriptCourseId = Guid.Parse("38f4a112-0d64-447b-8970-5c1d36908eb9");

    if (dbContext.Courses.Any(course =>
        course.Id == javascriptCourseId || course.Id == javaCourseId || course.Id == reactCourseId))
    {
        EnsureHistoricalCourseContent(dbContext, javascriptCourseId, javaCourseId, reactCourseId,
            dotnetCourseId, nextjsCourseId, pythonCourseId, cppCourseId, typescriptCourseId, contentRootPath);
        return;
    }

    var frontendCategory = new Category
    {
        Name = "Frontend",
        Description = "Frontend development focuses on building the user interface and user experience of a website or application. It involves technologies such as HTML, CSS, and JavaScript, along with modern frameworks like React, Vue.js, and Angular. Frontend developers ensure that applications are visually appealing, responsive, and provide a seamless user experience across different devices and browsers."
    };

    var backendCategory = new Category
    {
        Name = "Backend",
        Description = "Back-end development means working on server-side software, which focuses on everything you can't see on a website. Back-end developers ensure the website performs correctly, focusing on databases, back-end logic, application programming interface (APIs), architecture, and servers."
    };

    dbContext.Categories.AddRange(frontendCategory, backendCategory);

    dbContext.Courses.AddRange(
        new Course
        {
            Id = javascriptCourseId,
            Title = "Khoa hoc Javascript tu co ban den nang cao",
            Description = "Khoa hoc lap trinh Javascript tu co ban toi nang cao danh cho ban. Tham gia ngay de kham pha suc manh cua Javascript!",
            PreviewImage = "https://techvccloud.mediacdn.vn/2018/11/23/js-15429579443112042672363-crop-1542957949936317424252.png",
            PreviewVideoUrl = "https://www.youtube.com/watch?v=DHjqpvDnNGE",
            Level = CourseLevel.Intermediate,
            Status = CourseStatus.Publish,
            CourseType = CourseType.FreeCourse,
            Category = frontendCategory,
            Modules = new List<CourseManagement.Model.Model.Module>
            {
                CreateHistoricalModule(2, "Gioi thieu Javascript", javascriptCourseId, 1, new[]
                {
                    CreateHistoricalLesson(1, "Gioi thieu Javascript", "Javascript la ngon ngu lap trinh bac cao, cuc ky linh hoat duoc su dung chu yeu de tao ra ung dung chay tren trinh duyet web.", "https://www.youtube.com/watch?v=PkZNo7MFNFg", 1),
                    CreateHistoricalLesson(3, "IDE la gi?", "IDE la phan mem may tinh khong the thieu khi lap trinh JavaScript.", "https://www.youtube.com/watch?v=jS4aFq5-91M", 2),
                    CreateHistoricalLesson(2, "Dev Tools la gi?", "Dev Tools giup xem loi va debug code JavaScript tren trinh duyet.", "https://www.youtube.com/watch?v=jS4aFq5-91M", 3)
                }),
                CreateHistoricalModule(3, "Javascript co ban", javascriptCourseId, 2, new[]
                {
                    CreateHistoricalLesson(4, "Chuong trinh Javascript dau tien", "Viet chuong trinh JavaScript dau tien tren trinh duyet.", "https://www.youtube.com/watch?v=PkZNo7MFNFg", 1),
                    CreateHistoricalLesson(6, "Cau truc code trong Javascript", "Cau lenh la don vi co ban de xay dung chuong trinh JavaScript.", "https://www.youtube.com/watch?v=PkZNo7MFNFg", 2),
                    CreateHistoricalLesson(5, "Strict Mode trong JavaScript", "Tim hieu che do nghiem ngat trong JavaScript.", "https://www.youtube.com/watch?v=jS4aFq5-91M", 3)
                }),
                CreateHistoricalModule(4, "Object trong Javascript", javascriptCourseId, 3, Array.Empty<Lesson>()),
                CreateHistoricalModule(5, "New", javascriptCourseId, 4, new[]
                {
                    CreateHistoricalLesson(7, "DOM la gi?", "Lam quen voi DOM va cach JavaScript tuong tac voi trinh duyet.", "https://www.youtube.com/watch?v=PkZNo7MFNFg", 1)
                }),
                CreateHistoricalModule(10, "new 2", javascriptCourseId, 5, Array.Empty<Lesson>()),
                CreateHistoricalModule(9, "new 3", javascriptCourseId, 6, Array.Empty<Lesson>()),
                CreateHistoricalModule(11, "New 4", javascriptCourseId, 7, Array.Empty<Lesson>())
            }
        },
        new Course
        {
            Id = javaCourseId,
            Title = "Khoa hoc Java tu co ban den nang cao",
            Description = "Khoa hoc lap trinh Java tu co ban toi nang cao danh cho ban. Tham gia ngay de kham pha suc manh cua Java!",
            PreviewImage = "https://logos-world.net/wp-content/uploads/2022/07/Java-Logo.png",
            PreviewVideoUrl = "https://www.youtube.com/watch?v=l9AzO1FMgM8",
            Level = CourseLevel.Intermediate,
            Status = CourseStatus.Publish,
            CourseType = CourseType.FreeCourse,
            Category = backendCategory,
            Modules = CreateJavaModules().ToList()
        },
        new Course
        {
            Id = reactCourseId,
            Title = "Khoa hoc Reactjs tu co ban den nang cao",
            Description = "Khoa hoc lap trinh Reactjs tu co ban toi nang cao danh cho ban. Tham gia ngay de kham pha suc manh cua Reactjs!",
            PreviewImage = "https://upload.wikimedia.org/wikipedia/commons/a/a7/React-icon.svg",
            PreviewVideoUrl = "https://www.youtube.com/watch?v=Tn6-PIqc4UM",
            Level = CourseLevel.Advanced,
            Status = CourseStatus.Publish,
            CourseType = CourseType.ProCourse,
            Category = frontendCategory,
            Modules = CreateReactModules().ToList()
        });

    dbContext.SaveChanges();
    EnsureAdditionalPlaylistCourses(dbContext, dotnetCourseId, nextjsCourseId, pythonCourseId, cppCourseId, typescriptCourseId);
    dbContext.SaveChanges();
    SyncCoursePlaylistsFromJson(dbContext, contentRootPath, javascriptCourseId, javaCourseId, reactCourseId,
        dotnetCourseId, nextjsCourseId, pythonCourseId, cppCourseId, typescriptCourseId);
    dbContext.SaveChanges();
}

static void EnsureHistoricalCourseContent(
    CourseManagementDb dbContext,
    Guid javascriptCourseId,
    Guid javaCourseId,
    Guid reactCourseId,
    Guid dotnetCourseId,
    Guid nextjsCourseId,
    Guid pythonCourseId,
    Guid cppCourseId,
    Guid typescriptCourseId,
    string contentRootPath)
{
    var hasChanges = false;

    if (dbContext.Courses.Any(course => course.Id == javascriptCourseId))
    {
        hasChanges |= RepairJavaScriptContent(dbContext, javascriptCourseId);
    }

    if (dbContext.Courses.Any(course => course.Id == javaCourseId)
        && !dbContext.Modules.Any(module => module.CourseId == javaCourseId && module.Status == ModuleStatus.Active))
    {
        foreach (var module in CreateJavaModules())
        {
            module.CourseId = javaCourseId;
            dbContext.Modules.Add(module);
        }
        hasChanges = true;
    }

    if (dbContext.Courses.Any(course => course.Id == reactCourseId)
        && !dbContext.Modules.Any(module => module.CourseId == reactCourseId && module.Status == ModuleStatus.Active))
    {
        foreach (var module in CreateReactModules())
        {
            module.CourseId = reactCourseId;
            dbContext.Modules.Add(module);
        }
        hasChanges = true;
    }

    if (dbContext.Courses.Any(course => course.Id == reactCourseId))
    {
        hasChanges |= RepairReactContent(dbContext, reactCourseId);
    }

    var addedPlaylistCourses = EnsureAdditionalPlaylistCourses(dbContext, dotnetCourseId, nextjsCourseId, pythonCourseId, cppCourseId, typescriptCourseId);
    if (addedPlaylistCourses)
    {
        dbContext.SaveChanges();
        hasChanges = true;
    }

    hasChanges |= SyncCoursePlaylistsFromJson(dbContext, contentRootPath, javascriptCourseId, javaCourseId, reactCourseId,
        dotnetCourseId, nextjsCourseId, pythonCourseId, cppCourseId, typescriptCourseId);

    if (hasChanges)
    {
        dbContext.SaveChanges();
    }
}

static bool EnsureAdditionalPlaylistCourses(
    CourseManagementDb dbContext,
    Guid dotnetCourseId,
    Guid nextjsCourseId,
    Guid pythonCourseId,
    Guid cppCourseId,
    Guid typescriptCourseId)
{
    var hasChanges = false;
    var frontendCategory = EnsureCategory(dbContext, "Frontend",
        "Frontend development focuses on building user interfaces with HTML, CSS, JavaScript and modern frameworks.");
    var backendCategory = EnsureCategory(dbContext, "Backend",
        "Backend development focuses on server-side logic, databases, APIs, architecture and servers.");

    hasChanges |= AddCourseIfMissing(dbContext, new Course
    {
        Id = dotnetCourseId,
        Title = "Khoa hoc .NET nen tang",
        Description = "Khoa hoc lap trinh .NET nen tang, phu hop de hoc C#, kieu du lieu, cau truc dieu khien, OOP va LINQ.",
        PreviewImage = "https://i.ytimg.com/vi/-GHF0aAvKEI/hqdefault.jpg",
        PreviewVideoUrl = "https://www.youtube.com/watch?v=-GHF0aAvKEI",
        Level = CourseLevel.Intermediate,
        Status = CourseStatus.Publish,
        CourseType = CourseType.ProCourse,
        Category = backendCategory
    });

    hasChanges |= AddCourseIfMissing(dbContext, new Course
    {
        Id = nextjsCourseId,
        Title = "Khoa hoc Next.js 14 mien phi",
        Description = "Khoa hoc Next.js 14 thuc chien voi routing, component, style, image, font va cac kien thuc nen tang.",
        PreviewImage = "https://i.ytimg.com/vi/ucdjfU_XKpw/hqdefault.jpg",
        PreviewVideoUrl = "https://www.youtube.com/watch?v=ucdjfU_XKpw",
        Level = CourseLevel.Advanced,
        Status = CourseStatus.Publish,
        CourseType = CourseType.ProCourse,
        Category = frontendCategory
    });

    hasChanges |= AddCourseIfMissing(dbContext, new Course
    {
        Id = pythonCourseId,
        Title = "Khoa hoc Python co ban",
        Description = "Khoa hoc lap trinh Python co ban, tu cai dat moi truong den bien, kieu du lieu, chuoi va cac cau truc lap trinh.",
        PreviewImage = "https://i.ytimg.com/vi/NZj6LI5a9vc/hqdefault.jpg",
        PreviewVideoUrl = "https://www.youtube.com/watch?v=NZj6LI5a9vc",
        Level = CourseLevel.Beginner,
        Status = CourseStatus.Publish,
        CourseType = CourseType.ProCourse,
        Category = backendCategory
    });

    hasChanges |= AddCourseIfMissing(dbContext, new Course
    {
        Id = cppCourseId,
        Title = "Khoa hoc C++ co ban",
        Description = "Khoa hoc lap trinh C++ co ban tu tong quan, gioi thieu ngon ngu, bien, kieu du lieu den cau truc chuong trinh.",
        PreviewImage = "https://i.ytimg.com/vi/WS05AU6YYm4/hqdefault.jpg",
        PreviewVideoUrl = "https://www.youtube.com/watch?v=WS05AU6YYm4",
        Level = CourseLevel.Beginner,
        Status = CourseStatus.Publish,
        CourseType = CourseType.FreeCourse,
        Category = backendCategory
    });

    hasChanges |= AddCourseIfMissing(dbContext, new Course
    {
        Id = typescriptCourseId,
        Title = "Khoa hoc TypeScript co ban",
        Description = "Khoa hoc TypeScript co ban tu cai dat moi truong, kieu du lieu, annotation, inference den cac tinh nang can thiet.",
        PreviewImage = "https://i.ytimg.com/vi/lE4DfZKlwDA/hqdefault.jpg",
        PreviewVideoUrl = "https://www.youtube.com/watch?v=lE4DfZKlwDA",
        Level = CourseLevel.Intermediate,
        Status = CourseStatus.Publish,
        CourseType = CourseType.FreeCourse,
        Category = frontendCategory
    });

    return hasChanges;
}

static Category EnsureCategory(CourseManagementDb dbContext, string name, string description)
{
    var category = dbContext.Categories.FirstOrDefault(item => item.Name == name);
    if (category != null)
    {
        return category;
    }

    category = new Category
    {
        Name = name,
        Description = description
    };
    dbContext.Categories.Add(category);
    return category;
}

static void SeedSampleBlogs(CourseManagementDb dbContext)
{
    var author = dbContext.Users.FirstOrDefault(user => user.Email == "admin@admin.com")
        ?? dbContext.Users.FirstOrDefault();
    if (author == null)
    {
        return;
    }

    var webCategory = EnsureCategory(dbContext, "Web Development",
        "Articles about web development, frontend, backend, tools and project practice.");
    var frontendCategory = EnsureCategory(dbContext, "Frontend",
        "Frontend development focuses on building user interfaces with HTML, CSS, JavaScript and modern frameworks.");
    var backendCategory = EnsureCategory(dbContext, "Backend",
        "Backend development focuses on server-side logic, databases, APIs, architecture and servers.");
    var studyTipsCategory = EnsureCategory(dbContext, "Study Tips",
        "Practical tips for learning programming and building better study habits.");
    var careerCategory = EnsureCategory(dbContext, "Career",
        "Career orientation and technology choices for new developers.");

    var blogs = new[]
    {
        CreateSampleBlog(
            "Lo trinh hoc lap trinh web cho nguoi moi",
            "<p>Neu ban moi bat dau, hay di theo thu tu HTML, CSS, JavaScript, sau do moi chon React hoac Next.js.</p><p>Moi giai doan nen co mot du an nho: landing page, todo app, blog ca nhan, roi den ung dung co dang nhap va API.</p><ul><li>Nam chac HTML/CSS de dung layout.</li><li>Hoc JavaScript bang bai tap thao tac DOM.</li><li>Ket noi API de hieu frontend va backend lam viec voi nhau.</li></ul>",
            "https://images.unsplash.com/photo-1498050108023-c5249f4df085?auto=format&fit=crop&w=1200&q=80",
            author.Id,
            DateTime.UtcNow.AddDays(-4),
            128,
            webCategory,
            frontendCategory,
            studyTipsCategory),
        CreateSampleBlog(
            "Cach hoc JavaScript hieu qua qua du an nho",
            "<p>JavaScript se de hieu hon khi ban dung no de giai quyet mot tinh huong that.</p><p>Hay tao cac project nho nhu may tinh, quan ly ghi chu, bo loc khoa hoc, gio hang don gian. Sau moi project, viet lai loi ban gap va cach sua.</p><ul><li>Doc loi tren Console truoc khi sua.</li><li>Chia logic thanh ham nho.</li><li>Commit tung buoc de de quay lai khi can.</li></ul>",
            "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=1200&q=80",
            author.Id,
            DateTime.UtcNow.AddDays(-3),
            96,
            webCategory,
            frontendCategory,
            studyTipsCategory),
        CreateSampleBlog(
            "Nen chon .NET, Java hay Python khi moi bat dau backend?",
            "<p>Ca ba deu la lua chon tot, nhung nen chon theo muc tieu gan nhat cua ban.</p><p>.NET phu hop neu ban hoc C# va muon lam web API voi SQL Server. Java manh trong enterprise va Android. Python de tiep can, hop voi automation, data va web backend nhe.</p><ul><li>Muon di theo C# va SQL Server: chon .NET.</li><li>Muon nen tang OOP enterprise: chon Java.</li><li>Muon hoc nhanh va lam nhieu linh vuc: chon Python.</li></ul>",
            "https://images.unsplash.com/photo-1515879218367-8466d910aaa4?auto=format&fit=crop&w=1200&q=80",
            author.Id,
            DateTime.UtcNow.AddDays(-2),
            154,
            webCategory,
            backendCategory,
            careerCategory),
        CreateSampleBlog(
            "React va Next.js khac nhau the nao?",
            "<p>React la thu vien xay dung UI. Next.js la framework nam tren React, bo sung routing, render tren server, image optimization va cach to chuc project day du hon.</p><p>Neu moi hoc, ban nen nam React component, props, state va hooks truoc. Khi da quen, Next.js se giup ban lam ung dung web thuc te nhanh hon.</p>",
            "https://images.unsplash.com/photo-1555066931-4365d14bab8c?auto=format&fit=crop&w=1200&q=80",
            author.Id,
            DateTime.UtcNow.AddDays(-1),
            111,
            webCategory,
            frontendCategory),
        CreateSampleBlog(
            "Meo doc loi va debug khi hoc lap trinh",
            "<p>Debug khong phai la doan cuoi cua viec hoc, ma la ky nang phai tap moi ngay.</p><p>Khi gap loi, hay doc thong bao tu dong dau tien co lien quan den code cua minh, xem file va line, sau do tao lai loi bang cach don gian nhat.</p><ul><li>Doc message loi cham lai.</li><li>Kiem tra input, output va gia tri null.</li><li>Ghi log co muc dich, dung xoa lung tung.</li></ul>",
            "https://images.unsplash.com/photo-1555949963-aa79dcee981c?auto=format&fit=crop&w=1200&q=80",
            author.Id,
            DateTime.UtcNow,
            87,
            webCategory,
            studyTipsCategory)
    };

    var existingTitles = dbContext.Blogs.Select(blog => blog.Title).ToHashSet();
    foreach (var blog in blogs)
    {
        if (!existingTitles.Contains(blog.Title))
        {
            dbContext.Blogs.Add(blog);
        }
    }

    dbContext.SaveChanges();
}

static Blog CreateSampleBlog(
    string title,
    string content,
    string imageUrl,
    string authorId,
    DateTime createdAt,
    int viewCount,
    params Category[] categories)
{
    return new Blog
    {
        Title = title,
        Content = content,
        UrlImage = imageUrl,
        CreatedAt = createdAt,
        ViewCount = viewCount,
        Status = BlogStatus.Published,
        UserId = authorId,
        Categories = categories.ToList()
    };
}

static bool AddCourseIfMissing(CourseManagementDb dbContext, Course course)
{
    if (dbContext.Courses.Any(item => item.Id == course.Id))
    {
        return false;
    }

    dbContext.Courses.Add(course);
    return true;
}

static bool SyncCoursePlaylistsFromJson(
    CourseManagementDb dbContext,
    string contentRootPath,
    Guid javascriptCourseId,
    Guid javaCourseId,
    Guid reactCourseId,
    Guid dotnetCourseId,
    Guid nextjsCourseId,
    Guid pythonCourseId,
    Guid cppCourseId,
    Guid typescriptCourseId)
{
    var playlistPath = Path.Combine(contentRootPath, "Data", "youtube-playlists.json");
    if (!File.Exists(playlistPath))
    {
        return false;
    }

    var playlists = JsonSerializer.Deserialize<Dictionary<string, YoutubePlaylistSeed>>(
        File.ReadAllText(playlistPath),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (playlists == null)
    {
        return false;
    }

    var hasChanges = false;
    hasChanges |= SyncCoursePlaylist(dbContext, javascriptCourseId, "javascript", "JavaScript co ban",
        "https://techvccloud.mediacdn.vn/2018/11/23/js-15429579443112042672363-crop-1542957949936317424252.png", playlists);
    hasChanges |= SyncCoursePlaylist(dbContext, javaCourseId, "java", "Java co ban",
        "https://logos-world.net/wp-content/uploads/2022/07/Java-Logo.png", playlists);
    hasChanges |= SyncCoursePlaylist(dbContext, reactCourseId, "react", "ReactJS co ban",
        "https://upload.wikimedia.org/wikipedia/commons/a/a7/React-icon.svg", playlists);
    hasChanges |= SyncCoursePlaylist(dbContext, dotnetCourseId, "dotnet", ".NET nen tang",
        "https://i.ytimg.com/vi/-GHF0aAvKEI/hqdefault.jpg", playlists);
    hasChanges |= SyncCoursePlaylist(dbContext, nextjsCourseId, "nextjs", "Next.js 14",
        "https://i.ytimg.com/vi/ucdjfU_XKpw/hqdefault.jpg", playlists);
    hasChanges |= SyncCoursePlaylist(dbContext, pythonCourseId, "python", "Python co ban",
        "https://i.ytimg.com/vi/NZj6LI5a9vc/hqdefault.jpg", playlists);
    hasChanges |= SyncCoursePlaylist(dbContext, cppCourseId, "cpp", "C++ co ban",
        "https://i.ytimg.com/vi/WS05AU6YYm4/hqdefault.jpg", playlists);
    hasChanges |= SyncCoursePlaylist(dbContext, typescriptCourseId, "typescript", "TypeScript co ban",
        "https://i.ytimg.com/vi/lE4DfZKlwDA/hqdefault.jpg", playlists);

    return hasChanges;
}

static bool SyncCoursePlaylist(
    CourseManagementDb dbContext,
    Guid courseId,
    string playlistKey,
    string modulePrefix,
    string previewImage,
    IReadOnlyDictionary<string, YoutubePlaylistSeed> playlists)
{
    if (!playlists.TryGetValue(playlistKey, out var playlist) || playlist.Videos.Count == 0)
    {
        return false;
    }

    var course = dbContext.Courses.FirstOrDefault(item => item.Id == courseId);
    if (course == null)
    {
        return false;
    }

    var activeLessons = dbContext.Lessons
        .Include(item => item.Module)
        .Where(item => item.Module.CourseId == courseId && item.Status == LessonStatus.Active && item.Module.Status == ModuleStatus.Active)
        .OrderBy(item => item.Module.Order)
        .ThenBy(item => item.Order)
        .ToList();

    var hasMetadataChanges = course.PreviewVideoUrl != playlist.Videos.First().Url || course.PreviewImage != previewImage;
    course.PreviewVideoUrl = playlist.Videos.First().Url;
    course.PreviewImage = previewImage;

    if (!hasMetadataChanges
        && activeLessons.Count == playlist.Videos.Count
        && activeLessons.FirstOrDefault()?.UrlVideo == playlist.Videos.First().Url
        && activeLessons.LastOrDefault()?.UrlVideo == playlist.Videos.Last().Url)
    {
        return false;
    }

    foreach (var lesson in activeLessons)
    {
        lesson.Status = LessonStatus.Delete;
    }

    var activeModules = dbContext.Modules
        .Where(item => item.CourseId == courseId && item.Status == ModuleStatus.Active)
        .ToList();

    foreach (var module in activeModules)
    {
        module.Status = ModuleStatus.Delete;
    }

    for (var offset = 0; offset < playlist.Videos.Count; offset += 10)
    {
        var group = playlist.Videos.Skip(offset).Take(10).ToList();
        var moduleOrder = offset / 10 + 1;
        var module = new CourseManagement.Model.Model.Module
        {
            Title = $"{modulePrefix} - Bai {offset + 1} den {offset + group.Count}",
            Order = moduleOrder,
            Status = ModuleStatus.Active,
            CourseId = courseId,
            Lessons = new List<Lesson>()
        };

        for (var index = 0; index < group.Count; index++)
        {
            var video = group[index];
            module.Lessons.Add(new Lesson
            {
                Title = video.Title,
                Description = string.IsNullOrWhiteSpace(video.Duration)
                    ? $"Video {video.Index}"
                    : $"Video {video.Index} - thoi luong {video.Duration}",
                UrlVideo = video.Url,
                Order = index + 1,
                VideoDuration = TryParseDuration(video.Duration),
                Status = LessonStatus.Active
            });
        }

        dbContext.Modules.Add(module);
    }

    return true;
}

static TimeSpan? TryParseDuration(string? duration)
{
    if (string.IsNullOrWhiteSpace(duration))
    {
        return null;
    }

    var parts = duration.Split(':').Select(part => int.TryParse(part, out var value) ? value : -1).ToArray();
    if (parts.Any(part => part < 0))
    {
        return null;
    }

    return parts.Length switch
    {
        2 => new TimeSpan(0, parts[0], parts[1]),
        3 => new TimeSpan(parts[0], parts[1], parts[2]),
        _ => null
    };
}

static bool RepairReactContent(CourseManagementDb dbContext, Guid reactCourseId)
{
    const string reactFullCourseVideo = "https://www.youtube.com/watch?v=bMknfKXIFA8";
    const string reactBeginnerVideo = "https://www.youtube.com/watch?v=SqcY0GlETPk";

    var hasChanges = false;
    hasChanges |= UpdateLessonVideo(dbContext, reactCourseId, "Gioi thieu ReactJS", reactFullCourseVideo);
    hasChanges |= UpdateLessonVideo(dbContext, reactCourseId, "JSX va Component", reactBeginnerVideo);
    hasChanges |= UpdateLessonVideo(dbContext, reactCourseId, "useState", reactFullCourseVideo);
    hasChanges |= UpdateLessonVideo(dbContext, reactCourseId, "Xu ly event", reactFullCourseVideo);

    return hasChanges;
}

static bool RepairJavaScriptContent(CourseManagementDb dbContext, Guid javascriptCourseId)
{
    var hasChanges = false;
    const string javaScriptBasicsVideo = "https://www.youtube.com/watch?v=PkZNo7MFNFg";
    const string javaScriptProgrammingVideo = "https://www.youtube.com/watch?v=jS4aFq5-91M";

    hasChanges |= RepairLesson(dbContext, javascriptCourseId, "Strict Mode trong JavaScript",
        "Strict Mode trong JavaScript",
        "Tim hieu che do nghiem ngat giup JavaScript bat loi ro rang hon.",
        javaScriptProgrammingVideo);

    hasChanges |= RepairLesson(dbContext, javascriptCourseId, "Test",
        "DOM la gi?",
        "Lam quen voi DOM va cach JavaScript tuong tac voi trinh duyet.",
        javaScriptBasicsVideo);

    hasChanges |= UpdateLessonVideo(dbContext, javascriptCourseId, "Gioi thieu Javascript", javaScriptBasicsVideo);
    hasChanges |= UpdateLessonVideo(dbContext, javascriptCourseId, "IDE la gi?", javaScriptProgrammingVideo);
    hasChanges |= UpdateLessonVideo(dbContext, javascriptCourseId, "Dev Tools la gi?", javaScriptProgrammingVideo);
    hasChanges |= UpdateLessonVideo(dbContext, javascriptCourseId, "Chuong trinh Javascript dau tien", javaScriptBasicsVideo);
    hasChanges |= UpdateLessonVideo(dbContext, javascriptCourseId, "Cau truc code trong Javascript", javaScriptBasicsVideo);
    hasChanges |= UpdateLessonVideo(dbContext, javascriptCourseId, "DOM la gi?", javaScriptBasicsVideo);
    hasChanges |= UpdateLessonVideo(dbContext, javascriptCourseId, "Object la gi?", javaScriptProgrammingVideo);
    hasChanges |= UpdateLessonVideo(dbContext, javascriptCourseId, "Array co ban", javaScriptProgrammingVideo);
    hasChanges |= UpdateLessonVideo(dbContext, javascriptCourseId, "let, const va arrow function", javaScriptProgrammingVideo);
    hasChanges |= UpdateLessonVideo(dbContext, javascriptCourseId, "Template string va destructuring", javaScriptProgrammingVideo);
    hasChanges |= UpdateLessonVideo(dbContext, javascriptCourseId, "Fetch API", javaScriptProgrammingVideo);
    hasChanges |= UpdateLessonVideo(dbContext, javascriptCourseId, "Xu ly loi khi goi API", javaScriptProgrammingVideo);
    hasChanges |= UpdateLessonVideo(dbContext, javascriptCourseId, "Xay dung mini project", javaScriptProgrammingVideo);

    hasChanges |= EnsureModuleLessons(dbContext, javascriptCourseId, "Object trong Javascript", new[]
    {
        CreateHistoricalLesson(0, "Object la gi?", "Tim hieu object, property va method trong JavaScript.", javaScriptProgrammingVideo, 1),
        CreateHistoricalLesson(0, "Array co ban", "Lam viec voi mang, them, sua, xoa va lap qua phan tu.", javaScriptProgrammingVideo, 2)
    });

    hasChanges |= RenameModule(dbContext, javascriptCourseId, "New", "DOM va Browser");

    hasChanges |= RenameModule(dbContext, javascriptCourseId, "new 2", "ES6 co ban");
    hasChanges |= EnsureModuleLessons(dbContext, javascriptCourseId, "ES6 co ban", new[]
    {
        CreateHistoricalLesson(0, "let, const va arrow function", "Nam cac cu phap ES6 hay dung trong du an JavaScript.", javaScriptProgrammingVideo, 1),
        CreateHistoricalLesson(0, "Template string va destructuring", "Viet code ngan gon hon voi template string va destructuring.", javaScriptProgrammingVideo, 2)
    });

    hasChanges |= RenameModule(dbContext, javascriptCourseId, "new 3", "Lam viec voi API");
    hasChanges |= EnsureModuleLessons(dbContext, javascriptCourseId, "Lam viec voi API", new[]
    {
        CreateHistoricalLesson(0, "Fetch API", "Goi API tu trinh duyet va xu ly du lieu JSON.", javaScriptProgrammingVideo, 1),
        CreateHistoricalLesson(0, "Xu ly loi khi goi API", "Kiem tra response va hien thi thong bao loi phu hop.", javaScriptProgrammingVideo, 2)
    });

    hasChanges |= RenameModule(dbContext, javascriptCourseId, "New 4", "Tong ket du an");
    hasChanges |= EnsureModuleLessons(dbContext, javascriptCourseId, "Tong ket du an", new[]
    {
        CreateHistoricalLesson(0, "Xay dung mini project", "Ghep cac kien thuc da hoc de tao mot ung dung JavaScript nho.", javaScriptProgrammingVideo, 1)
    });

    return hasChanges;
}

static bool UpdateLessonVideo(CourseManagementDb dbContext, Guid courseId, string title, string urlVideo)
{
    var lesson = dbContext.Lessons
        .Include(item => item.Module)
        .FirstOrDefault(item => item.Title == title && item.Module.CourseId == courseId);

    if (lesson == null || lesson.UrlVideo == urlVideo)
    {
        return false;
    }

    lesson.UrlVideo = urlVideo;
    return true;
}

static bool RepairLesson(CourseManagementDb dbContext, Guid courseId, string currentTitle, string newTitle, string description, string urlVideo)
{
    var lesson = dbContext.Lessons
        .Include(item => item.Module)
        .FirstOrDefault(item => item.Title == currentTitle && item.Module.CourseId == courseId);

    if (lesson == null)
    {
        return false;
    }

    var hasChanges = lesson.Title != newTitle
        || lesson.Description != description
        || lesson.UrlVideo != urlVideo;

    lesson.Title = newTitle;
    lesson.Description = description;
    lesson.UrlVideo = urlVideo;

    return hasChanges;
}

static bool RenameModule(CourseManagementDb dbContext, Guid courseId, string currentTitle, string newTitle)
{
    var module = dbContext.Modules.FirstOrDefault(item => item.CourseId == courseId && item.Title == currentTitle);
    if (module == null || module.Title == newTitle)
    {
        return false;
    }

    module.Title = newTitle;
    return true;
}

static bool EnsureModuleLessons(CourseManagementDb dbContext, Guid courseId, string moduleTitle, IEnumerable<Lesson> lessons)
{
    var module = dbContext.Modules
        .Include(item => item.Lessons)
        .FirstOrDefault(item => item.CourseId == courseId && item.Title == moduleTitle);

    if (module == null || module.Lessons.Any(item => item.Status == LessonStatus.Active))
    {
        return false;
    }

    foreach (var lesson in lessons)
    {
        module.Lessons.Add(lesson);
    }

    return true;
}

static IEnumerable<CourseManagement.Model.Model.Module> CreateJavaModules()
{
    return new[]
    {
        CreateHistoricalModule(0, "Java co ban", Guid.Empty, 1, new[]
        {
            CreateHistoricalLesson(0, "Gioi thieu Java", "Lam quen voi ngon ngu Java, JDK, JVM va cach chay chuong trinh dau tien.", "https://www.youtube.com/watch?v=l9AzO1FMgM8", 1),
            CreateHistoricalLesson(0, "Bien va kieu du lieu", "Tim hieu bien, kieu du lieu, toan tu va nhap xuat co ban trong Java.", "https://www.youtube.com/watch?v=eIrMbAQSU34", 2)
        }),
        CreateHistoricalModule(0, "Lap trinh huong doi tuong", Guid.Empty, 2, new[]
        {
            CreateHistoricalLesson(0, "Class va Object", "Nam cach tao lop, doi tuong, thuoc tinh va phuong thuc trong Java.", "https://www.youtube.com/watch?v=IUqKuGNasdM", 1),
            CreateHistoricalLesson(0, "Ke thua va da hinh", "Hoc cach tai su dung code bang ke thua, override va da hinh.", "https://www.youtube.com/watch?v=46T2wD3IuhM", 2)
        })
    };
}

static IEnumerable<CourseManagement.Model.Model.Module> CreateReactModules()
{
    return new[]
    {
        CreateHistoricalModule(0, "ReactJS co ban", Guid.Empty, 1, new[]
        {
            CreateHistoricalLesson(0, "Gioi thieu ReactJS", "Lam quen voi ReactJS, component va cach tao giao dien theo tung thanh phan.", "https://www.youtube.com/watch?v=Tn6-PIqc4UM", 1),
            CreateHistoricalLesson(0, "JSX va Component", "Tim hieu JSX, props va cach tach giao dien thanh cac component co the tai su dung.", "https://www.youtube.com/watch?v=SqcY0GlETPk", 2)
        }),
        CreateHistoricalModule(0, "State va su kien", Guid.Empty, 2, new[]
        {
            CreateHistoricalLesson(0, "useState", "Quan ly state trong function component bang hook useState.", "https://www.youtube.com/watch?v=O6P86uwfdR0", 1),
            CreateHistoricalLesson(0, "Xu ly event", "Bat su kien click, submit form va cap nhat giao dien theo hanh dong nguoi dung.", "https://www.youtube.com/watch?v=bMknfKXIFA8", 2)
        })
    };
}

static CourseManagement.Model.Model.Module CreateHistoricalModule(
    int id,
    string title,
    Guid courseId,
    int order,
    IEnumerable<Lesson> lessons)
{
    return new CourseManagement.Model.Model.Module
    {
        Title = title,
        Order = order,
        Status = ModuleStatus.Active,
        Lessons = lessons.ToList()
    };
}

static Lesson CreateHistoricalLesson(
    int id,
    string title,
    string description,
    string urlVideo,
    int order)
{
    return new Lesson
    {
        Title = title,
        Description = description,
        UrlVideo = urlVideo,
        Order = order,
        Status = LessonStatus.Active
    };
}

sealed class YoutubePlaylistSeed
{
    public string ListId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<YoutubeVideoSeed> Videos { get; set; } = new();
}

sealed class YoutubeVideoSeed
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string VideoId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
}

