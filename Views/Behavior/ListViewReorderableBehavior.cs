using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MovieEditor.Views.Behavior;

/// <summary>
/// ドラッグアンドドロップによってリストの項目を入れ替えるために必要なビヘイビア
/// （入れ替え先のインデックスを取得できる）
/// </summary>
internal class ListViewReorderableBehavior
{
    /// <summary>
    /// プロパティの定義
    /// </summary>
    public static readonly DependencyProperty ReorderProperty =
    DependencyProperty.RegisterAttached(
        // プロパティの名前
        "Reorder",
        // プロパティの型
        typeof(ICommand),
        // このプロパティの所有者の型
        typeof(ListViewReorderableBehavior),
        // プロパティの初期値をnullに設定し、プロパティ変更時のイベントハンドラにOnPropertyChangedを設定する
        new UIPropertyMetadata(null, OnPropertyChanged)
    );

    /// <summary>
    /// プロパティを取得します
    /// </summary>
    /// <param name="target">対象とするDependencyObject</param>
    /// <returns></returns>
    [AttachedPropertyBrowsableForType(typeof(ListView))]
    public static ICommand GetReorder(DependencyObject target)
    {
        return (ICommand)target.GetValue(ReorderProperty);
    }

    /// <summary>
    /// プロパティを設定します
    /// </summary>
    /// <param name="target">対象とするDependencyObject</param>
    /// <param name="value">設定する値</param>
    public static void SetReorder(DependencyObject target, ICommand value)
    {
        target.SetValue(ReorderProperty, value);
    }

    /// <summary>
    /// プロパティ変更イベントハンドラ
    /// </summary>
    /// <param name="sender">イベント発行元</param>
    /// <param name="e">イベント引数</param>
    private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        // イベント発行元がItemsControl（ListView等の上位クラス）でなければ何もしない
        if (sender is not ItemsControl itemsControl) return;

        // デフォルトでイベントを削除
        itemsControl.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        itemsControl.PreviewMouseMove -= OnPreviewMouseMove;
        itemsControl.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
        itemsControl.PreviewDragEnter -= OnPreviewDragEnter;
        itemsControl.PreviewDragLeave -= OnPreviewDragLeave;
        itemsControl.PreviewDrop -= OnPreviewDrop;

