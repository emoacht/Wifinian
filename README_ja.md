[English](README.md)

WLAN Profile Viewer
===================

WLAN Profile Viewerは無線LANプロファイルを管理するためのWindowsデスクトップアプリです。以下のことが可能です。
 - 既存の無線LANプロファイルの一覧表示
 - 無線LANへの接続と切断
 - 無線LANプロファイルの順番（優先度）の変更
 - 無線LANプロファイルの削除

![Screenshot on Windows 10](Images/Screenshot_Win10.png)<br>
![Screenshot on Windows 8.1](Images/Screenshot_Win81.png)

##動作条件

 * Windows 7以降
 * .NET Framework 4.5.2

##ダウンロード

[ダウンロード](https://github.com/emoacht/WlanProfileViewer/releases/download/1.1.2/WlanProfileViewer112.zip)

##インストール

インストール作業は必要ありません。

設定ファイルは以下のフォルダーに保存されます。<br>
[システムドライブ]\Users\\[ユーザー名]\AppData\Roaming\WlanProfileViewer\

##特記事項

 - 各プロファイルは特定の無線LANアダプターに関連付けられているので、そのアダプターが取り外されているときは（例えばUSBアダプターの場合）、表示されません。

 - リロードは無線LANアダプターに無線LANの再スキャンを要求します。結果的に、既に接続中の場合、頻繁なリロードは接続速度を遅くするかもしれません。

 - Windows 10では、プロファイルの順番を変更することはできますが（電波が入っているプロファイルを除く）、OSが状況によって自動的に順番を変更するため、実質的に無意味です。

 - プロファイルの順番を変更したとき、別のプロファイルの位置が飛ぶことがあります（とくに認証がオープンな場合）。OS自身によるものですが、明確な理由は不明です。

##履歴

[History](History.md)

##ライセンス

 - MIT License

##ライブラリ

 - [Reactive Extensions][1]
 - [Reactive Property][2]
 - [Managed Native Wifi][3]
 - [WPF Monitor Aware Window][4]

[1]: https://github.com/Reactive-Extensions/Rx.NET
[2]: https://github.com/runceel/ReactiveProperty
[3]: https://github.com/emoacht/ManagedNativeWifi
[4]: https://github.com/emoacht/WpfMonitorAware

##参考

###無線LANプロファイルをOSのGUIから削除する方法

無線LANプロファイルを削除するためのGUIがWindows 8.1 Updateから復活しました。このGUIに辿り着くには以下を見てください。

####Windows 8.1 Update

チャームの[設定] &rarr; [PC設定の変更] &rarr; [ネットワーク] &rarr; [接続] &rarr; [Wi-Fi]の[既知のネットワークの管理]

####Windows 10

通知領域から[ネットワーク設定]（またはスタートメニューから[設定]）&rarr; [ネットワークとインターネット] &rarr; [Wi-Fi] &rarr; [Wi-Fi設定を管理する] &rarr; [既知のネットワークの管理]

注意: 同名のプロファイルが複数ある場合（プロファイル名には無線LANのSSIDが使われるので、同じ無線LANに複数の無線LANアダプターで接続した場合に起こる）、これらは区別されず、まとめて削除されます。
