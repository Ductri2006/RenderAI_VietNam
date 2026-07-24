# Local smoke checks

Start the ASP.NET Core API and FastAPI service locally, then run these checks from PowerShell:

```powershell
Invoke-RestMethod http://localhost:5080/health
Invoke-RestMethod http://localhost:8000/health
```

Expected result: both responses contain `status=ok`.
