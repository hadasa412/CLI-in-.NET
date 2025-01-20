
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

var excludedFolders = new[] {
    "\\bin\\", "\\obj\\", "\\node_modules\\", "\\build\\", "\\dist\\", "\\out\\", "\\temp\\", "\\tmp\\", "\\coverage\\", "\\.git\\", "\\.idea\\", "\\.vscode\\"
};

var fileExtensions = new Dictionary<string, string[]>
{
    { "C#", new[] { ".cs" } },
    { "ANGULAR", new[] { ".scss", ".html", ".ts", ".css", ".json" } },
    { "REACT", new[] { ".js", ".jsx", ".ts", ".tsx" } },
    { "C++", new[] { ".cpp", ".h" } },
    { "DARK", new[] { ".dark" } },
    { "JAVA", new[] { ".java" } },
    { "PYTHON", new[] { ".py" } },
    { "JAVASCRIPT", new[] { ".js" } },
    { "TYPESCRIPT", new[] { ".ts" } },
    { "RUBY", new[] { ".rb" } },
    { "GO", new[] { ".go" } },
    { "PHP", new[] { ".php" } },
    { "HTML", new[] { ".html" } },
    { "CSS", new[] { ".css" } },
    { "SQL", new[] { ".sql" } },
    { "JSON", new[] { ".json" } },
    { "XML", new[] { ".xml" } },
    { "YAML", new[] { ".yaml" } },
};

// פקודת bundle
var bundleCommand = new Command("bundle", "Bundle code files from a directory to a single file");

var outputOption = new Option<FileInfo>("--output", "The output file path and name") { IsRequired = true };
outputOption.AddAlias("-o");
bundleCommand.AddOption(outputOption);

var languageOption = new Option<string>("--language", "The programming language(s) to select files from") { IsRequired = true };
languageOption.AddAlias("-l");
bundleCommand.AddOption(languageOption);

var noteOption = new Option<bool>("--note", "Add the source file path as a comment in the bundle file");
noteOption.AddAlias("-n");
bundleCommand.AddOption(noteOption);

var sortOption = new Option<bool>("--sort", "Sort files by 'name' or 'extension' (default: 'extension')");
sortOption.AddAlias("-s");
bundleCommand.AddOption(sortOption);

var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from the source code before adding it to the bundle");
removeEmptyLinesOption.AddAlias("-r");
bundleCommand.AddOption(removeEmptyLinesOption);

var authorOption = new Option<string>("--author", "Name of the author to add as a comment in the bundle file");
authorOption.AddAlias("-a");
bundleCommand.AddOption(authorOption);

