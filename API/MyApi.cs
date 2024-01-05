using System.IO;

namespace MyCommonFunctions;

public class MyApi
{
    /// <summary>
    /// 重複しないファイルパスに書き換える
    /// </summary>
    /// <param name="filePath"></param>
    public static void ToNonDuplicatePath(ref string filePath)
    {
        // ファイルパスが重複しないならば何もしない
        if (false == File.Exists(filePath)) return;

        // ファイルパスが重複するならば新しいファイル名に書き換える
        var directoryPath = Path.GetDirectoryName(filePath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        int duplicateCount = 2;

        // 重複しないファイルパスが見つかるまで番号を増やす
        do
        {
            filePath = Path.Combine(
                directoryPath, 
                $"{fileName}_{duplicateCount}{extension}"
            );
            duplicateCount++;
        } while (File.Exists(filePath));
    }
}