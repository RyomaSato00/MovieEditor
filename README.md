# Movie Editor使用方法

## 目次

- [0. 使用準備](#preparation)

- [1. 動画圧縮](#compression)

- [2. 動画音声抽出](#audioExtraction)

- [3. 再生速度変更](#speedChange)

<div id="preparation">

</div>

## 0. 使用準備

[FFmpegをインストール](https://ffmpeg.org/)する。インストールしたら、FFmpegのbinフォルダにある、"ffmpeg.exe", "ffplay.exe", "ffprobe.exe"以上3つの実行ファイルをコピーし、Movie Editorのルートディレクトリにペーストする。

<div id="compression">

</div>

## 1. 動画圧縮

1. 圧縮したい動画ファイルを「ファイルを追加」ボタンを押して選択する。
   または、動画ファイルを以下の領域にドラッグアンドドロップする。
   
   <img title="" src="README_imgs/drag_and_drop.png" alt="drag_and_drop.png" data-align="center">

2. 圧縮条件を入力する。数値条件を負の数にすると、「元ファイルと同じ」と認識される。
   ![圧縮条件.png](README_imgs/圧縮条件.png)

3. オプションで時間範囲指定が可能。リストの中から一つの動画の上で右クリックすると、「時間範囲指定」の項目が現れる。そこで開始時刻と終了時刻を秒数で指定する。

4. 出力先のディレクトリを設定する。「参照」ボタンからフォルダを選択するか、エクスプローラーからフォルダをドラッグアンドドロップすることで設定可能である。
   また、出力ファイルには「出力タグ」で設定した文字列がファイル名に付け加えられる。 （例：出力タグがcmpのとき、movie.mp4 → movie_cmp.mp4）![出力設定.png](README_imgs/出力設定.png)

5. 「実行」ボタンを押すことで圧縮が開始される。

<div id="audioExtraction">

</div>

## 2. 動画音声抽出

入力ファイルのセットおよび出力先ディレクトリの設定は[動画圧縮](#compression)と同様の操作で行う。
その後、「実行」ボタンを押すと出力先ディレクトリに音声ファイルが出力される。

<div id="speedChange">

</div>

## 3. 再生速度変更

入力ファイルのセットおよび出力先ディレクトリの設定は[動画圧縮](#compression)と同様の操作で行う。その後、速度倍率を指定および「実行」ボタンを押すと出力先ディレクトリに動画ファイルが出力される。
