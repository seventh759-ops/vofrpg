# M0：Unityプロジェクトの作り方

Unityプロジェクトは、このリポジトリの `Game/` フォルダに置く。
`.gitignore` と `.gitattributes` は用意済み（`Game/Library` などの自動生成フォルダは git に入らない）。

## 1. 準備

1. Unity Hub で **Unity 6 の LTS 版**（その時点の最新）を入れる
   - 追加モジュールで **Android Build Support**（OpenJDK、Android SDK & NDK Tools も含める）にチェック
   - PC版も作るので **Windows Build Support** も入っていることを確認（Windows のエディタなら標準で入っている）
2. このリポジトリを手元に用意し、作業ブランチに切り替える

```
git clone https://github.com/seventh759-ops/vofrpg.git
cd vofrpg
git checkout claude/brave-brahmagupta-dnbh9z
```

## 2. プロジェクトを作る

1. Unity Hub →「新しいプロジェクト」
2. テンプレートは **Universal 2D**（URP の 2D 用）
3. プロジェクト名：**Game**
4. 保存場所：手元の **vofrpg フォルダ**（結果として `vofrpg/Game/` ができる）
5. 作成してエディタが開くまで待つ

## 3. エディタでの設定

| 場所 | 設定 |
| --- | --- |
| File → Build Profiles | **Android** を選んで「Switch Platform」 |
| Edit → Project Settings → Player → Resolution and Presentation | Default Orientation：**Portrait** |
| Edit → Project Settings → Player → Other Settings | Active Input Handling：**Input System Package (New)**（「Both」でも動く） |
| Edit → Project Settings → Player | Company Name・Product Name を入れる（Product Name は仮で「Himori」など） |
| Game ビューの解像度 | 「+」で **1080×2340（縦）** を追加して選ぶ |
| Window → Package Manager | **Input System** と **2D Pixel Perfect**（2D の中に含まれている場合あり）が入っていることを確認 |

## 4. 確かめる

- Play を押して、空のシーンが縦長の画面で表示されれば M0 の完了
- できれば Android 端末でも確認：端末の「開発者向けオプション」で USB デバッグを有効にし、USB でつないで Build Profiles の「Build And Run」

## 5. git に入れる

```
git status
```

で `Game/Assets`、`Game/Packages`、`Game/ProjectSettings` だけが出ていて、**`Game/Library` が出ていない**ことを確認してから：

```
git add .
git commit -m "Create Unity project"
git push
```

push したら教えてほしい。次の M1（移動・回避・自動攻撃・群れ）のスクリプトを `Game/Assets/Scripts/` に追加する。

## うまくいかないとき

- Unity Hub が「フォルダが空でない」と言う：`vofrpg/Game` が既にあると作れない。いったん削除するか別名で作ってから `Game` に名前を変える
- `git status` に `Library` が出る：`.gitignore` がリポジトリの一番上にあるか、フォルダ名が `Game` になっているかを確認
