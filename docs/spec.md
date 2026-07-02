# 社群媒體系統 — 開發規格書 (Spec)

> 玉山銀行 .NET 實作題 — 簡易社群媒體平台
> 本文件為 Spec-Driven Development 的規格來源,供 GitHub Copilot coding agent 依此實作。

---

## 1. 專案目標

實作一個簡易社群媒體平台,提供註冊、登入驗證、發文、留言功能。採用三層式架構,透過 Stored Procedure 存取 SQL Server 資料庫,並具備防 SQL Injection 與 XSS 的安全防護。

---

## 2. 技術堆疊 (Tech Stack)

| 項目 | 選用技術 |
|------|----------|
| 語言 | C# |
| 框架 | ASP.NET Core 10 MVC (.NET 10) |
| 前端 | Razor View + Bootstrap 5 (支援 RWD) |
| 資料庫 | SQL Server |
| 資料存取 | **Dapper**,呼叫 Stored Procedure |
| 身份驗證 | ASP.NET Core Cookie Authentication |
| 密碼處理 | `PasswordHasher<T>` (PBKDF2,內含 per-user salt) |
| 測試 | xUnit + Moq |
| 架構 | Web Server + Application Server + RDBMS 三層式;程式碼分展示層 / 業務層 / 資料層 / 共用層 |

---

## 3. 系統架構 (Architecture)

### 3.1 三層式部署架構
```
[Browser/Client]  ──HTTP──▶  [Web Server (ASP.NET Core Kestrel/IIS)]
                                      │
                                      ▼
                              [Application Server (Business Logic)]
                                      │
                                      ▼ (Stored Procedure only)
                              [Database Server (SQL Server)]
```

### 3.2 專案分層 (Solution 結構)
採用「多專案」方式,清楚展現分層:

```
SocialMedia.sln
├── src/
│   ├── SocialMedia.Web            # 展示層:MVC Controllers, Views, wwwroot
│   ├── SocialMedia.Business       # 業務層:Services, 驗證邏輯, 交易編排
│   ├── SocialMedia.DataAccess     # 資料層:Repositories, Dapper 呼叫 SP, DbConnectionFactory
│   └── SocialMedia.Shared         # 共用層:Entities, DTOs, ViewModels, Constants, Helpers
├── tests/
│   └── SocialMedia.Tests          # 單元測試 (xUnit)
└── DB/
    ├── DDL/                       # 建表 script
    ├── SP/                        # Stored Procedure script
    └── DML/                       # 種子資料 script
```

### 3.3 各層職責與相依方向
- **相依方向**:`Web → Business → DataAccess → (SQL Server)`;`Shared` 被所有層參考。
- **展示層 (Web)**:接收 HTTP 請求、模型繫結、呼叫業務層、回傳 View。**不得**直接存取資料庫。
- **業務層 (Business)**:商業規則、輸入驗證、權限判斷、**Transaction 編排**。
- **資料層 (DataAccess)**:僅透過 Dapper 呼叫 Stored Procedure;提供 `IDbConnectionFactory`。**不含**商業邏輯。
- **共用層 (Shared)**:跨層共用的 Entity / DTO / ViewModel / 例外類別 / 常數。

---

## 4. 功能需求 (Functional Requirements)

### FR-1 註冊功能
- 使用者以**手機號碼**作為帳號註冊。
- 註冊表單欄位:
  | 欄位 | 必填 | 驗證規則 |
  |------|------|----------|
  | 手機號碼 (Phone) | ✅ | 台灣格式:`09` 開頭,共 10 碼數字;全站唯一 |
  | 使用者名稱 (UserName) | ✅ | 1~50 字元 |
  | Email | ✅ | 合法 email 格式 |
  | 密碼 (Password) | ✅ | 至少 8 碼,需含英文與數字 |
  | 確認密碼 (ConfirmPassword) | ✅ | 需與密碼相同 |
  | 自我介紹 (Biography) | ✅ | 最長 500 字元 |
  | 封面照片 (CoverImage) | ❌(選填) | 上傳圖片檔;僅允許 `.jpg/.jpeg/.png`;大小上限 5 MB;未上傳則 CoverImagePath 存 NULL |
