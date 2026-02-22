using System.IO.Compression;
using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CronJobServices;
using RichardSzalay.MockHttp;
using Xunit;

namespace PreflightApi.Tests.ObstacleTests;

public class ObstacleDailyChangeCronServiceTests : IDisposable
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly ObstacleDailyChangeCronService _service;

    public ObstacleDailyChangeCronServiceTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new PreflightApiDbContext(options);
        _mockHttp = new MockHttpMessageHandler();

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => _mockHttp.ToHttpClient());

        var logger = Substitute.For<ILogger<ObstacleDailyChangeCronService>>();

        var telemetry = Substitute.For<ISyncTelemetryService>();
        _service = new ObstacleDailyChangeCronService(logger, httpClientFactory, _dbContext, telemetry);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    /// <summary>
    /// Builds a fixed-width DOF line with fields at the correct positions.
    /// </summary>
    private static string BuildDofLine(
        string oasCode = "06",
        string obstacleNumber = "123456",
        string verificationStatus = "O",
        string country = "US",
        string state = "TX",
        string city = "DALLAS",
        string latDeg = "32",
        string latMin = "53",
        string latSec = "48.50",
        string latHemi = "N",
        string lonDeg = "097",
        string lonMin = "02",
        string lonSec = "16.80",
        string lonHemi = "W",
        string type = "TOWER",
        string quantity = "1",
        string heightAgl = "1500",
        string heightAmsl = "2000",
        string lighting = "R",
        string hAccuracy = "1",
        string vAccuracy = "A",
        string mark = " ",
        string faaStudy = "2024-OE-12345",
        string action = "A",
        string julianDate = "2024001")
    {
        var sb = new StringBuilder(128);
        sb.Append(oasCode.PadRight(2));       // 0-1
        sb.Append(' ');                        // 2
        sb.Append(obstacleNumber.PadRight(6)); // 3-8
        sb.Append(' ');                        // 9
        sb.Append(verificationStatus.PadRight(1)); // 10
        sb.Append(' ');                        // 11
        sb.Append(country.PadRight(2));        // 12-13
        sb.Append(' ');                        // 14
        sb.Append(state.PadRight(2));          // 15-16
        sb.Append(' ');                        // 17
        sb.Append(city.PadRight(16));          // 18-33
        sb.Append(' ');                        // 34
        sb.Append(latDeg.PadLeft(2));          // 35-36
        sb.Append(' ');                        // 37
        sb.Append(latMin.PadLeft(2));          // 38-39
        sb.Append(' ');                        // 40
        sb.Append(latSec.PadLeft(5));          // 41-45
        sb.Append(latHemi);                    // 46
        sb.Append(' ');                        // 47
        sb.Append(lonDeg.PadLeft(3));          // 48-50
        sb.Append(' ');                        // 51
        sb.Append(lonMin.PadLeft(2));          // 52-53
        sb.Append(' ');                        // 54
        sb.Append(lonSec.PadLeft(5));          // 55-59
        sb.Append(lonHemi);                    // 60
        sb.Append(' ');                        // 61
        sb.Append(type.PadRight(18));          // 62-79
        sb.Append(' ');                        // 80
        sb.Append(quantity.PadLeft(1));        // 81
        sb.Append(' ');                        // 82
        sb.Append(heightAgl.PadLeft(5));       // 83-87
        sb.Append(' ');                        // 88
        sb.Append(heightAmsl.PadLeft(5));      // 89-93
        sb.Append(' ');                        // 94
        sb.Append(lighting.PadRight(1));       // 95
        sb.Append(' ');                        // 96
        sb.Append(hAccuracy.PadRight(1));      // 97
        sb.Append(' ');                        // 98
        sb.Append(vAccuracy.PadRight(1));      // 99
        sb.Append(' ');                        // 100
        sb.Append(mark.PadRight(1));           // 101
        sb.Append(' ');                        // 102
        sb.Append(faaStudy.PadRight(14));      // 103-116
        sb.Append(' ');                        // 117
        sb.Append(action.PadRight(1));         // 118
        sb.Append(' ');                        // 119
        sb.Append(julianDate.PadRight(7));     // 120-126
        return sb.ToString();
    }

    /// <summary>
    /// Builds a daily change file line by prepending a 10-char ACTION prefix to a DOF line.
    /// </summary>
    private static string BuildChangeLine(string actionPrefix, string dofLine)
    {
        return actionPrefix.PadRight(10) + dofLine;
    }

    /// <summary>
    /// Creates an in-memory ZIP containing DOF_DAILY_CHANGE_UPDATE.DAT with the given data lines
    /// (headers are prepended automatically).
    /// </summary>
    private static byte[] CreateChangeZip(params string[] dataLines)
    {
        var content = new StringBuilder();
        content.AppendLine("DAILY DOF CHANGE UPDATE");
        content.AppendLine("");
        content.AppendLine("ACTION    OAS  NUMBER  VS CTRY ST CITY             LAT        LON           TYPE              QTY  AGL   AMSL  LT HA VA MK FAA STUDY       ACT  JULIAN");
        content.AppendLine("--------- -- -------- -- -- -- ---------------- -- -- ------  --- -- ------ ----------------  - ----- ----- - - - - -------------- - -------");

        foreach (var line in dataLines)
        {
            content.AppendLine(line);
        }

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("DOF_DAILY_CHANGE_UPDATE.DAT");
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(content.ToString());
        }

        memoryStream.Position = 0;
        return memoryStream.ToArray();
    }

    private void MockHttpWithZip(byte[] zipBytes)
    {
        _mockHttp.When("https://aeronav.faa.gov/Obst_Data/DOF_DAILY_CHANGE_UPDATE.ZIP")
            .Respond(HttpStatusCode.OK, new ByteArrayContent(zipBytes));
    }

    [Fact]
    public async Task ProcessDailyChanges_AddAction_InsertsNewObstacle()
    {
        var dofLine = BuildDofLine(oasCode: "06", obstacleNumber: "000001");
        var changeLine = BuildChangeLine("ADD", dofLine);
        var zip = CreateChangeZip(changeLine);
        MockHttpWithZip(zip);

        await _service.ProcessDailyChangesAsync();

        var obstacle = await _dbContext.Obstacles.FirstOrDefaultAsync(o => o.OasNumber == "06-000001");
        obstacle.Should().NotBeNull();
        obstacle!.CityName.Should().Be("DALLAS");
        obstacle.HeightAgl.Should().Be(1500);
    }

    [Fact]
    public async Task ProcessDailyChanges_AddAction_ExistingObstacle_UpdatesInstead()
    {
        // Seed an existing obstacle
        _dbContext.Obstacles.Add(new Obstacle
        {
            OasNumber = "06-000001",
            OasCode = "06",
            ObstacleNumber = "000001",
            CityName = "OLD CITY",
            HeightAgl = 100
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var dofLine = BuildDofLine(oasCode: "06", obstacleNumber: "000001", city: "NEW CITY", heightAgl: "2000");
        var changeLine = BuildChangeLine("ADD", dofLine);
        var zip = CreateChangeZip(changeLine);
        MockHttpWithZip(zip);

        await _service.ProcessDailyChangesAsync();

        var obstacle = await _dbContext.Obstacles.FirstOrDefaultAsync(o => o.OasNumber == "06-000001");
        obstacle.Should().NotBeNull();
        obstacle!.CityName.Should().Be("NEW CITY");
        obstacle.HeightAgl.Should().Be(2000);
    }

    [Fact]
    public async Task ProcessDailyChanges_OldNewPair_UpdatesObstacle()
    {
        // Seed existing obstacle
        _dbContext.Obstacles.Add(new Obstacle
        {
            OasNumber = "06-000002",
            OasCode = "06",
            ObstacleNumber = "000002",
            CityName = "ORIGINAL",
            HeightAgl = 500
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var oldLine = BuildChangeLine("OLD", BuildDofLine(oasCode: "06", obstacleNumber: "000002", city: "ORIGINAL", heightAgl: "500"));
        var newLine = BuildChangeLine("NEW", BuildDofLine(oasCode: "06", obstacleNumber: "000002", city: "UPDATED", heightAgl: "750"));
        var zip = CreateChangeZip(oldLine, newLine);
        MockHttpWithZip(zip);

        await _service.ProcessDailyChangesAsync();

        var obstacle = await _dbContext.Obstacles.FirstOrDefaultAsync(o => o.OasNumber == "06-000002");
        obstacle.Should().NotBeNull();
        obstacle!.CityName.Should().Be("UPDATED");
        obstacle.HeightAgl.Should().Be(750);
    }

    [Fact]
    public async Task ProcessDailyChanges_OldAction_SkippedEntirely()
    {
        var oldLine = BuildChangeLine("OLD", BuildDofLine(oasCode: "06", obstacleNumber: "000099"));
        var zip = CreateChangeZip(oldLine);
        MockHttpWithZip(zip);

        await _service.ProcessDailyChangesAsync();

        // OLD lines should not insert anything
        var count = await _dbContext.Obstacles.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task ProcessDailyChanges_DismantleAction_DeletesObstacle()
    {
        // Seed existing obstacle
        _dbContext.Obstacles.Add(new Obstacle
        {
            OasNumber = "06-000003",
            OasCode = "06",
            ObstacleNumber = "000003",
            CityName = "TO DELETE"
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var dismantleLine = BuildChangeLine("DISMANTLE", BuildDofLine(oasCode: "06", obstacleNumber: "000003"));
        var zip = CreateChangeZip(dismantleLine);
        MockHttpWithZip(zip);

        await _service.ProcessDailyChangesAsync();

        var obstacle = await _dbContext.Obstacles.FirstOrDefaultAsync(o => o.OasNumber == "06-000003");
        obstacle.Should().BeNull();
    }

    [Fact]
    public async Task ProcessDailyChanges_DismantleAction_AlreadyGone_NoOp()
    {
        var dismantleLine = BuildChangeLine("DISMANTLE", BuildDofLine(oasCode: "06", obstacleNumber: "999999"));
        var zip = CreateChangeZip(dismantleLine);
        MockHttpWithZip(zip);

        // Should not throw
        await _service.ProcessDailyChangesAsync();

        var count = await _dbContext.Obstacles.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task ProcessDailyChanges_HeaderLinesSkipped()
    {
        // Only header lines, no data — should process with no changes
        var zip = CreateChangeZip(); // no data lines
        MockHttpWithZip(zip);

        await _service.ProcessDailyChangesAsync();

        var count = await _dbContext.Obstacles.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task ProcessDailyChanges_MixedActions_ProcessesAllCorrectly()
    {
        // Seed an obstacle to update and one to dismantle
        _dbContext.Obstacles.Add(new Obstacle
        {
            OasNumber = "06-000010",
            OasCode = "06",
            ObstacleNumber = "000010",
            CityName = "UPDATE ME",
            HeightAgl = 300
        });
        _dbContext.Obstacles.Add(new Obstacle
        {
            OasNumber = "06-000020",
            OasCode = "06",
            ObstacleNumber = "000020",
            CityName = "DELETE ME"
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var addLine = BuildChangeLine("ADD", BuildDofLine(oasCode: "06", obstacleNumber: "000030", city: "BRAND NEW"));
        var oldLine = BuildChangeLine("OLD", BuildDofLine(oasCode: "06", obstacleNumber: "000010", city: "UPDATE ME"));
        var newLine = BuildChangeLine("NEW", BuildDofLine(oasCode: "06", obstacleNumber: "000010", city: "UPDATED", heightAgl: "600"));
        var dismantleLine = BuildChangeLine("DISMANTLE", BuildDofLine(oasCode: "06", obstacleNumber: "000020"));
        var zip = CreateChangeZip(addLine, oldLine, newLine, dismantleLine);
        MockHttpWithZip(zip);

        await _service.ProcessDailyChangesAsync();

        // ADD: new obstacle inserted
        var added = await _dbContext.Obstacles.FirstOrDefaultAsync(o => o.OasNumber == "06-000030");
        added.Should().NotBeNull();
        added!.CityName.Should().Be("BRAND NEW");

        // NEW: existing obstacle updated
        var updated = await _dbContext.Obstacles.FirstOrDefaultAsync(o => o.OasNumber == "06-000010");
        updated.Should().NotBeNull();
        updated!.CityName.Should().Be("UPDATED");
        updated.HeightAgl.Should().Be(600);

        // DISMANTLE: obstacle removed
        var deleted = await _dbContext.Obstacles.FirstOrDefaultAsync(o => o.OasNumber == "06-000020");
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task ProcessDailyChanges_Idempotent_ReprocessingSameFile()
    {
        var dofLine = BuildDofLine(oasCode: "06", obstacleNumber: "000050", city: "IDEMPOTENT");
        var changeLine = BuildChangeLine("ADD", dofLine);
        var zip = CreateChangeZip(changeLine);

        // First run
        MockHttpWithZip(zip);
        await _service.ProcessDailyChangesAsync();

        // Second run — should upsert, not throw
        _mockHttp.Clear();
        MockHttpWithZip(zip);
        _dbContext.ChangeTracker.Clear();
        await _service.ProcessDailyChangesAsync();

        var obstacles = await _dbContext.Obstacles.Where(o => o.OasNumber == "06-000050").ToListAsync();
        obstacles.Should().HaveCount(1);
        obstacles[0].CityName.Should().Be("IDEMPOTENT");
    }

    [Fact]
    public async Task ProcessDailyChanges_NewAction_MissingObstacle_InsertsAnyway()
    {
        // NEW without a corresponding existing record should upsert (insert for safety)
        var newLine = BuildChangeLine("NEW", BuildDofLine(oasCode: "06", obstacleNumber: "000060", city: "ORPHAN NEW"));
        var zip = CreateChangeZip(newLine);
        MockHttpWithZip(zip);

        await _service.ProcessDailyChangesAsync();

        var obstacle = await _dbContext.Obstacles.FirstOrDefaultAsync(o => o.OasNumber == "06-000060");
        obstacle.Should().NotBeNull();
        obstacle!.CityName.Should().Be("ORPHAN NEW");
    }

    [Fact]
    public async Task ProcessDailyChanges_HttpError_Throws()
    {
        _mockHttp.When("https://aeronav.faa.gov/Obst_Data/DOF_DAILY_CHANGE_UPDATE.ZIP")
            .Respond(HttpStatusCode.InternalServerError);

        var act = () => _service.ProcessDailyChangesAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
