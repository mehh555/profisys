## Setup

**Backend:**
```bash
cd backend
cp appsettings.Development.json.example appsettings.Development.json
dotnet tool install --global dotnet-ef    
dotnet ef migrations add Initial           
dotnet run
```

**Frontend:**
```bash
cd frontend
cp .env.example .env.development
npm install
npm run dev
```

## Tech Stack

ASP.NET Core 8 · EF Core · SQLite · CsvHelper · React 18 · TypeScript · Ant Design · Vite
