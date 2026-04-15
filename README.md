## Setup

**Backend (first run):**
```bash
cd backend
cp appsettings.Development.json.example appsettings.Development.json
dotnet tool install --global dotnet-ef    
dotnet ef migrations add Initial           
dotnet run
```
Starts on `http://localhost:5001`. Schema is applied at startup via `db.Database.Migrate()`.

**Backend (subsequent runs):**
```bash
cd backend
dotnet run
```

**Frontend:**
```bash
cd frontend
cp .env.example .env.development
npm install
npm run dev
```
Starts on `http://localhost:5173`.

**Import data:** open the app, upload `Documents.csv` and `DocumentItems.csv` via the UI, click **Import**.

## API

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/import` | Import documents + items from CSV (multipart, max 100 MB) |
| GET | `/api/documents` | Paginated list with filters and sorting |
| GET | `/api/documents/{id}` | Document detail with line items |
| GET | `/api/documents/export` | Export filtered documents as CSV |

## Tech Stack

ASP.NET Core 8 · EF Core · SQLite · CsvHelper · React 18 · TypeScript · Ant Design · Vite
