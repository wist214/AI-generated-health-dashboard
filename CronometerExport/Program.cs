using CronometerExport;

// Parse command line arguments
string? username = null;
string? password = null;
string outputDir = ".";
DateTime startDate = DateTime.Today.AddYears(-1);
DateTime endDate = DateTime.Today;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-u" or "--username":
            username = args[++i];
            break;
        case "-p" or "--password":
            password = args[++i];
            break;
        case "-o" or "--output":
            outputDir = args[++i];
            break;
        case "-s" or "--start":
            startDate = DateTime.Parse(args[++i]);
            break;
        case "-e" or "--end":
            endDate = DateTime.Parse(args[++i]);
            break;
        case "-h" or "--help":
            ShowHelp();
            return;
    }
}

// Prompt for credentials if not provided
if (string.IsNullOrEmpty(username))
{
    Console.Write("Enter Cronometer username/email: ");
    username = Console.ReadLine();
}

if (string.IsNullOrEmpty(password))
{
    Console.Write("Enter Cronometer password: ");
    password = ReadPassword();
    Console.WriteLine();
}

if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
{
    Console.Error.WriteLine("Username and password are required.");
    return;
}

// Create output directory
Directory.CreateDirectory(outputDir);

// Export data
using var client = new CronometerClient();

try
{
    Console.WriteLine("Connecting to Cronometer...");
    Console.WriteLine($"Logging in as {username}...");
    
    await client.LoginAsync(username, password);
    Console.WriteLine("Login successful!");

    Console.WriteLine();
    Console.WriteLine($"Exporting data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
    Console.WriteLine($"Output directory: {Path.GetFullPath(outputDir)}");
    Console.WriteLine();

    var exports = new (string Name, string Filename, Func<DateTime, DateTime, Task<string>> Export)[]
    {
        ("Daily Nutrition", "daily_nutrition.csv", client.ExportDailyNutritionAsync),
        ("Servings", "servings.csv", client.ExportServingsAsync),
        ("Exercises", "exercises.csv", client.ExportExercisesAsync),
        ("Biometrics", "biometrics.csv", client.ExportBiometricsAsync),
        ("Notes", "notes.csv", client.ExportNotesAsync)
    };

    int successCount = 0;
    foreach (var (name, filename, export) in exports)
    {
        Console.Write($"Exporting {name}...");
        try
        {
            var data = await export(startDate, endDate);
            var filePath = Path.Combine(outputDir, filename);
            await File.WriteAllTextAsync(filePath, data);
            Console.WriteLine($" OK ({data.Length} bytes)");
            successCount++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" FAILED: {ex.Message}");
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Export complete! {successCount}/{exports.Length} exports successful.");
    Console.WriteLine($"Files saved to: {Path.GetFullPath(outputDir)}");

    Console.WriteLine("Logging out...");
    await client.LogoutAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}

static string ReadPassword()
{
    var password = string.Empty;
    ConsoleKey key;
    do
    {
        var keyInfo = Console.ReadKey(intercept: true);
        key = keyInfo.Key;

        if (key == ConsoleKey.Backspace && password.Length > 0)
        {
            password = password[..^1];
            Console.Write("\b \b");
        }
        else if (!char.IsControl(keyInfo.KeyChar))
        {
            password += keyInfo.KeyChar;
            Console.Write("*");
        }
    } while (key != ConsoleKey.Enter);

    return password;
}

static void ShowHelp()
{
    Console.WriteLine("Cronometer Data Export Tool");
    Console.WriteLine();
    Console.WriteLine("Usage: CronometerExport [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -u, --username <email>    Cronometer username/email");
    Console.WriteLine("  -p, --password <pass>     Cronometer password");
    Console.WriteLine("  -o, --output <dir>        Output directory (default: current directory)");
    Console.WriteLine("  -s, --start <date>        Start date YYYY-MM-DD (default: 1 year ago)");
    Console.WriteLine("  -e, --end <date>          End date YYYY-MM-DD (default: today)");
    Console.WriteLine("  -h, --help                Show this help message");
}
