# KK Daily Outfits — v0.4.0 測試版

v0.4.0：角色欄位使用遊戲內建班級名稱（1-1、2-1、2-2、3-1），例如 `角色名 (2-2, Seat 20)`，按班級索引與座位排序；劇情角色顯示時排在一般角色後方。

角色衣櫃預設為 `Disabled`，只有選擇資料夾或輸入非空路徑才啟用換裝。取消指定／清空路徑即可停用；不會還原目前身上的衣服，也不會清除原有穿著紀錄。舊版留空的 Shared wardrobe 設定在升級後變成 Disabled，原已指定的路徑保留。不再提供 Shared wardrobe 選項；若要共用，請讓多位角色選相同資料夾。

插件每次啟動會補建 `UserData/DailyOutfits/DefaultCloset`，安裝腳本也會建立。已存在的卡片不會被覆寫；無法建立時記錄警告。這個資料夾只供選用，不會自動分配給角色。若自訂 OutfitFolder 指向其他位置，選單仍以自訂位置為準。

v0.4.0 已編譯並通過 159 項檢查；班級排序、啟動時建目錄與 Disabled 的遊戲內行為仍待實測。以下舊版更新紀錄中的 Shared wardrobe 描述僅代表舊版行為。

v0.3.2 簡化 F1 衣櫃介面（維持英文）：`Show wardrobe status` 原位切換為 `Hide wardrobe status`，刷新改放狀態底部的 `Refresh status`。衣櫃選单不再重複列出目前選項；換成其他資料夾後仍可選回共用根目錄。`Refresh folders` 統一放在 General 的 `OutfitFolder` 下，所有角色共用掃描結果。`Custom path...` 預設收起。資料夾選單高度依選項數量縮短，且選單／狀態不會同時展開。此次只調整 UI，未改動抽選、存檔或冷卻邏輯；已编譯，實際遊戲內排版仍待確認。

v0.3.1 修正轉校後舊衣櫃欄位殘留：載入存檔後每秒對照目前角色名單，確認離開的角色會刪除其衣櫃設定與本存檔中的穿著紀錄；實際服裝卡與資料夾不會刪除。重新轉入的角色可重新選擇衣櫃。角色欄位改顯示名稱與班級／座位，固定劇情角色標示 `(Story)`。

新增 `General / ShowStoryCharacters`，預設不勾選。勾選才在設定清單顯示固定劇情角色；這只控制顯示，不停用他們原本的每日換裝。依遊戲 `fixCharaID != 0` 判定，不靠名字。切換後约一秒更新清單，必要時關閉再開 F1。

換存檔時先隱藏上一份存檔的角色欄位，不把另一份存檔的角色誤認成轉校者刪除。升級前已留在 cfg、但不屬於當前角色的未綁定舊鍵不會顯示，也不會僅憑缺席便刪除（可能屬於其他存檔）。自動刪除針對此版本在同一份已載入存檔中確認的轉校。轉校後請正常存檔，才能把紀錄清除結果保存到該存檔。

v0.3.0 維持英文設定介面，新增角色衣櫃資料夾選單與狀態顯示。點衣櫃欄位的 `▼` 可選共用根目錄或子資料夾，`Refresh folders` 重掃資料夾；自訂絕對路徑仍可輸入，原設定不會被選單重設。

點 `Show wardrobe status` 顯示 `Today (last applied)`、各張卡的 `Available`／`Washing - N day(s) until eligible`／`Wearing today`，以及無法使用的卡片原因。`N` 是從目前遊戲日算起，還要經過幾次換日才符合冷卻規則。套數不足仍沿用最久沒穿優先的規則。

今日穿著顯示的是本插件最後成功套用的紀錄，不會偵測其他插件或手動換装。更換衣櫃後若原卡不在該資料夾，會顯示 `Not found in this wardrobe`。狀態檢視只讀取卡片、不會幫角色試穿；可用卡表示通過來源驗證，角色本身的額外資料限制仍在實際套用時檢查。新增／刪除卡片後請按 `Refresh wardrobe status`；換日、讀檔會清除快取。子資料夾掃描不跟隨 junction／symlink，避免循環掃描。

