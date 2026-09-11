# hidari-camera-on

OBS Studioの左カメラ表示グループを、コメントまたは音声認識結果から制御するASP.NET Coreアプリです。

## 開発時の準備

`token`内の`.example`ファイルを拡張子なしの実ファイルへコピーし、値を書き換えてください。

## 実行

```bash
dotnet restore hidari-camera-on.csproj
dotnet run --project hidari-camera-on.csproj
```

ヘルスチェックは`http://127.0.0.1:15082/health`です。