bundleCommand.SetHandler((FileInfo output, string language, bool note, bool sort, bool removeEmptyLines, string author) =>
{
    IEnumerable<string> allFiles;

    if (language.ToLower() == "all")
    {
        allFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories)
            .Where(file => !excludedFolders.Any(excludedFolder => file.Contains(excludedFolder, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }
    else
    {
        var selectedExtensions = new List<string>();
        foreach (var lang in language.Split(',').Select(l => l.Trim().ToUpper()))
        {
            if (fileExtensions.ContainsKey(lang))
            {
                selectedExtensions.AddRange(fileExtensions[lang]);
            }
        }

        if (!selectedExtensions.Any())
        {
            Console.WriteLine("There are no files matching the selected language in the folder.");
            return;
        }

        allFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories)
            .Where(file => !excludedFolders.Any(folder => (Path.GetDirectoryName(file).Contains(folder, StringComparison.OrdinalIgnoreCase))) &&
                           selectedExtensions.Contains(Path.GetExtension(file).ToLower()))
            .ToArray();
    }

    // מיון הקבצים לפי שם הקובץ או לפי הסיומת
    if (sort)
    {
        allFiles = allFiles.OrderBy(item => Path.GetExtension(item)).ToArray();
    }
    else
    {
        allFiles = allFiles.OrderBy(item => Path.GetFileName(item)).ToArray();
    }

    try
    {
        string bundleFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"bundle.txt");

        using (StreamWriter writer = new StreamWriter(bundleFilePath, append: false))
        {
            foreach (var file in allFiles)
            {
                if (note)
                {
                    writer.WriteLine($"// {file}");
                }

                string content = File.ReadAllText(file, Encoding.UTF8);


                if (removeEmptyLines)
                {
                    content = RemoveEmptyLines(content);
                }

                writer.WriteLine(content);
            }
        }

        Console.WriteLine($"Successfully bundled files into {output.FullName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}, outputOption, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

string RemoveEmptyLines(string content)
{
    var nonEmptyLines = content.Split('\n')
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .ToList();
    return string.Join("\n", nonEmptyLines);
}

// פקודת create-rsp
var createRspCommand = new Command("create-rsp", "Create an RSP file with the bundled command");


createRspCommand.SetHandler(() =>
{
    Console.WriteLine("Enter the path for the RSP file to save: (default: response.txt)");
    string rspFilePath = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(rspFilePath))
    {
        rspFilePath = Path.Combine(Directory.GetCurrentDirectory(), "response.txt");
    }

    // Make sure rspFilePath points to a file, not a directory
    if (Directory.Exists(rspFilePath) || rspFilePath.EndsWith("\\") || rspFilePath.EndsWith("/"))
    {
        rspFilePath = Path.Combine(rspFilePath, "response.txt");
    }

    string directoryPath = Path.GetDirectoryName(rspFilePath);
    if (!Directory.Exists(directoryPath))
    {
        Console.WriteLine($"Directory '{directoryPath}' does not exist. Creating directory...");
        try
        {
            Directory.CreateDirectory(directoryPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating directory: {ex.Message}");
            return;
        }
    }

    // בודקים אם הקלט על השפות תקין
    string languages = "";
    while (true)
    {
        Console.WriteLine("Enter language(s) (comma separated, e.g. 'c#,java' or 'all'): ");
        languages = Console.ReadLine()?.Trim();

        // המרת השפות לאותיות גדולות
        string languagesString = string.Join(",", languages.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                             .Select(lang => lang.Trim().ToUpper()));

        // אם המשתמש בחר ב-"all", כל השפות תקינות
        if (languagesString.Contains("ALL"))
        {
            break; // יוצאים מהלולאה אם בחר "all"
        }

        // בדיקת תקינות השפות
        if (!ValidateLanguages(languagesString, fileExtensions))
        {
            Console.WriteLine("Invalid language input. Please enter valid languages or 'all'.");
        }
        else
        {
            break; // הקלט תקין, יוצאים מהלולאה
        }
    }

    try
    {
        using (var writer = new StreamWriter(rspFilePath))
        {
            writer.Write("bundle");
            string bundleFilePath = Path.Combine(Directory.GetCurrentDirectory(), "bundle.txt");
            writer.Write($" --output \"{bundleFilePath}\"");

            writer.Write($" --language {languages}");

            Console.WriteLine("Add file paths as comments? (Y/N):");
            string addNotes = Console.ReadLine()?.Trim().ToUpper();
            if (addNotes == "Y")
            {
                writer.Write(" --note");
            }

            Console.WriteLine("Sort files by name ? (Y/N) (default extension):");
            string sortOption = Console.ReadLine()?.Trim().ToUpper();
            if (sortOption == "Y")
            {
                writer.Write(" --sort");
            }

            Console.WriteLine("Remove empty lines? (Y/N):");
            string removeEmptyLines = Console.ReadLine()?.Trim().ToUpper();
            if (removeEmptyLines == "Y")
            {
                writer.Write(" --remove-empty-lines");
            }

            Console.WriteLine("Enter author name (optional, press Enter to skip):");
            string author = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(author))
            {
                writer.Write($" --author {author}");
            }
        }

        Console.WriteLine($"RSP file created at {rspFilePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
});


// פונקציה לבדיקת תקינות השפות שהוזנו
// להוסיף פונקציה שתבצע את בדיקת השפות
static bool ValidateLanguages(string languages, Dictionary<string, string[]> fileExtensions)
{
    var languageList = languages.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(lang => lang.Trim().ToUpper())
                                 .ToList();

    // אם המשתמש בחר ב-"all", כל השפות תקינות
    if (languageList.Contains("ALL"))
    {
        return true;
    }

    // בודקים כל שפה שהוזנה
    foreach (var lang in languageList)
    {
        if (!fileExtensions.ContainsKey(lang))
        {
            Console.WriteLine($"Error: Language '{lang}' is not supported.");
            return false; // אם אין את השפה במילון, מחזירים false
        }
    }
    return true; // אם כל השפות תקינות, מחזירים true
}

var rootCommand = new RootCommand("File Bundle CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);
rootCommand.InvokeAsync(args);