- 手機號碼重複時,回傳明確錯誤訊息。
- 密碼經 `PasswordHasher<T>` (PBKDF2 + per-user salt) 雜湊後儲存,**嚴禁明碼**。
- 圖片上傳至 `wwwroot/uploads/covers/`,DB 僅儲存相對路徑。
- 註冊未上傳封面照片、發文未上傳圖片時,`CoverImagePath` / `ImagePath` 一律儲存為 NULL。

### FR-2 登入驗證功能
- 使用者以**手機號碼 + 密碼**登入。
- 驗證成功後,以 **Cookie Authentication** 建立登入狀態。
- 提供登出功能。
- **未登入者不得發文、編輯/刪除發文、留言**;嘗試存取受保護資源時導向登入頁 (`[Authorize]`)。

### FR-3 發文功能
- **新增發文**:登入者可發文,內容必填 (最長 2000 字元),圖片選填 (規則同 CoverImage,存至 `wwwroot/uploads/posts/`)。
- **列出所有發文**:首頁顯示**全站所有發文**,依 `CreatedAt` 由新到舊排序;每篇顯示作者名稱、內容、圖片、發佈時間、留言數。
- **發文詳細頁**:顯示單篇發文完整內容 + 該篇所有留言 + 新增留言表單。
- **編輯發文**:僅**發文作者本人**可編輯自己的發文。
- **刪除發文**:僅**發文作者本人**可刪除自己的發文;刪除發文時需**一併刪除其所有留言**(以 Transaction 保證一致性,見 NFR-3)。

### FR-4 留言功能
- 登入者可對任一發文新增留言,內容必填 (最長 1000 字元)。
- 留言顯示於發文詳細頁,依 `CreatedAt` 排序。
- **留言不提供刪除功能**(依需求)。

### FR-5 個人頁 (Profile)
- 提供簡易 Profile 頁,顯示使用者的 **封面照片、使用者名稱、Email、自我介紹**,以及**該使用者的所有發文**。
- 任何人皆可瀏覽他人 Profile;僅顯示公開資訊(不顯示密碼相關欄位)。

---

## 5. 資料庫設計 (Database Design)

> 所有資料表建立於預設 schema `dbo`。所有存取一律透過 Stored Procedure。

### 5.1 資料表

#### `Users`
| 欄位 | 型別 | 說明 |
|------|------|------|
| UserId | INT IDENTITY PK | 使用者 ID |
| Phone | VARCHAR(10) | 手機號碼,**UNIQUE**,登入帳號 |
| UserName | NVARCHAR(50) | 使用者名稱 |
| Email | NVARCHAR(256) | 電子郵件 |
| PasswordHash | NVARCHAR(MAX) | PBKDF2 雜湊後密碼(含 salt) |
| CoverImagePath | NVARCHAR(500) NULL | 封面照片相對路徑 |
| Biography | NVARCHAR(500) NULL | 自我介紹 |
| CreatedAt | DATETIME2 | 建立時間,預設 `SYSUTCDATETIME()` |

#### `Posts`
| 欄位 | 型別 | 說明 |
|------|------|------|
| PostId | INT IDENTITY PK | 發文 ID |
| UserId | INT FK → Users | 發文者 |
| Content | NVARCHAR(2000) | 內文 |
| ImagePath | NVARCHAR(500) NULL | 圖片相對路徑 |
| CreatedAt | DATETIME2 | 發佈時間,預設 `SYSUTCDATETIME()` |

#### `Comments`
| 欄位 | 型別 | 說明 |
|------|------|------|
| CommentId | INT IDENTITY PK | 留言 ID |
| UserId | INT FK → Users | 留言者 |
| PostId | INT FK → Posts | 所屬發文 |
| Content | NVARCHAR(1000) | 留言內容 |
| CreatedAt | DATETIME2 | 留言時間,預設 `SYSUTCDATETIME()` |

- 外鍵:`Posts.UserId → Users.UserId`、`Comments.UserId → Users.UserId`、`Comments.PostId → Posts.PostId`。
- 建議索引:`Posts(CreatedAt DESC)`、`Comments(PostId)`。

