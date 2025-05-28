# BundleReplacer

## 说明

这是一个基于 <https://github.com/nesrak1/AssetsTools.NET> 制作的简易命令行工具，允许用户替换 Unity 游戏中资源包（AssetBundle）文件里包含的 MonoBehaviour、TestAsset、Texture2D 资产。

### 使用方法

#### 导出
```
BundleReplacer.exe export -p <input_path> -o <output_path> -f <replace_path>
```

- `-p` 或 `--path=` 指定资源包文件路径。
- `-o` 或 `--output=` 指定导出文件的输出目录。
- `-f` 或 `--filter=` 指定导出的类型，包含 `MonoBehaviour`、`TestAsset` 和 `Texture2D`，以 `,` 分隔。

#### 替换
```
BundleReplacer.exe import -p <input_path> -r <replace_path> -o <output_path> -f <replace_path>
```

- `-p` 或 `--path=` 指定资源包文件路径。
- `-r` 或 `--replace=` 指定替换的文件目录。
- `-o` 或 `--output=` 指定替换后的资源包输出路径。
- `-f` 或 `--filter=` 指定替换的类型，包含 `MonoBehaviour`、`TestAsset` 和 `Texture2D`，以 `,` 分隔。

## Decription

This is a simple command line tool based on <https://github.com/nesrak1/AssetsTools.NET> that allows users to replace MonoBehaviour, TestAsset, and Texture2D assets in Unity game AssetBundle files.

### Usage
#### Export
```
BundleReplacer.exe export -p <input_path> -o <output_path> -f <replace_path>
```

- `-p` or `--path=` specifies the path to the AssetBundle file.
- `-o` or `--output=` specifies the output directory for the exported files.
- `-f` or `--filter=` specifies the types to export, including `MonoBehaviour`, `TestAsset`, and `Texture2D`, separated by `,`.

#### Import
```
BundleReplacer.exe import -p <input_path> -r <replace_path> -o <output_path> -f <replace_path>
```

- `-p` or `--path=` specifies the path to the AssetBundle file.
- `-r` or `--replace=` specifies the directory of files to replace.
- `-o` or `--output=` specifies the output path for the replaced AssetBundle.
- `-f` or `--filter=` specifies the types to replace, including `MonoBehaviour`, `TestAsset`, and `Texture2D`, separated by `,`.

## License

This repo is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
