using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace BundleReplacer
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("BundleReplacer - AssetBundle manipulation tool");

            // 导出命令
            var exportCommand = new Command("export", "Export assets from bundle");
            var exportPathOption = new Option<string>(new[] { "-p", "--path" }, "AssetBundle file path") { IsRequired = true };
            var exportOutputOption = new Option<string>(new[] { "-o", "--output" }, "Output directory") { IsRequired = true };
            var exportFilterOption = new Option<string>(new[] { "-f", "--filter" }, "Filter by type names (comma separated)");
            var exportClassIdOption = new Option<string>(new[] { "-c", "--classid" }, "Filter by AssetClassID (comma separated)");
            
            exportCommand.AddOption(exportPathOption);
            exportCommand.AddOption(exportOutputOption);
            exportCommand.AddOption(exportFilterOption);
            exportCommand.AddOption(exportClassIdOption);
            exportCommand.SetHandler(ExportAssets, exportPathOption, exportOutputOption, exportFilterOption, exportClassIdOption);

            // 导入命令
            var importCommand = new Command("import", "Import assets to bundle");
            var importPathOption = new Option<string>(new[] { "-p", "--path" }, "AssetBundle file path") { IsRequired = true };
            var importReplaceOption = new Option<string>(new[] { "-r", "--replace" }, "Replace files directory") { IsRequired = true };
            var importOutputOption = new Option<string>(new[] { "-o", "--output" }, "Output file path") { IsRequired = true };
            var importFilterOption = new Option<string>(new[] { "-f", "--filter" }, "Filter by type names (comma separated)");
            var importClassIdOption = new Option<string>(new[] { "-c", "--classid" }, "Filter by AssetClassID (comma separated)");
            
            importCommand.AddOption(importPathOption);
            importCommand.AddOption(importReplaceOption);
            importCommand.AddOption(importOutputOption);
            importCommand.AddOption(importFilterOption);
            importCommand.AddOption(importClassIdOption);
            importCommand.SetHandler(ImportAssets, importPathOption, importReplaceOption, importOutputOption, importFilterOption, importClassIdOption);

            rootCommand.AddCommand(exportCommand);
            rootCommand.AddCommand(importCommand);

            return await rootCommand.InvokeAsync(args);
        }

        static void ExportAssets(string bundlePath, string outputDir, string filter, string classIdFilter)
        {
            try
            {
                var typeFilters = !string.IsNullOrEmpty(filter) ? 
                    filter.Split(',').Select(f => f.Trim()).ToHashSet() : null;
                
                var classIdFilters = !string.IsNullOrEmpty(classIdFilter) ?
                    classIdFilter.Split(',').Select(f => int.TryParse(f.Trim(), out var id) ? id : -1)
                    .Where(id => id != -1).ToHashSet() : null;

                Directory.CreateDirectory(outputDir);

                var am = new AssetsManager();
                var bundleInst = am.LoadBundleFile(bundlePath);
                var assets = am.LoadAssetsFileFromBundle(bundleInst, 0);

                var assetInfos = assets.file.GetAssetsOfType(AssetClassID.MonoBehaviour)
                    .Concat(assets.file.GetAssetsOfType(AssetClassID.TextAsset))
                    .Concat(assets.file.GetAssetsOfType(AssetClassID.Texture2D));

                foreach (var assetInfo in assetInfos)
                {
                    if (ShouldProcess(assetInfo, typeFilters, classIdFilters))
                    {
                        string typeName = GetTypeName(assetInfo.TypeId);
                        ExportAsset(am, assets, assetInfo, outputDir, typeName);
                    }
                }

                Console.WriteLine($"Export completed to {outputDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export failed: {ex.Message}");
            }
        }

        static string GetTypeName(int typeId)
        {
            return typeId switch
            {
                (int)AssetClassID.MonoBehaviour => "MonoBehaviour",
                (int)AssetClassID.TextAsset => "TextAsset",
                (int)AssetClassID.Texture2D => "Texture2D",
                _ => "Unknown"
            };
        }

        static bool ShouldProcess(AssetFileInfo assetInfo, HashSet<string> typeFilters, HashSet<int> classIdFilters)
        {
            if ((typeFilters == null || typeFilters.Count == 0) && 
                (classIdFilters == null || classIdFilters.Count == 0))
                return true;

            bool typeMatch = typeFilters == null || typeFilters.Count == 0 || 
                           typeFilters.Contains(assetInfo.TypeId.ToString());

            bool classIdMatch = classIdFilters == null || classIdFilters.Count == 0 || 
                              classIdFilters.Contains(assetInfo.TypeId);

            return typeMatch && classIdMatch;
        }

        static void ExportAsset(AssetsManager am, AssetsFileInstance assets, AssetFileInfo assetInfo, string outputDir, string typeName)
        {
            try
            {
                var baseField = am.GetBaseField(assets, assetInfo);
                var assetName = baseField.Get("m_Name").AsString;
                
                if (string.IsNullOrEmpty(assetName))
                    assetName = $"unnamed_{assetInfo.PathId}";

                var safeName = MakeSafeFileName(assetName);
                var outputPath = Path.Combine(outputDir,$"{safeName}_{assetInfo.PathId}.{typeName.ToLower()}");

                var assetData = baseField.WriteToByteArray();
                File.WriteAllBytes(outputPath, assetData);
                Console.WriteLine($"Exported: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to export asset {assetInfo.PathId}: {ex.Message}");
            }
        }

        static void ImportAssets(string bundlePath, string replaceDir, string outputPath, string filter, string classIdFilter)
        {
            try
            {
                var typeFilters = !string.IsNullOrEmpty(filter) ? 
                    filter.Split(',').Select(f => f.Trim()).ToHashSet() : null;
                
                var classIdFilters = !string.IsNullOrEmpty(classIdFilter) ?
                    classIdFilter.Split(',').Select(f => int.TryParse(f.Trim(), out var id) ? id : -1)
                    .Where(id => id != -1).ToHashSet() : null;

                var am = new AssetsManager();
                var bundleInst = am.LoadBundleFile(bundlePath);
                var assets = am.LoadAssetsFileFromBundle(bundleInst, 0);

                var replaceFiles = Directory.GetFiles(replaceDir, "*.*");
                var replacedCount = 0;

                foreach (var replaceFile in replaceFiles)
                {
                    if (TryReplaceAsset(am, assets, replaceFile, typeFilters, classIdFilters))
                    {
                        replacedCount++;
                    }
                }

                if (replacedCount > 0)
                {
                    using (var stream = File.OpenWrite(outputPath))
                    using (var writer = new AssetsFileWriter(stream))
                    {
                        assets.file.Write(writer);
                    }
                    Console.WriteLine($"Replaced {replacedCount} assets, saved to {outputPath}");
                }
                else
                {
                    Console.WriteLine("No assets were replaced");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Import failed: {ex.Message}");
            }
        }

        static bool TryReplaceAsset(AssetsManager am, AssetsFileInstance assets, string filePath, 
                                  HashSet<string> typeFilters, HashSet<int> classIdFilters)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                if (fileName.Contains('_') && long.TryParse(fileName.Split('_').Last(), out var pathId))
                {
                    var assetInfo = assets.file.GetAssetInfo(pathId);
                    if (assetInfo != null && ShouldProcess(assetInfo, typeFilters, classIdFilters))
                    {
                        var newData = File.ReadAllBytes(filePath);
                        
                        // 直接使用AssetsFileReader读取数据并设置到资产信息
                        var byteArray = newData;
                        assetInfo.SetNewData(byteArray);
                        
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to replace file {filePath}: {ex.Message}");
            }
            return false;
        }

        static string MakeSafeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        }
    }
}
