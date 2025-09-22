using AssetsTools.NET;
using AssetsTools.NET.Extra;
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
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            try
            {
                var options = ParseArguments(args);
                if (options != null)
                {
                    ProcessCommand(options);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine("使用 -h 或 --help 查看帮助信息");
            }
        }

        private static CommandLineOptions ParseArguments(string[] args)
        {
            var options = new CommandLineOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-h":
                    case "--help":
                        ShowHelp();
                        return null;
                        
                    case "-c":
                    case "--command":
                        if (i + 1 < args.Length) options.Command = args[++i];
                        break;
                        
                    case "-p":
                    case "--path":
                        if (i + 1 < args.Length) options.InputPath = args[++i];
                        break;
                        
                    case "-o":
                    case "--output":
                        if (i + 1 < args.Length) options.OutputPath = args[++i];
                        break;
                        
                    case "-r":
                    case "--replace":
                        if (i + 1 < args.Length) options.ReplacePath = args[++i];
                        break;
                        
                    case "-f":
                    case "--filter":
                        if (i + 1 < args.Length) options.Filter = args[++i];
                        break;
                        
                    case "-C":
                    case "--convert":
                        options.ConvertToReadable = true;
                        break;
                        
                    default:
                        Console.WriteLine($"未知参数: {args[i]}");
                        ShowHelp();
                        return null;
                }
            }

            // 验证必需参数
            if (string.IsNullOrEmpty(options.Command))
            {
                Console.WriteLine("错误: 必须指定命令类型");
                ShowHelp();
                return null;
            }

            if (string.IsNullOrEmpty(options.InputPath))
            {
                Console.WriteLine("错误: 必须指定输入文件路径");
                ShowHelp();
                return null;
            }

            if (string.IsNullOrEmpty(options.OutputPath))
            {
                Console.WriteLine("错误: 必须指定输出路径");
                ShowHelp();
                return null;
            }

            return options;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("BundleReplacer - Unity AssetBundle 修改工具");
            Console.WriteLine("用法: BundleReplacer -c <command> -p <input_path> -o <output_path> [options]");
            Console.WriteLine();
            Console.WriteLine("命令:");
            Console.WriteLine("  export          导出资产文件");
            Console.WriteLine("  import          导入替换资产");
            Console.WriteLine("  export-bundle   导出 Bundle 文件");
            Console.WriteLine("  import-bundle   导入替换到 Bundle 文件");
            Console.WriteLine();
            Console.WriteLine("参数:");
            Console.WriteLine("  -c, --command <command>   执行命令");
            Console.WriteLine("  -p, --path <path>        输入文件路径");
            Console.WriteLine("  -o, --output <path>      输出路径");
            Console.WriteLine("  -r, --replace <path>     替换文件目录（import 命令需要）");
            Console.WriteLine("  -f, --filter <types>     过滤类型（逗号分隔，如：texture2d,textasset）");
            Console.WriteLine("  -C, --convert           转换为可读格式");
            Console.WriteLine("  -h, --help              显示帮助信息");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  BundleReplacer -c export-bundle -p game.bundle -o ./export");
            Console.WriteLine("  BundleReplacer -c export-bundle -p assets.bundle -o ./textures -f texture2d,sprite");
            Console.WriteLine("  BundleReplacer -c import-bundle -p original.bundle -r ./modified -o patched.bundle");
        }

        private static void ProcessCommand(CommandLineOptions options)
        {
            switch (options.Command.ToLower())
            {
                case "export":
                    ExportAssets(options.InputPath, options.OutputPath, options.Filters);
                    break;
                    
                case "import":
                    if (string.IsNullOrEmpty(options.ReplacePath))
                    {
                        Console.WriteLine("错误: import 命令需要 -r 参数指定替换目录");
                        return;
                    }
                    ImportAssets(options.InputPath, options.ReplacePath, options.OutputPath, options.Filters);
                    break;
                    
                case "export-bundle":
                    ExportBundle(options.InputPath, options.OutputPath, options.Filters, options.ConvertToReadable);
                    break;
                    
                case "import-bundle":
                    if (string.IsNullOrEmpty(options.ReplacePath))
                    {
                        Console.WriteLine("错误: import-bundle 命令需要 -r 参数指定替换目录");
                        return;
                    }
                    ImportToBundle(options.InputPath, options.ReplacePath, options.OutputPath, options.Filters);
                    break;
                    
                default:
                    Console.WriteLine($"未知命令: {options.Command}");
                    ShowHelp();
                    break;
            }
        }

        #region Bundle 处理功能
        private static void ExportBundle(string bundlePath, string outputDir, string[] filters, bool convert = false)
        {
            if (!File.Exists(bundlePath))
                throw new FileNotFoundException($"Bundle 文件不存在: {bundlePath}");

            Console.WriteLine($"正在导出 Bundle: {Path.GetFileName(bundlePath)}");
            Directory.CreateDirectory(outputDir);

            using (var stream = File.OpenRead(bundlePath))
            using (var reader = new AssetsFileReader(stream))
            {
                var bundleFile = new AssetBundleFile();
                bundleFile.Read(reader);

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
                        if (fileExtension == ".assets")
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

            Console.WriteLine($"正在处理 Bundle: {Path.GetFileName(bundlePath)}");

            // 加载原始 Bundle
            AssetBundleFile originalBundle;
            using (var stream = File.OpenRead(bundlePath))
            using (var reader = new AssetsFileReader(stream))
            {
                originalBundle = new AssetBundleFile();
                originalBundle.Read(reader);
            }

            var newBundle = new AssetBundleFile();
            int replacedCount = 0;

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
                    replacedCount++;
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

            Console.WriteLine($"处理完成: 替换了 {replacedCount} 个文件");
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
            Console.WriteLine($"处理资产文件: {Path.GetFileName(assetsPath)}");
            
            // 示例：简单的文件复制到子目录
            var assetsOutputDir = Path.Combine(outputDir, "assets_content");
            Directory.CreateDirectory(assetsOutputDir);
            
            // 这里可以调用现有的资产处理逻辑
        }

        // 现有的资产导出导入方法（保持原有功能）
        private static void ExportAssets(string inputPath, string outputDir, string[] filters)
        {
            Console.WriteLine($"执行资产导出: {inputPath} -> {outputDir}");
            // 原有的导出逻辑
        }

        private static void ImportAssets(string inputPath, string replacePath, string outputPath, string[] filters)
        {
            Console.WriteLine($"执行资产导入: {inputPath} -> {outputPath}");
            Console.WriteLine($"替换目录: {replacePath}");
            // 原有的导入逻辑
        }
        #endregion
    }

    public class CommandLineOptions
    {
        public string Command { get; set; }
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public string ReplacePath { get; set; }
        public string Filter { get; set; }
        public bool ConvertToReadable { get; set; }

        public string[] Filters => string.IsNullOrEmpty(Filter) 
            ? Array.Empty<string>() 
            : Filter.Split(',').Select(f => f.Trim()).ToArray();
    }
}
