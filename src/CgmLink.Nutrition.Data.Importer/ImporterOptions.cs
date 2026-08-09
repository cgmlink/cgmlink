using Microsoft.Extensions.Configuration;

namespace CgmLink.Nutrition.Data.Importer;

public enum ImportMode
{
    Rebuild,
    Backfill
}

public sealed record ImporterOptions(
    string Path,
    ImportMode Mode,
    bool OverwriteMissingImages,
    int BatchSize)
{
    private const int DefaultBatchSize = 10000;

    public static ImporterOptions Parse(string[] args, IConfiguration configuration)
    {
        string? path = configuration["OpenFoodFacts:Path"];
        var mode = ImportMode.Rebuild;
        var overwriteMissingImages = false;
        var batchSize = DefaultBatchSize;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--path":
                    path = GetValue(args, ++i, "--path");
                    break;
                case "--mode":
                    mode = Enum.Parse<ImportMode>(GetValue(args, ++i, "--mode"), ignoreCase: true);
                    break;
                case "--overwrite-missing-images":
                    overwriteMissingImages = true;
                    break;
                case "--batch-size":
                    batchSize = int.Parse(GetValue(args, ++i, "--batch-size"));
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Open Food Facts import path must be provided via --path or OpenFoodFacts:Path configuration.");
        }

        if (batchSize <= 0)
        {
            throw new InvalidOperationException("Batch size must be greater than zero.");
        }

        return new ImporterOptions(path, mode, overwriteMissingImages, batchSize);
    }

    private static string GetValue(string[] args, int index, string optionName)
    {
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new InvalidOperationException($"{optionName} requires a value.");
        }

        return args[index];
    }
}