### 5.2 Stored Procedure 清單
> 全部使用參數化輸入 (防 SQL Injection)。命名慣例 `usp_{Entity}_{Action}`。

| SP 名稱 | 用途 |
|---------|------|
| `usp_User_Create` | 新增使用者(註冊) |
| `usp_User_GetByPhone` | 依手機號碼查詢使用者(登入驗證用) |
| `usp_User_GetById` | 依 ID 查詢使用者(Profile 用) |
| `usp_User_ExistsByPhone` | 檢查手機號碼是否已存在 |
| `usp_Post_Create` | 新增發文 |
| `usp_Post_GetAll` | 取得所有發文(含作者名稱、留言數),依時間 DESC |
| `usp_Post_GetById` | 取得單篇發文 |
| `usp_Post_GetByUserId` | 取得某使用者所有發文(Profile 用) |
| `usp_Post_Update` | 編輯發文(需驗證作者) |
| `usp_Post_Delete` | 刪除發文(僅刪 Posts 本身;留言刪除由呼叫端 Transaction 一併處理) |
| `usp_Comment_Create` | 新增留言 |
| `usp_Comment_GetByPostId` | 取得某發文的所有留言(含留言者名稱) |
| `usp_Comment_DeleteByPostId` | 刪除某發文的所有留言(供刪文 Transaction 使用) |

### 5.3 DB Script 交付
- `DB/DDL/`:建立資料庫、資料表、索引、外鍵。
- `DB/SP/`:所有 Stored Procedure。
- `DB/DML/`:種子資料(至少 2 名使用者、數篇發文與留言,供展示)。
- 每個 script 需可重複執行(建議 `IF EXISTS ... DROP` 或 `CREATE OR ALTER`)。

---

## 6. 非功能需求 (Non-Functional Requirements)

### NFR-1 防 SQL Injection
- 一律透過 Dapper 呼叫 Stored Procedure,並以 `CommandType.StoredProcedure` + 匿名物件參數傳值(Dapper 自動轉為 `SqlParameter`)。
- **嚴禁**任何字串拼接 SQL。

### NFR-2 防 XSS
- 所有使用者輸入輸出一律使用 Razor 預設 HTML 編碼,**禁止使用 `@Html.Raw()`** 顯示使用者內容。
- 伺服器端對所有輸入做驗證 (Data Annotations + 業務層驗證)。
- 加入 **Content-Security-Policy** 回應標頭(透過 middleware),限制外部資源載入。

### NFR-3 Transaction
- **刪除發文**時需同時刪除該發文的所有留言,涉及 `Comments` 與 `Posts` 兩張表,必須包在同一個資料庫 Transaction:
  ```
  BeginTransaction
    → usp_Comment_DeleteByPostId (先刪留言)
    → usp_Post_Delete            (再刪發文,並驗證作者)
  Commit / (失敗則 Rollback)
  ```
- Transaction 由**業務層**編排,透過 Dapper 傳入 `IDbTransaction`。

### NFR-4 安全性其他
- 密碼永不記錄於 log,永不回傳前端。
- Cookie 設定 `HttpOnly`、`Secure`(HTTPS)、合理 `ExpireTimeSpan`。
- 上傳檔案驗證副檔名與大小,產生隨機檔名(避免路徑穿越與覆蓋)。

### NFR-5 RWD
- 使用 Bootstrap 5,主要頁面(註冊、登入、首頁動態牆、發文詳細頁、Profile)在手機與桌機皆正常顯示。

---

## 7. 頁面 / 路由 (Pages & Routes)

