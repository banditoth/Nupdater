using System;
using System.IO;
using System.Linq;
using System.Xml;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NuGetPackageUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: NuGetPackageUpdater <path_to_csproj> [--include-pre-release]");
                return;
            }

            string csprojFilePath = args[0];
            bool includePreRelease = args.Length >= 2 && args[1] == "--include-pre-release";

            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(csprojFilePath);

                XmlNodeList packageReferenceNodes = xmlDocument.SelectNodes("//ItemGroup/PackageReference");

                foreach (XmlNode packageReferenceNode in packageReferenceNodes)
                {
                    string packageName = packageReferenceNode.Attributes["Include"].Value;
                    string currentVersion = packageReferenceNode.Attributes["Version"].Value;

                    var versionRange = new VersionRange(NuGetVersion.Parse(currentVersion), includePreRelease);

                    // Replace with your logic to fetch latest package version from NuGet repository

                    NuGetVersion latestVersion = FetchLatestPackageVersion(packageName, includePreRelease).Result;

                    if (latestVersion != null)
                    {
                        if (versionRange.Satisfies(latestVersion))
                        {
                            Console.WriteLine($"Updating '{packageName}' to version {latestVersion}.");
                            packageReferenceNode.Attributes["Version"].Value = latestVersion.ToNormalizedString();
                        }
                        else
                        {
                            Console.WriteLine($"Package '{packageName}' is up to date.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Package '{packageName}' not found in the NuGet repository.");
                    }
                }

                xmlDocument.Save(csprojFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        // Replace with your logic to fetch latest package version from NuGet repository
        static async Task<NuGetVersion> FetchLatestPackageVersion(string packageName, bool includePreRelease)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>();

            IEnumerable<IPackageSearchMetadata> packages = await resource.GetMetadataAsync(
                packageName,
                includePrerelease: includePreRelease,
                includeUnlisted: false,
                cache,
                logger,
                cancellationToken);

            if (packages.Any())
            {
                NuGetVersion latestVersion = packages.Max(p => p.Identity.Version);
                return latestVersion;
            }

            return null; // Package not found
        }
    }
}