v0.2.0 新增 `General / NoRepeatDays`（0–365，預設 1）。設為 1 時，昨天穿過的卡今天不會再抽中；設為 2 時，排除前兩天穿過的卡。0 關閉排除。有足夠套數時從可用卡中隨機抽選；套數不足時選最久沒穿的卡，同樣久則隨機；只有一套時仍穿那套。要保證 N 天不重複，至少需要 N+1 張不同的可用卡。

每位角色的穿著紀錄獨立，與遊戲存檔一起保存，正常存檔後重開會記得；讀取舊存檔也會回到當時紀錄。不會依現實時間推進。沒有成功換裝時不記入新卡，原套裝則視為繼續穿著。`NoRepeatDays` 本身存在插件設定檔，重開仍保留。

紀錄以整張服裝卡的 SHA-256 識別，所以改檔名、搬資料夾或放入完全相同的副本不會繞過限制；重新儲存卡片（即使只改縮圖）會被視為新卡。不同卡即使內衣褲外觀相同，仍可能算不同套。升級前的版本沒有紀錄，因此從新版第一次成功換日更衣開始記錄，不能回推之前穿過哪張卡。

v0.1.1 修正 F1 角色欄位顯示：使用 Configuration Manager 要求的屬性類型名稱，讓欄位顯示角色名與短識別碼。沿用原有設定鍵與衣櫃路徑，換裝邏輯未變。已編譯；遊戲內顯示仍待驗證。

原版 Koikatsu 的 BepInEx 插件。每次劇情模式換日，從每位女性角色自己的服裝卡資料夾隨機抽一張，僅替換內衣與內褲。

目前狀態：使用者已實測舊版可每日換內衣褲、當天切換外衣仍保持同一套。新版已編譯並通過 23 項服裝／資料夾檢查與 124 項穿著紀錄檢查（含連續 100 天存讀檔模擬）；**新介面排版與冷卻紀錄仍需遊戲內驗證**。

## 使用方式

1. 關閉開發副本的遊戲，在 PowerShell 執行 `./install.ps1`。預設遊戲位置為 `D:\Koikatsu3.33Perfection2`。
2. 在遊戲的 `UserData\DailyOutfits` 下放服裝卡 PNG。這是遊戲儲存的服裝卡，不是一般圖片、角色卡或 zipmod。
3. 可以建立不同衣櫃，例如：

   ```text
   UserData/DailyOutfits/
     DefaultCloset/         ← 自動建立；選用它的角色才會抽其中卡片
       shared-01.png
     Alice/
       underwear-01.png
       underwear-02.png
     Bob/
       underwear-01.png
   ```

4. 啟動遊戲並載入劇情存檔，按 **F1** 打開插件設定，搜尋 **KK Daily Outfits**。你的 Configuration Manager 目前設定為 F1；若遊戲整合包覆寫快捷鍵，請從其插件設定入口開啟。
5. 在 **Character wardrobes** 中找到角色，預設是 `Disabled`。展開衣櫃選單並選擇 `Alice`、`Bob` 或 `DefaultCloset` 即啟用該角色的每日換裝。也能輸入完整路徑；選回 `Disabled` 或清空路徑即停用。
6. 每個衣櫃**只讀取該層的 PNG**，不會把子資料夾一併抽選。指定資料夾不存在或為空時會跳過，不會偷偷使用別人的衣櫃。
7. 第一次設定後儲存遊戲，讓角色的識別碼一起保存。若角色欄尚未出現，推進一個時段後重新開啟設定；新增角色也會在時段切換時註冊。
8. 推進至隔天。每人從自己的資料夾抽一張，以同一張卡的內衣與內褲配成一套。

角色名稱後的小段識別碼可區別同名角色。資料夾設定存在 BepInEx 設定檔中，角色識別碼則存在遊戲存檔；換電腦時兩者都要帶走。更名後可能需重啟遊戲才更新設定欄顯示名稱。

## 第一版行為與限制

