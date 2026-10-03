# 放課後卡讀取調查（2026-10-02）

## 已觀察到的證據

- BepInEx 的 Logging.Disk.Enabled=false；本次日誌實際在遊戲根目錄 output_log.txt，已保存快照為同目錄的 2026-10-02-loading-output_log.txt。
- 第 1186 行：KK Daily Outfits 在 Wednesday 成功處理 1 位角色，跳過 12 位。日誌未出現本插件的例外。
- 第 2585、2590 行（檔案末尾）：Unity 報 CAB-94df441747444b397dff0300ce932442 corrupted / Position out of bounds。
- 在 abdata 與 mods 搜尋此 CAB 字串，命中 D:\Koikatsu3.33Perfection\abdata\chara\co_bot_50.unity3d。
- 當前檔案大小 73,208,370 bytes；SHA-256 CC557F32B02F66463A14F30DF3C2EBB121E30DA6DACE11EA0DEBA6FDF9B06CE6。
- 另有備份 D:\Koikatsu\Koikatsu3.33PerfectionBackup\abdata\chara\co_bot_50.unity3d，大小相同；SHA-256 3AAE581589C6E93E46B3E356E7882E005611542AD1A6828C32833731A6BD5B92。
- 兩份檔案 1,683,396 個 byte 不同，第一處差異在 0xA1000。僅凭差異不能斷定哪一份健康；尚未在 Unity 中獨立驗證備份。
- 日誌另有 KK_LewdCrestX 缺少 KoiSkinOverlayController 的重複例外，以及幾個 zipmod 的 ZIP 格式錯誤。這些不能直接等同於本次卡讀取的根因。

## 插件檢查

OnPeriodChange 僅註冊衣櫃設定與同步識別碼；換装在 OnDayChange 執行。GetRelatedChaFiles 的本機 KKAPI 實作只收集現存角色檔案參考，未見整套服裝載入。插件與安裝腳本沒有寫入 abdata 的程式碼。

## 結論與下一步

目前最直接的線索是上述 Unity 資源讀取失敗，不能據此完全排除插件與既有服裝／插件的互動。尚未重現問題，也未修改任何遊戲檔案或停用插件。

可先關閉遊戲，暫時將 KK_DailyOutfits.dll 移出 plugins（僅關閉 Enabled 不會停用 v0.1 的時段註冊程式），用安裝插件前的存檔重試相同轉場。如果仍然報同一 CAB 錯誤，再保留當前資源檔並試用原有備份的同名檔案；不應直接依照 Unity 日誌文字刪除該資源。
