using System.Windows;
using System.Windows.Media;

namespace MyCommonFunctions;

public class WpfApi
{
    /// <summary>
    /// VisualTree内から指定された型の子要素を検索するメソッド
    /// </summary>
    /// <param name="obj"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
    {
        if(obj is null) return null;

        for(var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if(child is null) continue;
            if(child is T childAsT) return childAsT;

            var childOfChild = FindVisualChild<T>(child);
            if(childOfChild is not null) return childOfChild; 
        }
        return null;
    }
}