| 路由 | 方法 | 說明 | 需登入 |
|------|------|------|--------|
| `/Account/Register` | GET/POST | 註冊 | ❌ |
| `/Account/Login` | GET/POST | 登入 | ❌ |
| `/Account/Logout` | POST | 登出 | ✅ |
| `/` 或 `/Posts` | GET | 所有發文(動態牆) | ❌(瀏覽可)/發文需登入 |
| `/Posts/Details/{id}` | GET | 發文詳細 + 留言 | ❌ |
| `/Posts/Create` | GET/POST | 新增發文 | ✅ |
| `/Posts/Edit/{id}` | GET/POST | 編輯發文(限作者) | ✅ |
| `/Posts/Delete/{id}` | POST | 刪除發文(限作者) | ✅ |
| `/Comments/Create` | POST | 新增留言 | ✅ |
| `/Profile/{userId}` | GET | 個人頁 | ❌ |

---

## 8. 驗收條件 (Acceptance Criteria)

- [ ] 可用手機號碼註冊,重複手機號碼被拒絕,密碼以雜湊儲存(DB 中查無明碼)。
- [ ] 可用手機號碼 + 密碼登入 / 登出;未登入無法發文、編輯、刪除、留言。
- [ ] 首頁列出全站發文,依時間新到舊,顯示留言數。
- [ ] 可新增發文(含選填圖片);圖片存於 `wwwroot`,DB 存路徑。
- [ ] 僅作者本人可編輯 / 刪除自己的發文;他人操作被拒。
- [ ] 刪除發文會一併刪除其留言,且此操作為 Transaction(可透過中途失敗驗證 rollback)。
- [ ] 發文詳細頁可看到所有留言並新增留言。
- [ ] Profile 頁顯示封面照片、自我介紹與該使用者所有發文。
- [ ] 所有資料存取皆透過 Stored Procedure(程式中無 inline SQL)。
- [ ] 輸入含 `<script>` 等內容時不會被執行(XSS 防護生效)。
- [ ] 主要頁面在手機寬度下版面正常(RWD)。
- [ ] `DB/` 資料夾含可執行的 DDL / SP / DML script。
- [ ] 專案分為展示 / 業務 / 資料 / 共用四層。
- [ ] 具備關鍵單元測試且通過(見第 9 節)。
- [ ] README 完整,依步驟可在本機建置與執行。

---

## 9. 測試需求 (Testing)

使用 xUnit + Moq,對**業務層**撰寫關鍵單元測試(資料層以 mock 隔離),至少涵蓋:
- **密碼雜湊**:同一密碼雜湊後不等於明碼;驗證正確 / 錯誤密碼行為。
- **註冊**:手機號碼已存在時應拒絕註冊。
- **手機格式驗證**:合法 `09xxxxxxxx` 通過,非法格式被拒。
- **發文權限**:非作者嘗試編輯 / 刪除他人發文應被拒。
- **刪文 Transaction**:刪文流程會先刪留言再刪發文;任一步驟失敗則整體 rollback(以 mock 驗證呼叫順序與 rollback)。

---

## 10. 交付物 (Deliverables)

- [ ] 完整可建置的 .NET 10 Solution(依第 3.2 節結構)。
- [ ] `DB/` 內的 DDL / SP / DML script。
- [ ] `README.md`,內容包含:
  - 專案簡介與**架構說明(含分層圖)**。
  - 環境需求(.NET 10 SDK、SQL Server)。
  - **建置與執行步驟**(如何跑 DB script、設定連線字串、啟動網站)。
  - Stored Procedure 清單與用途。
  - 安全性設計說明(防 SQL Injection / XSS / 密碼雜湊 / Transaction 的具體做法)。
  - 測試執行方式。
- [ ] `appsettings.json` 連線字串使用可辨識的預留值(如 `Server=...;Database=SocialMediaDb;...`),敏感資訊不硬編。

---

## 11. 實作注意事項 (Notes for Implementation)

- `IDbConnectionFactory` 建立 `SqlConnection`,連線字串由 `appsettings.json` 注入。
- Repository 方法簽章需支援可選的 `IDbTransaction`,以利業務層編排 Transaction。
- 以 DI 註冊各層 Service 與 Repository(`AddScoped`)。
- 目前登入者 ID 由 `HttpContext.User` 的 Claim 取得,**權限判斷在業務層再次驗證**(不可只靠前端隱藏按鈕)。
- 例外處理:自訂 `BusinessException` 於共用層,Controller 統一轉為友善錯誤訊息。