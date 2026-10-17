using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using TheReachScreensaver.Diagnostics;

namespace TheReachScreensaver.Persistence;

internal static class JourneyResetSelfTest
{
    private const string Host = "reach.example";
    private const string Url = "https://reach.example/index.html?reach=1";
    private const string PopulatedCheckpoint = """
        {"schema":1,"itinerary":{"id":"solar-system-41","version":1},"destinationId":"mars",
        "routeIndex":2,"phase":{"kind":"observe","elapsed":8,"duration":30},
        "simulation":{"jd":2460000,"rate":1},"frame":{"starId":"c0"},
        "odometer":{"completedKm":123456789,"roundTrips":3,"leg":null},
        "camera":{"position":[1000000,2000000,3000000],"orientation":[0,0,0,1],
        "referenceId":"mars","targetId":"mars","autoOrbit":0.012,"flight":null}}
        """;

    public static int Run()
    {
        if (!OperatingSystem.IsWindows()) return 1;
        var root = Path.Combine(Path.GetTempPath(), "ReachJourneyResetFixture-" + Guid.NewGuid().ToString("N"));
        Log.Initialize(root);
        var result = 1;
        try { CheckFreshCheckpointValidation(); }
        catch (Exception ex)
        {
            Log.Error("Reach journey checkpoint validation: FAIL", ex);
            return result;
        }
        System.Windows.Forms.Application.EnableVisualStyles();
        using (var form = new Form
        {
            Text = "Reach journey reset fixture",
            Size = new Size(800, 600),
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterScreen
        })
        {
            form.Shown += async (_, _) =>
            {
                try
                {
                    await RunFixture(form, root);
                    Log.Info("Reach journey reset fixture: PASS");
                    result = 0;
                }
                catch (Exception ex) { Log.Error("Reach journey reset fixture: FAIL", ex); }
                finally { form.Close(); }
            };
            System.Windows.Forms.Application.Run(form);
        }
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                var full = Path.GetFullPath(root);
                var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) && Directory.Exists(full))
                    Directory.Delete(full, true);
                break;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (attempt == 9) Log.Warn($"Could not remove temporary reset fixture: {ex.Message}");
                else Thread.Sleep(250);
            }
        }
        return result;
    }

    public static int RunCheckpointValidation()
    {
        try
        {
            CheckFreshCheckpointValidation();
            Console.WriteLine("Reach journey checkpoint validation: PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Reach journey checkpoint validation: FAIL {ex}");
            return 1;
        }
    }

    private static void CheckFreshCheckpointValidation()
    {
        static string Probe(string? checkpoint) => JsonSerializer.Serialize(JsonSerializer.Serialize(
            new { applied = true, checkpoint }));
        static string Fresh(int version, string destinationId = "earth", double completedKm = 0) =>
            JsonSerializer.Serialize(new
            {
                schema = 1,
                itinerary = new { id = "solar-system-41", version },
                destinationId,
                routeIndex = 0,
                phase = new { kind = "observe" },
                odometer = new { completedKm, roundTrips = 0, leg = (object?)null }
            });

        Check(JourneyReset.IsFreshCheckpoint(Probe(Fresh(4))), "fresh V4 checkpoint accepted");
        Check(JourneyReset.IsFreshCheckpoint(Probe(Fresh(5))), "fresh V5 checkpoint accepted");
        Check(JourneyReset.IsFreshCheckpoint(Probe(Fresh(6))), "fresh V6 checkpoint accepted");
        Check(JourneyReset.IsFreshCheckpoint(Probe(Fresh(7))), "fresh V7 checkpoint accepted");
        Check(!JourneyReset.IsFreshCheckpoint(Probe(Fresh(8))), "unsupported V8 checkpoint rejected");
        Check(!JourneyReset.IsFreshCheckpoint(Probe(Fresh(6, destinationId: "mars"))),
            "non-Earth V6 checkpoint rejected");
        Check(!JourneyReset.IsFreshCheckpoint(Probe(Fresh(6, completedKm: 1))),
            "nonzero V6 odometer rejected");
        Check(!JourneyReset.IsFreshCheckpoint(Probe(null)), "missing checkpoint rejected");
        Check(!JourneyReset.IsFreshCheckpoint(Probe("invalid")), "invalid checkpoint rejected");
    }

    private static async Task RunFixture(Form form, string root)
    {
        var dist = Environment.GetEnvironmentVariable("REACH_RESET_FIXTURE_DIST");
        if (string.IsNullOrWhiteSpace(dist))
        {
            dist = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Universe", "dist"));
            if (!File.Exists(Path.Combine(dist, "index.html")))
                dist = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "Universe", "dist"));
        }
        Check(File.Exists(Path.Combine(dist, "index.html")), "Universe dist is required for the reset fixture");

        Directory.CreateDirectory(root);
        var settings = Path.Combine(root, "settings.json");
        var asset = Path.Combine(root, "Assets", "earth", "image.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(asset)!);
        File.WriteAllText(settings, "{\"Mode\":\"Enhanced\",\"CountdownEnabled\":false}");
        File.WriteAllText(asset, "verified-fixture-asset");
        var settingsHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(settings)));
        var assetHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(asset)));

        var marker = Path.Combine(root, "pending-journey-reset.txt");
        Check(JourneyReset.PendingToken(root) is null && !File.Exists(marker), "cancelled reset leaves no marker");
        var heldName = "Local\\ReachJourneyResetFixtureHeld-" + Guid.NewGuid().ToString("N");
        using (var held = new Mutex(false, heldName))
        using (var acquired = new ManualResetEventSlim())
        using (var release = new ManualResetEventSlim())
        {
            var holder = Task.Run(() =>
            {
                held.WaitOne();
                acquired.Set();
                release.Wait();
                held.ReleaseMutex();
            });
            acquired.Wait();
            try
            {
                try { JourneyReset.Schedule(root, heldName); throw new Exception("Active fullscreen mutex was ignored."); }
                catch (InvalidOperationException) { }
                Check(!File.Exists(marker), "active fullscreen blocks reset without a marker");
            }
            finally { release.Set(); await holder; }
        }

        var site = Path.Combine(root, "seed-site");
        Directory.CreateDirectory(site);
        File.WriteAllText(Path.Combine(site, "index.html"), "<!doctype html><title>Reach reset fixture</title>");
        var profile = Path.Combine(root, "WebView2Fixture");
        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: profile,
            options: new CoreWebView2EnvironmentOptions("--no-proxy-server"));

        var seed = await NewView(form, environment, site);
        await Navigate(seed.CoreWebView2, "https://reach.example/index.html");
        var setupScript = "localStorage.setItem('" + JourneyReset.CheckpointKey + "', "
            + JsonSerializer.Serialize(PopulatedCheckpoint) + "); "
            + "localStorage.setItem('reach:test:preference', 'keep'); "
            + "localStorage.setItem('the-reach:journey:pilot-proxima:v4', 'pilot-keep'); true";
        Check(await seed.CoreWebView2.ExecuteScriptAsync(setupScript) == "true", "populated checkpoint seeded");
        RemoveView(form, seed);

        var oldVoyage = await NewView(form, environment, dist);
        await Navigate(oldVoyage.CoreWebView2, Url);
        await WaitForReady(oldVoyage.CoreWebView2);
        var old = await ReadCheckpoint(oldVoyage.CoreWebView2);
        Check(old.GetProperty("itinerary").GetProperty("version").GetInt32() == 7
            && old.GetProperty("destinationId").GetString() == "mars"
            && old.GetProperty("odometer").GetProperty("completedKm").GetDouble() == 123456789
            && old.GetProperty("odometer").GetProperty("roundTrips").GetInt32() == 3,
            "Universe restores populated voyage, distance, and round trips");
        RemoveView(form, oldVoyage);

        var mutexName = "Local\\ReachJourneyResetFixture-" + Guid.NewGuid().ToString("N");
        JourneyReset.Schedule(root, mutexName);
        var token = JourneyReset.PendingToken(root);
        Check(token is not null && File.Exists(marker), "reset request survives process boundary");
        var resetVoyage = await NewView(form, environment, dist);
        await resetVoyage.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(JourneyReset.RemovalScript);
        await Navigate(resetVoyage.CoreWebView2, Url);
        var fresh = false;
        for (var attempt = 0; attempt < 80; attempt++)
        {
            if (JourneyReset.IsFreshCheckpoint(await resetVoyage.CoreWebView2.ExecuteScriptAsync(JourneyReset.ProbeScript)))
            {
                fresh = true;
                break;
            }
            await Task.Delay(250);
        }
        Check(fresh, "reset starts a fresh Earth voyage with zero odometer and round trips");
        var freshCheckpoint = await ReadCheckpoint(resetVoyage.CoreWebView2);
        Check(freshCheckpoint.GetProperty("itinerary").GetProperty("version").GetInt32() == 7,
            "reset confirms a fresh V7 itinerary");
        Check(JourneyReset.PendingToken(root) == token, "marker remains until fresh checkpoint confirmation");
        Check(JourneyReset.Complete(token!, root) && JourneyReset.PendingToken(root) is null,
            "marker cleared only after fresh checkpoint");
        var otherKeys = await resetVoyage.CoreWebView2.ExecuteScriptAsync(
            "JSON.stringify([localStorage.getItem('reach:test:preference'), "
            + "localStorage.getItem('the-reach:journey:pilot-proxima:v4')])");
        Check(JsonSerializer.Deserialize<string>(otherKeys) == "[\"keep\",\"pilot-keep\"]",
            "unrelated localStorage keys remain unchanged");
        RemoveView(form, resetVoyage);

        var restarted = await NewView(form, environment, dist);
        await Navigate(restarted.CoreWebView2, Url);
        await WaitForReady(restarted.CoreWebView2);
        var afterRestart = await ReadCheckpoint(restarted.CoreWebView2);
        Check(afterRestart.GetProperty("destinationId").GetString() == "earth"
            && afterRestart.GetProperty("routeIndex").GetInt32() == 0
            && afterRestart.GetProperty("odometer").GetProperty("completedKm").GetDouble() == 0
            && afterRestart.GetProperty("odometer").GetProperty("roundTrips").GetInt32() == 0,
            "fresh voyage persists through restart without stale restoration");
        RemoveView(form, restarted);

        Check(settingsHash == Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(settings)))
            && assetHash == Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(asset))),
            "imagery and preferences remain unchanged");
    }

    private static async Task<WebView2> NewView(Form form, CoreWebView2Environment environment, string contentRoot)
    {
        var view = new WebView2 { Dock = DockStyle.Fill };
        form.Controls.Add(view);
        await view.EnsureCoreWebView2Async(environment);
        view.CoreWebView2.SetVirtualHostNameToFolderMapping(Host, contentRoot,
            CoreWebView2HostResourceAccessKind.DenyCors);
        return view;
    }

    private static void RemoveView(Form form, WebView2 view)
    {
        form.Controls.Remove(view);
        view.Dispose();
    }

    private static async Task Navigate(CoreWebView2 core, string url)
    {
        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<CoreWebView2NavigationCompletedEventArgs>? handler = null;
        handler = (_, args) =>
        {
            core.NavigationCompleted -= handler;
            if (args.IsSuccess) done.TrySetResult(true);
            else done.TrySetException(new InvalidOperationException($"Fixture navigation failed: {args.WebErrorStatus}"));
        };
        core.NavigationCompleted += handler;
        core.Navigate(url);
        await done.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    private static async Task WaitForReady(CoreWebView2 core)
    {
        for (var attempt = 0; attempt < 80; attempt++)
        {
            if (await core.ExecuteScriptAsync("document.getElementById('loader')?.classList.contains('done') === true") == "true")
                return;
            await Task.Delay(250);
        }
        throw new TimeoutException("Universe did not finish loading in the reset fixture.");
    }

    private static async Task<JsonElement> ReadCheckpoint(CoreWebView2 core)
    {
        var scriptResult = await core.ExecuteScriptAsync("localStorage.getItem('" + JourneyReset.CheckpointKey + "')");
        var checkpoint = JsonSerializer.Deserialize<string>(scriptResult);
        Check(checkpoint is not null, "Universe checkpoint exists");
        using var document = JsonDocument.Parse(checkpoint!);
        return document.RootElement.Clone();
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
    }
}
