# [新進人員]玉山銀行軟體工程師-.NET實作題 第一題

這是一個以 ASP.NET Core 10 MVC、Razor Views、SQL Server、Dapper 實作的簡易社群媒體系統，採用四層式架構：展示層、業務層、資料層與共用層。

## 專案簡介

系統功能包含：

- 使用手機號碼註冊與登入
- Cookie 驗證
- 發文、編輯發文、刪除發文
- 留言功能
- 個人頁 Profile
- 使用 Bootstrap 支援 RWD
- 透過 Stored Procedure 存取資料庫
- 具備 Transaction、SQL Injection 防護與 XSS 防護

## 架構說明

本專案分成四層：

- `SocialMedia.Web`：展示層，負責 MVC Controller、Razor View、登入狀態、檔案上傳與畫面呈現
- `SocialMedia.Business`：業務層，負責註冊、登入、發文、留言、權限與交易流程
- `SocialMedia.DataAccess`：資料層，負責 Dapper 與 Stored Procedure 呼叫
- `SocialMedia.Shared`：共用層，放 Entity、ViewModel、常數與共用例外

資料流向：

`Web -> Business -> DataAccess -> SQL Server`

## 方案結構

```text
SocialMedia.slnx
├── src/
│   ├── SocialMedia.Web
│   ├── SocialMedia.Business
│   ├── SocialMedia.DataAccess
│   └── SocialMedia.Shared
├── tests/
│   └── SocialMedia.Tests
└── DB/
	├── DDL/
	├── SP/
	└── DML/
```

## 環境需求

- .NET 10 SDK
- SQL Server 2019 以上、SQL Server Express 或 LocalDB
- Visual Studio 2026 或 `dotnet` CLI

## 資料庫腳本執行順序

請依序執行以下腳本：

1. `DB/DDL/01_create_schema.sql`
2. `DB/SP/01_stored_procedures.sql`
3. `DB/DML/01_seed_data.sql`

上述腳本皆可重複執行。

### SSMS 執行方式

用 SSMS 開啟腳本檔，依序執行即可。

### sqlcmd 執行方式

```powershell
sqlcmd -S .\SQLEXPRESS -E -i "C:\Dev\ESun\Test1 SocialMedia\DB\DDL\01_create_schema.sql"
sqlcmd -S .\SQLEXPRESS -E -i "C:\Dev\ESun\Test1 SocialMedia\DB\SP\01_stored_procedures.sql"
sqlcmd -S .\SQLEXPRESS -E -i "C:\Dev\ESun\Test1 SocialMedia\DB\DML\01_seed_data.sql"
```

若你的 SQL Server instance 不同，請把 `-S` 改成實際名稱。

## 連線字串設定

請在以下檔案設定連線字串：

- `src/SocialMedia.Web/appsettings.json`
- `src/SocialMedia.Web/appsettings.Development.json`

範例：

```json
{
  "ConnectionStrings": {
	"SocialMediaDb": "Server=.\\SQLEXPRESS;Database=SocialMediaDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

若你本機不是 `.\SQLEXPRESS`，請改成你的實際 SQL Server instance。

## 啟動網站

```powershell
dotnet run --project src/SocialMedia.Web/SocialMedia.Web.csproj
```

或直接從 Visual Studio 啟動 `SocialMedia.Web`。

### 主要路由

- `/Account/Register`
- `/Account/Login`
- `/Posts`
- `/Posts/Details/{id}`
- `/Profile/{userId}`

## Stored Procedure 清單

### Users

- `usp_User_Create`：新增使用者
- `usp_User_GetByPhone`：依手機號碼查詢使用者
- `usp_User_GetById`：依使用者 ID 查詢使用者
- `usp_User_ExistsByPhone`：檢查手機號碼是否已存在

### Posts

- `usp_Post_Create`：新增發文
- `usp_Post_GetAll`：取得所有發文（含作者名稱與留言數）
- `usp_Post_GetById`：取得單篇發文
- `usp_Post_GetByUserId`：取得某使用者的所有發文
- `usp_Post_Update`：編輯發文
- `usp_Post_Delete`：刪除發文

### Comments

- `usp_Comment_Create`：新增留言
- `usp_Comment_GetByPostId`：依發文 ID 取得留言
- `usp_Comment_DeleteByPostId`：刪除某篇發文的所有留言

## 安全性設計

### 防 SQL Injection

- 所有資料存取都透過 Stored Procedure
- Dapper 皆使用參數化輸入與 `CommandType.StoredProcedure`

### 防 XSS

- Razor 預設 HTML 編碼
- 不使用 `@Html.Raw()` 顯示使用者輸入內容

### 密碼雜湊

- 使用 `PasswordHasher<T>`
- 採 PBKDF2 與 per-user salt
- DB 不存明碼密碼

### Cookie 驗證

- Cookie 設定 `HttpOnly`
- Cookie 設定 `SecurePolicy = Always`
- 驗證逾時 7 天，並啟用 sliding expiration

### Transaction

- 刪除發文時，先刪留言再刪發文
- 任一步驟失敗都會 rollback

### 檔案上傳

- 僅允許 `.jpg`、`.jpeg`、`.png`
- 檔案大小上限 5 MB
- 上傳檔案存放於 `wwwroot/uploads/covers/` 與 `wwwroot/uploads/posts/`
- 檔名使用隨機 GUID

### Content-Security-Policy

- 以 middleware 加入 `Content-Security-Policy` 回應標頭
- 限制資源來源為同網域

## 測試方式

本專案使用 xUnit + Moq。

執行全部測試：

```powershell
dotnet test
```

已涵蓋的測試情境：

- 密碼雜湊與驗證
- 重複手機註冊被拒
- 手機格式驗證
- 非作者編輯 / 刪除被拒
- 刪文 transaction 順序與 rollback 行為

## 備註

- Home 會導向發文動態牆
- Profile 頁為公開頁面，不顯示密碼相關欄位
- 留言沒有刪除功能