- 只對劇情模式的女性角色處理，不支援 Studio、自由模式或 Sunshine。
- 預設同步角色的制服、運動服、社團服、便服、睡衣等服裝欄位的內衣／內褲；**泳裝欄位預設跳過**，可用 `IncludeSwimCoordinate` 開啟。
- 保留其他衣物、髮型、配件、妝容、外衣的遮罩選項；某些外衣本來會隱藏內衣，換裝後依然遵守原本的隱藏規則。
- 抽選只掛在 KKAPI 的 `OnDayChange`；讀檔、切換場景不主動抽選。更換結果與穿著紀錄在遊戲正常存檔時保存，抽選遵守 `NoRepeatDays`。
- 不覆寫磁碟上的來源服裝卡或原始角色卡。換裝會改變目前遊戲內角色的服裝資料，儲存遊戲後即保留；停用插件不會自動還原已儲存的換裝。
- 支援基本服裝資料（款式、顏色、圖案、徽章與部件選項）。服裝卡由正常遊戲載入流程讀取，交由已安裝的 Sideloader 解析 MOD ID；zipmod 服裝相容性仍待遊戲內驗證。
- **MaterialEditor、Overlay 等擴充資料尚未支援局部轉移。** 這版以擴充資料 ID 中的 `material`／`overlay`／`clothes` 關鍵字保守判定；來源服裝卡含這類資料會跳過，目標角色含這類資料也會整個跳過，即使資料可能只影響其他部位。因此整合包裡的某些角色可能完全不更衣。未知插件仍可能需要另做相容處理。
- 空資料夾、無效服裝卡、跳過原因、選中的卡名與成功數量會記錄在日誌，搜尋 `KK Daily Outfits`。此整合包關閉了 BepInEx 磁碟日誌，請查看遊戲根目錄 `output_log.txt`；啟用磁碟日誌的環境則可查 `BepInEx\LogOutput.log`。

## 遊戲內驗收

建議使用測試存檔以及沒有額外材質／貼圖資料的角色與服裝卡：

1. 建立兩個衣櫃，各放一张明顯不同的服裝卡；指定給兩位角色。
2. 換日，確認兩人各自使用指定的內衣褲，外衣、襪子、配件與髮型不變。
3. 切換角色的制服與便服，確認內衣褲一致；泳裝維持原樣。
4. 存檔、退出、重新載入，確認角色衣櫃設定仍對應正確。
5. 每個衣櫃放兩張不同卡，`NoRepeatDays = 1`，從新版首次成功抽選起確認兩套逐日交替。存檔重開後也應交替。再用三張卡與 `NoRepeatDays = 2` 驗證排除前兩天；僅一張卡時仍應正常換日。
6. 檢查空衣櫃、不存在的衣櫃、含一般 PNG 的衣櫃：應記錄原因而不覆蓋角色服裝。

出問題時保留 `LogOutput.log`，以及角色使用的衣櫃設定，方便定位。

## 開發與建置

```powershell
./build.ps1 -GamePath 'D:\Koikatsu3.33Perfection2'
./tests/run.ps1 -GamePath 'D:\Koikatsu3.33Perfection2'
./install.ps1 -GamePath 'D:\Koikatsu3.33Perfection2'
```

建置使用已安裝的 .NET SDK Roslyn 編譯器，但直接參考原版遊戲的 CLR 2.0／.NET 3.5 DLL，避免誤產生現代 .NET 插件。無須下載 NuGet 套件，也不會將遊戲 DLL 複製到專案或輸出目錄。

已驗證的本機環境：BepInEx 5.4.23.2、KKAPI 1.42.2、BepisPlugins 20.1。輸出為 `dist/KK_DailyOutfits.dll`。

測試使用遊戲的實際 `ChaFileClothes` 類別，檢查僅指定部件被替換、顏色／圖案被複製、資料不共用、無效來源不會造成單邊替換，以及角色資料夾隔離。它們不等同於 Unity 內的整合測試。

安裝只複製本插件 DLL 並建立空的 `UserData\DailyOutfits` 資料夾。若已有舊版 DLL，安裝腳本會先將其備份至專案 `dist/previous`。解除安裝可在關閉遊戲後移除 `BepInEx\plugins\KK_DailyOutfits\KK_DailyOutfits.dll`；服裝卡不受影響。

## 參考

- [KKAPI GameCustomFunctionController](https://github.com/IllusionMods/IllusionModdingAPI/blob/master/src/KKAPI/MainGame/GameCustomFunctionController.cs)
- [KK_Pregnancy 換日事件使用範例](https://github.com/ManlyMarco/KoikatuGameplayMods/blob/master/src/KK_Pregnancy/PregnancyGameController.cs)

這是獨立實作，沒有複製 Pregnancy 插件程式碼，也不依賴它。方法簽名與服裝欄位已對照本機遊戲 DLL。