        // プロパティが設定されていれば、イベントを登録する
        if (GetReorder(itemsControl) is not null)
        {
            // ドロップを有効にする
            itemsControl.AllowDrop = true;
            // 各種イベント登録
            itemsControl.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            itemsControl.PreviewMouseMove += OnPreviewMouseMove;
            itemsControl.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            itemsControl.PreviewDragEnter += OnPreviewDragEnter;
            itemsControl.PreviewDragLeave += OnPreviewDragLeave;
            itemsControl.PreviewDrop += OnPreviewDrop;
        }
    }

    /// <summary>
    /// PreviewMouseLeftButtonDown イベントハンドラ
    /// </summary>
    /// <param name="sender">イベント発行元</param>
    /// <param name="e">イベント引数</param>
    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // イベント発行元がFrameworkElementでなければ何もしない
        if (sender is not FrameworkElement control) return;
        // イベント発行元の要素がFrameworkElementでなければ何もしない
        if (e.OriginalSource is not FrameworkElement origin) return;
        // ドラッグしようとしている要素の最初の親（ListViewやListBoxのItem要素）を取得する
        var rootElement = GetTemplatedRootElement(origin);
        // 親がなければ何もしない
        if (rootElement is not FrameworkElement draggedItem) return;
        _temporaryData = new DragDropObject
        {
            // GetWindowにより、ウィンドウを基準とした相対位置からドラッグの開始位置を取得
            Start = e.GetPosition(Window.GetWindow(control)),
            // Item要素を保持しておく
            DraggedItem = draggedItem
        };
    }

    /// <summary>
    /// PreviewMouseMove イベントハンドラ
    /// </summary>
    /// <param name="sender">イベント発行元</param>
    /// <param name="e">イベント引数</param>
    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        // このイベントはマウスがこのコントロール上を通過するだけで発生してしまう
        // ドラッグ開始したかどうかを_temporaryDataがnullかどうかで判断する
        if (_temporaryData is null) return;

        // イベント発行元がFrameworkElementでないならば何もしない
        if (sender is not FrameworkElement control) return;

        // ウィンドウを基準とした相対位置を取得
        var current = e.GetPosition(Window.GetWindow(control));

        // ドロップが可能なほど移動したか判定する
        if (_temporaryData.CheckStartDragging(current))
        {
            // 手動でドラッグ状態に移行させる（この状態にならないとドロップイベントは呼ばれない）
            DragDrop.DoDragDrop(control, _temporaryData.DraggedItem, DragDropEffects.Move);
        }
    }

    /// <summary>
    /// PreviewMouseLeftButtonUp イベントハンドラ
    /// </summary>
    /// <param name="sender">イベント発行元</param>
    /// <param name="e">イベント引数</param>
    private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // マウスボタンを押したものの、動かさずそのままボタンを離した場合、特に処理をせずに終了
        // _temporaryDataだけは破棄する
        _temporaryData = null;
    }

    /// <summary>
    /// PreviewDragLeave イベントハンドラ
    /// </summary>
    /// <param name="sender">イベント発行元</param>
    /// <param name="e">イベント引数</param>
    private static void OnPreviewDragLeave(object sender, DragEventArgs e)
    {
        // _temporaryDataがnull（＝ドラッグしていない）ならば何もしない
        if(_temporaryData is null) return;
        // コントロールの外側へ出たときはドロップしても何もしないようにするためのフィルタ
        _temporaryData.IsDroppable = false;
    }

    /// <summary>
    /// PreviewDragEnter イベントハンドラ
    /// </summary>
    /// <param name="sender">イベント発行元</param>
    /// <param name="e">イベント引数</param>
    private static void OnPreviewDragEnter(object sender, DragEventArgs e)
    {
        // _temporaryDataがnull（＝ドラッグしていない）ならば何もしない
        if(_temporaryData is null) return;
        // 一度コントロールの外側へ出ても、もう一度コントロールの内側へ戻ってきたならば
        // ドロップ可能にするためのフィルタ
        _temporaryData.IsDroppable = true;
    }

    /// <summary>
    /// PreviewDrop イベントハンドラ
    /// </summary>
    /// <param name="sender">イベント発行元</param>
    /// <param name="e">イベント引数</param>
    private static void OnPreviewDrop(object sender, DragEventArgs e)
    {
        // ドラッグしていないならば何もしない
        if(_temporaryData is null) return;
        // コントロールの外側でドロップしたならば何もしない
        if(false == _temporaryData.IsDroppable) return;
        // イベント発行元がItemcControlでないならば何もしない
        if(sender is not ItemsControl itemsControl) return;

        // 異なる ItemsControl（ListView等の上位クラス） 間でドロップ処理されないようにするために
        // 同一 ItemsControl 内にドラッグされたコンテナが存在することを確認する
        // コンテナが存在しなければ何もしない
        if(0 > itemsControl.ItemContainerGenerator.IndexFromContainer(_temporaryData.DraggedItem)) return;
        // イベント発行元の要素がFrameworkElementでなければ何もしない
        if(e.OriginalSource is not FrameworkElement element) return;

        // イベント発行元を辿って得られるItemを取得する
        var targetContainer = GetTemplatedRootElement(element);
        // Itemのインデックスを取得する
        var index = itemsControl.ItemContainerGenerator.IndexFromContainer(targetContainer);
        if (index >= 0)
        {
            var reorder = GetReorder(itemsControl);
            // プロパティに登録されたイベントを実行する
            reorder.Execute(index);
        }

        // _temporaryDataを破棄する
        _temporaryData = null;
    }

    /// <summary>
    /// 指定された FrameworkElement に対するテンプレートのルート要素を取得します。
    /// </summary>
    /// <param name="element">FrameworkElement を指定します。</param>
    /// <returns>TemplatedParent を辿った先のルート要素を返します。</returns>
    private static FrameworkElement? GetTemplatedRootElement(FrameworkElement element)
    {
        // elementの親がFramwworkElementでないならば、nullを返す
        if (element.TemplatedParent is not FrameworkElement parent) return null;

        // 最初の親までたどる
        while (parent.TemplatedParent is FrameworkElement rootParent)
        {
            parent = rootParent;
        }

        return parent;
    }

    /// <summary>
    /// ドラッグ中の一時データ
    /// </summary>
    private static DragDropObject? _temporaryData;

    /// <summary>
    /// ドラッグ＆ドロップに関するデータを表します。
    /// </summary>
    private record DragDropObject
    {
        /// <summary>
        /// ドラッグ開始座標を取得または設定します。
        /// </summary>
        public Point Start { get; init; }

        /// <summary>
        /// ドラッグ対象であるオブジェクトを取得または設定します。
        /// </summary>
        public FrameworkElement DraggedItem { get; init; } = new FrameworkElement();

        /// <summary>
        /// ドロップ可能かどうかを取得または設定します。
        /// </summary>
        public bool IsDroppable { get; set; }

        /// <summary>
        /// ドラッグを開始していいかどうかを確認します。
        /// </summary>
        /// <param name="current">現在のマウス座標を指定します。</param>
        /// <returns>十分マウスが移動している場合に true を返します。</returns>
        public bool CheckStartDragging(Point current)
        {
            return (current - Start).Length - _minimumDragPoint.Length > 0;
        }

        /// <summary>
        /// ドラッグ開始に必要な最短距離を示すベクトル
        /// </summary>
        private static readonly Vector _minimumDragPoint = new(SystemParameters.MinimumHorizontalDragDistance, SystemParameters.MinimumVerticalDragDistance);
    }
}

