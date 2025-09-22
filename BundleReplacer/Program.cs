using AssetsTools.NET;
using AssetsTools.NET.Extra;
using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BundleReplacer
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(options =>
                {
                    try
                    {
                        ProcessCommand(options);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"错误: {ex.Message}");
                    }
                });
        }

        private static void ProcessCommand(CommandLineOptions options)
        {
            switch (options.Command?.ToLower())
            {
                case "export":
                    ExportAssets(options.InputPath, options.OutputPath, options.Filters);
                    break;
                    
                case "import":
                    ImportAssets(options.InputPath, options.ReplacePath, options.OutputPath, options.Filters);
                    break;
                    
                case "export-bundle":
                    ExportBundle(options.InputPath, options.OutputPath, options.Filters, options.ConvertToReadable);
                    break;
                    
                case "import-bundle":
                    ImportToBundle(options.InputPath, options.ReplacePath, options.OutputPath, options.Filters);
                    break;
                    
                default:
                    Console.WriteLine("支持的命令: export, import, export-bundle, import-bundle");
                    Console.WriteLine("使用 'help' 查看详细用法");
                    break;
            }
        }

        #region Bundle 处理功能
        private static void ExportBundle(string bundlePath, string outputDir, string[] filters, bool convert = false)
        {
            if (!File.Exists(bundlePath))
                throw new FileNotFoundException($"Bundle 文件不存在: {bundlePath}");

            Directory.CreateDirectory(outputDir);

            using (var stream = File.OpenRead(bundlePath))
            using (var reader = new AssetsFileReader(stream))
            {
                var bundleFile = new AssetBundleFile();
                bundleFile.Read(reader);

                Console.WriteLine($"正在导出 Bundle: {Path.GetFileName(bundlePath)}");
                Console.WriteLine($"包含 {bundleFile.Files.Count} 个文件");

                // 导出每个文件
                foreach (var file in bundleFile.Files)
                {
                    var fileName = file.Name;
                    var fileExtension = Path.GetExtension(fileName).ToLower();

                    if (ShouldProcessFile(fileExtension, filters))
                    {
                        var outputFilePath = Path.Combine(outputDir, fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                        using (var fileStream = File.Create(outputFilePath))
                        using (var fileData = file.GetDataStream())
                        {
                            fileData.CopyTo(fileStream);
                        }

                        Console.WriteLine($"已导出: {fileName}");

                        // 如果是 .assets 文件，进一步处理其中的资产
                        if (fileExtension == ".assets" || fileExtension == ".bundle")
                        {
                            ProcessAssetsFile(outputFilePath, outputDir, filters, convert);
                        }
                    }
                }
            }

            Console.WriteLine($"Bundle 导出完成: {outputDir}");
        }

        private static void ImportToBundle(string bundlePath, string replaceDir, string outputPath, string[] filters)
        {
            if (!File.Exists(bundlePath))
                throw new FileNotFoundException($"Bundle 文件不存在: {bundlePath}");

            if (!Directory.Exists(replaceDir))
                throw new DirectoryNotFoundException($"替换文件目录不存在: {replaceDir}");

            // 加载原始 Bundle
            AssetBundleFile originalBundle;
            using (var stream = File.OpenRead(bundlePath))
            using (var reader = new AssetsFileReader(stream))
            {
                originalBundle = new AssetBundleFile();
                originalBundle.Read(reader);
            }

            var newBundle = new AssetBundleFile();

            // 处理每个文件
            foreach (var file in originalBundle.Files)
            {
                var fileName = file.Name;
                var fileExtension = Path.GetExtension(fileName).ToLower();
                var replaceFilePath = Path.Combine(replaceDir, fileName);

                if (File.Exists(replaceFilePath) && ShouldProcessFile(fileExtension, filters))
                {
                    // 使用替换文件
                    using (var replaceStream = File.OpenRead(replaceFilePath))
                    {
                        var newFile = new AssetBundleFile.FileEntry
                        {
                            Name = file.Name,
                            Flags = file.Flags
                        };
                        newFile.SetData(replaceStream);
                        newBundle.Files.Add(newFile);
                    }
                    Console.WriteLine($"已替换: {fileName}");
                }
                else
                {
                    // 保留原文件
                    newBundle.Files.Add(file);
                }
            }

            // 保存新的 Bundle
            using (var outputStream = File.Create(outputPath))
            using (var writer = new AssetsFileWriter(outputStream))
            {
                newBundle.Write(writer);
            }

            Console.WriteLine($"新的 Bundle 已保存: {outputPath}");
        }
        #endregion

        #region 辅助方法
        private static bool ShouldProcessFile(string fileExtension, string[] filters)
        {
            if (filters == null || filters.Length == 0)
                return true;

            return filters.Any(filter => 
                fileExtension.Equals($".{filter.ToLower()}") || 
                fileExtension.StartsWith($".{filter.ToLower()}."));
        }

        private static void ProcessAssetsFile(string assetsPath, string outputDir, string[] filters, bool convert)
        {
            // 这里可以添加处理 .assets 文件中具体资产的逻辑
            // 使用现有的 ExportAssets/ImportAssets 方法
        }

        // 现有的资产导出导入方法（保持原有功能）
        private static void ExportAssets(string inputPath, string outputDir, string[] filters)
        {
            // 原有的导出逻辑
            Console.WriteLine("执行资产导出...");
        }

        private static void ImportAssets(string inputPath, string replacePath, string outputPath, string[] filters)
        {
            // 原有的导入逻辑
            Console.WriteLine("执行资产导入...");
        }
        #endregion
    }

    // 扩展命令行选项类（在同一文件中）
    public class CommandLineOptions
    {
        [Option('c', "command", Required = true, HelpText = "执行命令: export, import, export-bundle, import-bundle")]
        public string Command { get; set; }

        [Option('p', "path", Required = true, HelpText = "输入文件路径")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "输出路径")]
        public string OutputPath { get; set; }

        [Option('r', "replace", HelpText = "替换文件目录")]
        public string ReplacePath { get; set; }

        [Option('f', "filter", HelpText = "过滤类型 (逗号分隔)")]
        public string Filter { get; set; }

        [Option('c', "convert", HelpText = "转换为可读格式", Default = false)]
        public bool ConvertToReadable { get; set; }

        public string[] Filters
        {
            get
            {
                return string.IsNullOrEmpty(Filter) ? 
                    Array.Empty<string>() : 
                    Filter.Split(',').Select(f => f.Trim()).ToArray();
            }
        }
    }
}
